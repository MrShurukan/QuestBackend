using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuestBackend.Contracts;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminRoutingEnigmaSettingsFlowTests
{
    [Fact]
    public async Task AdminConfiguration_ShouldCreateUpdateActivateRoutingEnigmaAndSettings()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync(includeSecondBlueQuestion: true);
        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        Guid alternativeBlueQuestionId = Guid.Empty;
        Guid alternativeBluePoolId = Guid.Empty;
        await factory.WithDbContextAsync(
            async db =>
            {
                Question alternativeBlueQuestion = new()
                {
                    TagId = config.BlueTagId,
                    Title = "Blue alternate question",
                    BodyRichText = "Alternate routing body",
                    FooterHint = "Alternate routing hint",
                    Status = QuestionStatus.Active,
                    IsActive = true,
                    AnswerSchema = new AnswerSchema
                    {
                        Kind = AnswerValidationKind.Numeric,
                        ExpectedNumericValue = 9,
                        NumericTolerance = 0,
                    },
                };

                QuestionPool alternativeBluePool = new()
                {
                    TagId = config.BlueTagId,
                    Name = "Blue alternate pool",
                    IsActive = true,
                    Description = "alternate blue pool",
                    SortOrder = 100,
                    Entries =
                    [
                        new QuestionPoolEntry
                        {
                            QuestionId = config.BlueQuestionId,
                            Position = 0,
                            IsEnabled = true,
                            Notes = "classic first",
                        },
                        new QuestionPoolEntry
                        {
                            Question = alternativeBlueQuestion,
                            Position = 1,
                            IsEnabled = true,
                            Notes = "alternate second",
                        },
                    ],
                };

                await db.QuestionPools.AddAsync(alternativeBluePool);
                await db.SaveChangesAsync();
                alternativeBlueQuestionId = alternativeBlueQuestion.Id;
                alternativeBluePoolId = alternativeBluePool.Id;
            });

        RoutingProfileUpsertRequest createRoutingRequest = new(
            "Operations routing draft",
            false,
            "draft routing profile",
            [
                new RoutingProfileTagStateRequest(config.BlueTagId, config.BluePoolId, 0, "PoolSlotRotation", true),
                new RoutingProfileTagStateRequest(config.RedTagId, config.RedPoolId, 0, "PoolSlotRotation", true),
            ]);
        HttpResponseMessage createRoutingResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/routing/profiles", createRoutingRequest);
        createRoutingResponseMessage.EnsureSuccessStatusCode();
        RoutingProfileResponse createdRouting = (await createRoutingResponseMessage.Content.ReadFromJsonAsync<RoutingProfileResponse>())!;
        createdRouting.Name.Should().Be("Operations routing draft");
        createdRouting.IsActive.Should().BeFalse();

        RoutingProfileUpsertRequest updateRoutingRequest = new(
            "Operations routing final",
            false,
            "uses alternate blue pool",
            [
                new RoutingProfileTagStateRequest(config.BlueTagId, alternativeBluePoolId, 1, "PoolSlotRotation", true),
                new RoutingProfileTagStateRequest(config.RedTagId, config.RedPoolId, 0, "PoolSlotRotation", false),
            ]);
        HttpResponseMessage updateRoutingResponseMessage = await adminClient.PutAsJsonAsync(
            $"/api/admin/routing/profiles/{createdRouting.Id}",
            updateRoutingRequest);
        updateRoutingResponseMessage.EnsureSuccessStatusCode();
        RoutingProfileResponse updatedRouting = (await updateRoutingResponseMessage.Content.ReadFromJsonAsync<RoutingProfileResponse>())!;
        updatedRouting.Name.Should().Be("Operations routing final");
        updatedRouting.Description.Should().Be("uses alternate blue pool");
        updatedRouting.TagStates.Single(x => x.TagId == config.BlueTagId).ActivePoolId.Should().Be(alternativeBluePoolId);
        updatedRouting.TagStates.Single(x => x.TagId == config.BlueTagId).RotationOffset.Should().Be(1);
        updatedRouting.TagStates.Single(x => x.TagId == config.RedTagId).IsEnabled.Should().BeFalse();

        List<RoutingProfileResponse> routingProfiles = (await adminClient.GetFromJsonAsync<List<RoutingProfileResponse>>("/api/admin/routing/profiles"))!;
        RoutingProfileResponse persistedRouting = routingProfiles.Single(x => x.Id == createdRouting.Id);
        persistedRouting.Name.Should().Be("Operations routing final");
        persistedRouting.TagStates.Single(x => x.TagId == config.BlueTagId).ActivePoolId.Should().Be(alternativeBluePoolId);
        persistedRouting.TagStates.Single(x => x.TagId == config.BlueTagId).RotationOffset.Should().Be(1);
        persistedRouting.TagStates.Single(x => x.TagId == config.RedTagId).IsEnabled.Should().BeFalse();

        HttpResponseMessage activateRoutingResponse = await adminClient.PostAsync($"/api/admin/routing/profiles/{createdRouting.Id}/activate", null);
        activateRoutingResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        GlobalSettingsResponse settingsAfterRoutingActivation =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;
        settingsAfterRoutingActivation.CurrentRoutingProfileId.Should().Be(createdRouting.Id);

        List<RoutingPreviewRowResponse> previewAfterActivation =
            (await adminClient.GetFromJsonAsync<List<RoutingPreviewRowResponse>>("/api/admin/routing/preview"))!;
        previewAfterActivation.Should().Contain(x => x.QrCodeId == config.BlueQrId && x.QuestionId == alternativeBlueQuestionId);

        HttpResponseMessage rotateResponse = await adminClient.PostAsync($"/api/admin/routing/tags/{config.BlueTagId}/rotate?step=1", null);
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<RoutingPreviewRowResponse> previewAfterRotate =
            (await adminClient.GetFromJsonAsync<List<RoutingPreviewRowResponse>>("/api/admin/routing/preview"))!;
        previewAfterRotate.Should().Contain(x => x.QrCodeId == config.BlueQrId && x.QuestionId == config.BlueQuestionId);

        config.SecondBlueQuestionId.HasValue.Should().BeTrue();
        Guid secondBlueQuestionId = config.SecondBlueQuestionId!.Value;
        QrBindingOverrideRequest overrideRequest = new(config.BlueQrId, secondBlueQuestionId, createdRouting.Id, true, "force backup question");
        HttpResponseMessage createOverrideResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/routing/overrides", overrideRequest);
        createOverrideResponseMessage.EnsureSuccessStatusCode();
        QrBindingOverrideResponse createdOverride = (await createOverrideResponseMessage.Content.ReadFromJsonAsync<QrBindingOverrideResponse>())!;
        createdOverride.IsActive.Should().BeTrue();
        createdOverride.ScopeProfileId.Should().Be(createdRouting.Id);

        List<RoutingPreviewRowResponse> previewAfterOverride =
            (await adminClient.GetFromJsonAsync<List<RoutingPreviewRowResponse>>("/api/admin/routing/preview"))!;
        previewAfterOverride.Should().Contain(
            x => x.QrCodeId == config.BlueQrId
                && x.QuestionId == secondBlueQuestionId
                && x.ResolutionMode.Contains("Override", StringComparison.OrdinalIgnoreCase));

        HttpResponseMessage clearOverrideResponse = await adminClient.DeleteAsync($"/api/admin/routing/overrides/{createdOverride.Id}");
        clearOverrideResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<RoutingPreviewRowResponse> previewAfterClear =
            (await adminClient.GetFromJsonAsync<List<RoutingPreviewRowResponse>>("/api/admin/routing/preview"))!;
        previewAfterClear.Should().Contain(x => x.QrCodeId == config.BlueQrId && x.QuestionId == config.BlueQuestionId);

        bool overrideIsActive = await factory.WithDbContextAsync(
            async db => await db.QrBindingOverrides
                .AsNoTracking()
                .Where(x => x.Id == createdOverride.Id)
                .Select(x => x.IsActive)
                .SingleAsync());
        overrideIsActive.Should().BeFalse();

        EnigmaProfileUpsertRequest createEnigmaRequest = new(
            "Operations enigma draft",
            "HistoricalLike",
            false,
            11,
            "decoded text",
            "wrong text",
            new Dictionary<Guid, int>
            {
                [config.BlueTagId] = 2,
                [config.RedTagId] = 5,
            },
            [
                new EnigmaRotorDefinitionRequest(config.BlueTagId, "Blue rotor custom", "#112233", 1, 0, 9, true),
                new EnigmaRotorDefinitionRequest(config.RedTagId, "Red rotor custom", null, 2, 1, 9, true),
            ]);
        HttpResponseMessage createEnigmaResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/enigma/profiles", createEnigmaRequest);
        createEnigmaResponseMessage.EnsureSuccessStatusCode();
        EnigmaProfileResponse createdEnigma = (await createEnigmaResponseMessage.Content.ReadFromJsonAsync<EnigmaProfileResponse>())!;
        createdEnigma.Mode.Should().Be("HistoricalLike");
        createdEnigma.AttemptCooldownMinutes.Should().Be(11);
        createdEnigma.Rotors.Select(x => x.Label).Should().Equal("Blue rotor custom", "Red rotor custom");

        EnigmaProfileUpsertRequest updateEnigmaRequest = new(
            "Operations enigma final",
            "SimpleCombination",
            false,
            7,
            "decoded final",
            "wrong final",
            new Dictionary<Guid, int>
            {
                [config.BlueTagId] = 4,
                [config.RedTagId] = 7,
            },
            [
                new EnigmaRotorDefinitionRequest(config.RedTagId, "Red rotor first", "#AA0000", 1, 1, 12, true),
                new EnigmaRotorDefinitionRequest(config.BlueTagId, "Blue rotor second", null, 2, 2, 10, false),
            ]);
        HttpResponseMessage updateEnigmaResponseMessage = await adminClient.PutAsJsonAsync(
            $"/api/admin/enigma/profiles/{createdEnigma.Id}",
            updateEnigmaRequest);
        updateEnigmaResponseMessage.EnsureSuccessStatusCode();
        EnigmaProfileResponse updatedEnigma = (await updateEnigmaResponseMessage.Content.ReadFromJsonAsync<EnigmaProfileResponse>())!;
        updatedEnigma.Name.Should().Be("Operations enigma final");
        updatedEnigma.Mode.Should().Be("SimpleCombination");
        updatedEnigma.AttemptCooldownMinutes.Should().Be(7);
        updatedEnigma.SuccessMessage.Should().Be("decoded final");
        updatedEnigma.FailureMessage.Should().Be("wrong final");
        updatedEnigma.Rotors.Select(x => x.Label).Should().Equal("Red rotor first", "Blue rotor second");
        updatedEnigma.Rotors.Select(x => x.DisplayOrder).Should().Equal(1, 2);
        updatedEnigma.Rotors[0].ColorOverride.Should().Be("#AA0000");
        updatedEnigma.Rotors[1].ColorOverride.Should().BeNull();
        updatedEnigma.Rotors[1].IsActive.Should().BeFalse();

        List<EnigmaProfileResponse> enigmaProfiles = (await adminClient.GetFromJsonAsync<List<EnigmaProfileResponse>>("/api/admin/enigma/profiles"))!;
        EnigmaProfileResponse persistedEnigma = enigmaProfiles.Single(x => x.Id == createdEnigma.Id);
        persistedEnigma.Name.Should().Be("Operations enigma final");
        persistedEnigma.Mode.Should().Be("SimpleCombination");
        persistedEnigma.AttemptCooldownMinutes.Should().Be(7);
        persistedEnigma.SecretCombination[config.BlueTagId].Should().Be(4);
        persistedEnigma.SecretCombination[config.RedTagId].Should().Be(7);
        persistedEnigma.Rotors.Select(x => x.Label).Should().Equal("Red rotor first", "Blue rotor second");

        HttpResponseMessage activateEnigmaResponse = await adminClient.PostAsync($"/api/admin/enigma/profiles/{createdEnigma.Id}/activate", null);
        activateEnigmaResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        GlobalSettingsResponse settingsAfterEnigmaActivation =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;
        settingsAfterEnigmaActivation.CurrentEnigmaProfileId.Should().Be(createdEnigma.Id);

        GlobalSettingsUpdateRequest updateSettingsRequest = new(
            3,
            9,
            6,
            "TrimWhitespace|IgnoreCase",
            settingsAfterEnigmaActivation.CurrentQuestDayStateId,
            createdRouting.Id,
            createdEnigma.Id,
            "{\"beta\":true,\"rotation\":\"ops\"}",
            "Europe/Warsaw");
        HttpResponseMessage updateSettingsResponseMessage = await adminClient.PutAsJsonAsync("/api/admin/settings/global", updateSettingsRequest);
        updateSettingsResponseMessage.EnsureSuccessStatusCode();
        GlobalSettingsResponse updatedSettings = (await updateSettingsResponseMessage.Content.ReadFromJsonAsync<GlobalSettingsResponse>())!;
        updatedSettings.AnswerCooldownMinutes.Should().Be(3);
        updatedSettings.EnigmaCooldownMinutes.Should().Be(9);
        updatedSettings.MaxTeamMembers.Should().Be(6);
        updatedSettings.DefaultAnswerNormalization.Should().Be("TrimWhitespace|IgnoreCase");
        updatedSettings.CurrentRoutingProfileId.Should().Be(createdRouting.Id);
        updatedSettings.CurrentEnigmaProfileId.Should().Be(createdEnigma.Id);
        updatedSettings.FlagsJson.Should().Be("{\"beta\":true,\"rotation\":\"ops\"}");
        updatedSettings.Timezone.Should().Be("Europe/Warsaw");

        GlobalSettingsResponse persistedSettings =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;
        persistedSettings.Should().BeEquivalentTo(updatedSettings);
    }
}
