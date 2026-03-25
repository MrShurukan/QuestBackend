using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminNegativeCasesFlowTests
{
    [Fact]
    public async Task Admin_InvalidRequests_ShouldReturnExpectedErrors_AndPreservePersistedState()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        HttpResponseMessage createTagResponseMessage = await adminClient.PostAsJsonAsync(
            "/api/admin/tags",
            new TagUpsertRequest("green", "Green", "#00AA44", true, 10, null));
        createTagResponseMessage.EnsureSuccessStatusCode();

        List<TagResponse> tagsBeforeDuplicates = (await adminClient.GetFromJsonAsync<List<TagResponse>>("/api/admin/tags"))!;

        HttpResponseMessage duplicateCodeResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/tags",
            new TagUpsertRequest("green", "Green Duplicate", "#00AA55", true, 11, null));
        duplicateCodeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        HttpResponseMessage duplicateNameResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/tags",
            new TagUpsertRequest("green-second", "Green", "#00AA66", true, 12, null));
        duplicateNameResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        List<TagResponse> tagsAfterDuplicates = (await adminClient.GetFromJsonAsync<List<TagResponse>>("/api/admin/tags"))!;
        tagsAfterDuplicates.Count.Should().Be(tagsBeforeDuplicates.Count);
        tagsAfterDuplicates.Should().ContainSingle(x => x.Code == "green" && x.Name == "Green");

        List<QrCodeResponse> qrBeforeDuplicate = (await adminClient.GetFromJsonAsync<List<QrCodeResponse>>("/api/admin/qr"))!;
        HttpResponseMessage duplicateQrResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/qr",
            new QrCodeUpsertRequest(config.BlueTagId, config.BlueSlug, "Duplicate QR", 99, true, "duplicate"));
        duplicateQrResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        List<QrCodeResponse> qrAfterDuplicate = (await adminClient.GetFromJsonAsync<List<QrCodeResponse>>("/api/admin/qr"))!;
        qrAfterDuplicate.Count.Should().Be(qrBeforeDuplicate.Count);
        qrAfterDuplicate.Should().ContainSingle(x => x.Id == config.BlueQrId && x.Slug == config.BlueSlug);

        HttpResponseMessage nonMultipartUploadResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/questions/upload-image",
            new { anything = "not multipart" });
        nonMultipartUploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage unsupportedUploadResponse = await factory.PostImageAsync(
            adminClient,
            "/api/admin/questions/upload-image",
            "image",
            "question.txt",
            "text/plain",
            [1, 2, 3]);
        unsupportedUploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage emptyUploadResponse = await factory.PostImageAsync(
            adminClient,
            "/api/admin/questions/upload-image",
            "image",
            "empty.png",
            "image/png",
            []);
        emptyUploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        GlobalSettingsResponse settingsBeforeInvalidUpdate =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;
        HttpResponseMessage invalidSettingsResponse = await adminClient.PutAsJsonAsync(
            "/api/admin/settings/global",
            new GlobalSettingsUpdateRequest(
                settingsBeforeInvalidUpdate.AnswerCooldownMinutes,
                settingsBeforeInvalidUpdate.EnigmaCooldownMinutes,
                0,
                settingsBeforeInvalidUpdate.DefaultAnswerNormalization,
                settingsBeforeInvalidUpdate.CurrentQuestDayStateId,
                settingsBeforeInvalidUpdate.CurrentRoutingProfileId,
                settingsBeforeInvalidUpdate.CurrentEnigmaProfileId,
                settingsBeforeInvalidUpdate.FlagsJson,
                settingsBeforeInvalidUpdate.Timezone));
        invalidSettingsResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        GlobalSettingsResponse settingsAfterInvalidUpdate =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;
        settingsAfterInvalidUpdate.Should().BeEquivalentTo(settingsBeforeInvalidUpdate);
    }

    [Fact]
    public async Task Admin_UnsupportedEnumStrings_ShouldFallbackToSafeDefaults()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        QuestionUpsertRequest questionRequest = new(
            config.BlueTagId,
            "Fallback question",
            "<p>body</p>",
            "hint",
            null,
            "",
            true,
            false,
            "fallback",
            new QuestionAnswerSchemaDto("mystery", ["fallback answer"], 12m, 2m, true, false, true, false));
        HttpResponseMessage questionResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/questions", questionRequest);
        questionResponseMessage.EnsureSuccessStatusCode();
        QuestionResponse createdQuestion = (await questionResponseMessage.Content.ReadFromJsonAsync<QuestionResponse>())!;
        createdQuestion.Status.Should().Be("Draft");
        createdQuestion.AnswerSchema.Kind.Should().Be("NormalizedText");

        RoutingProfileUpsertRequest routingRequest = new(
            "Fallback routing",
            false,
            "fallback routing",
            [
                new RoutingProfileTagStateRequest(config.BlueTagId, config.BluePoolId, 3, "", true),
                new RoutingProfileTagStateRequest(config.RedTagId, config.RedPoolId, 0, "not-real", true),
            ]);
        HttpResponseMessage routingResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/routing/profiles", routingRequest);
        routingResponseMessage.EnsureSuccessStatusCode();
        RoutingProfileResponse createdRouting = (await routingResponseMessage.Content.ReadFromJsonAsync<RoutingProfileResponse>())!;
        createdRouting.TagStates.Should().OnlyContain(x => x.SelectionMode == "PoolSlotRotation");

        EnigmaProfileUpsertRequest enigmaRequest = new(
            "Fallback enigma",
            "",
            false,
            5,
            "success",
            "failure",
            new Dictionary<Guid, int>
            {
                [config.BlueTagId] = 1,
            },
            [
                new EnigmaRotorDefinitionRequest(config.BlueTagId, "Blue fallback rotor", null, 1, 1, 9, true),
            ]);
        HttpResponseMessage enigmaResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/enigma/profiles", enigmaRequest);
        enigmaResponseMessage.EnsureSuccessStatusCode();
        EnigmaProfileResponse createdEnigma = (await enigmaResponseMessage.Content.ReadFromJsonAsync<EnigmaProfileResponse>())!;
        createdEnigma.Mode.Should().Be("SimpleCombination");

        List<QuestionResponse> questions = (await adminClient.GetFromJsonAsync<List<QuestionResponse>>("/api/admin/questions"))!;
        questions.Single(x => x.Id == createdQuestion.Id).Status.Should().Be("Draft");
        questions.Single(x => x.Id == createdQuestion.Id).AnswerSchema.Kind.Should().Be("NormalizedText");

        List<RoutingProfileResponse> routingProfiles = (await adminClient.GetFromJsonAsync<List<RoutingProfileResponse>>("/api/admin/routing/profiles"))!;
        routingProfiles.Single(x => x.Id == createdRouting.Id).TagStates.Should().OnlyContain(x => x.SelectionMode == "PoolSlotRotation");

        List<EnigmaProfileResponse> enigmaProfiles = (await adminClient.GetFromJsonAsync<List<EnigmaProfileResponse>>("/api/admin/enigma/profiles"))!;
        enigmaProfiles.Single(x => x.Id == createdEnigma.Id).Mode.Should().Be("SimpleCombination");
    }
}
