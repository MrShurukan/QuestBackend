using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminConfigurationCrudFlowTests
{
    [Fact]
    public async Task AdminConfiguration_ShouldCreateAndUpdateTagsQuestionsPoolsAndQrCodes()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync(includeSecondBlueQuestion: true);
        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        TagUpsertRequest createTagRequest = new("green", "Green", "#00AA44", true, 30, "green tag");
        HttpResponseMessage createTagResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/tags", createTagRequest);
        createTagResponseMessage.EnsureSuccessStatusCode();
        TagResponse createdTag = (await createTagResponseMessage.Content.ReadFromJsonAsync<TagResponse>())!;
        createdTag.Code.Should().Be("green");
        createdTag.Name.Should().Be("Green");
        createdTag.Color.Should().Be("#00AA44");
        createdTag.IsActive.Should().BeTrue();

        TagUpsertRequest updateTagRequest = new("green-ops", "Green Ops", "#008855", false, 31, "updated green tag");
        HttpResponseMessage updateTagResponseMessage = await adminClient.PutAsJsonAsync($"/api/admin/tags/{createdTag.Id}", updateTagRequest);
        updateTagResponseMessage.EnsureSuccessStatusCode();
        TagResponse updatedTag = (await updateTagResponseMessage.Content.ReadFromJsonAsync<TagResponse>())!;
        updatedTag.Code.Should().Be("green-ops");
        updatedTag.Name.Should().Be("Green Ops");
        updatedTag.Color.Should().Be("#008855");
        updatedTag.IsActive.Should().BeFalse();
        updatedTag.SortOrder.Should().Be(31);
        updatedTag.Description.Should().Be("updated green tag");

        List<TagResponse> tags = (await adminClient.GetFromJsonAsync<List<TagResponse>>("/api/admin/tags"))!;
        TagResponse persistedTag = tags.Single(x => x.Id == createdTag.Id);
        persistedTag.Code.Should().Be("green-ops");
        persistedTag.Name.Should().Be("Green Ops");
        persistedTag.Color.Should().Be("#008855");
        persistedTag.IsActive.Should().BeFalse();
        persistedTag.SortOrder.Should().Be(31);
        persistedTag.Description.Should().Be("updated green tag");

        QuestionImageUploadResponse uploadedImage = await factory.UploadQuestionImageAsync(adminClient, "question-a.png");
        uploadedImage.ImageUrl.Should().StartWith("/uploads/questions/");
        HttpResponseMessage uploadedImageResponse = await adminClient.GetAsync(uploadedImage.ImageUrl);
        uploadedImageResponse.EnsureSuccessStatusCode();

        QuestionImageUploadResponse replacementImage = await factory.UploadQuestionImageAsync(adminClient, "question-b.png");
        replacementImage.ImageUrl.Should().StartWith("/uploads/questions/");
        replacementImage.ImageUrl.Should().NotBe(uploadedImage.ImageUrl);

        QuestionUpsertRequest createQuestionRequest = new(
            config.BlueTagId,
            "Blue exact question",
            "<p>Type the exact phrase BLUE</p>",
            "Exact hint",
            uploadedImage.ImageUrl,
            "Active",
            true,
            false,
            "exact support notes",
            new QuestionAnswerSchemaDto("ExactText", ["BLUE"], null, null, false, false, false, false));
        HttpResponseMessage createQuestionResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/questions", createQuestionRequest);
        createQuestionResponseMessage.EnsureSuccessStatusCode();
        QuestionResponse createdQuestion = (await createQuestionResponseMessage.Content.ReadFromJsonAsync<QuestionResponse>())!;
        createdQuestion.Title.Should().Be("Blue exact question");
        createdQuestion.ImageUrl.Should().Be(uploadedImage.ImageUrl);
        createdQuestion.Status.Should().Be("Active");
        createdQuestion.AnswerSchema.Kind.Should().Be("ExactText");
        createdQuestion.AnswerSchema.AcceptedAnswers.Should().Equal("BLUE");

        QuestionUpsertRequest normalizedQuestionRequest = new(
            config.BlueTagId,
            "Blue normalized question",
            "<p>Normalize spaces and punctuation</p>",
            "Normalized hint",
            replacementImage.ImageUrl,
            "Disabled",
            false,
            true,
            "normalized support notes",
            new QuestionAnswerSchemaDto("NormalizedText", ["Blue Planet"], null, null, true, true, true, true));
        HttpResponseMessage normalizedUpdateResponseMessage = await adminClient.PutAsJsonAsync(
            $"/api/admin/questions/{createdQuestion.Id}",
            normalizedQuestionRequest);
        normalizedUpdateResponseMessage.EnsureSuccessStatusCode();
        QuestionResponse normalizedQuestion = (await normalizedUpdateResponseMessage.Content.ReadFromJsonAsync<QuestionResponse>())!;
        normalizedQuestion.Title.Should().Be("Blue normalized question");
        normalizedQuestion.ImageUrl.Should().Be(replacementImage.ImageUrl);
        normalizedQuestion.Status.Should().Be("Disabled");
        normalizedQuestion.IsActive.Should().BeFalse();
        normalizedQuestion.IsArchived.Should().BeTrue();
        normalizedQuestion.AnswerSchema.Kind.Should().Be("NormalizedText");
        normalizedQuestion.AnswerSchema.AcceptedAnswers.Should().Equal("Blue Planet");
        normalizedQuestion.AnswerSchema.TrimWhitespace.Should().BeTrue();
        normalizedQuestion.AnswerSchema.IgnoreCase.Should().BeTrue();
        normalizedQuestion.AnswerSchema.CollapseInnerWhitespace.Should().BeTrue();
        normalizedQuestion.AnswerSchema.RemovePunctuation.Should().BeTrue();

        QuestionUpsertRequest numericQuestionRequest = new(
            config.BlueTagId,
            "Blue numeric question",
            "<p>Enter the numeric code</p>",
            "Numeric hint",
            null,
            "Draft",
            true,
            false,
            "numeric support notes",
            new QuestionAnswerSchemaDto("Numeric", [], 42.5m, 0.25m, true, true, false, false));
        HttpResponseMessage numericUpdateResponseMessage = await adminClient.PutAsJsonAsync(
            $"/api/admin/questions/{createdQuestion.Id}",
            numericQuestionRequest);
        numericUpdateResponseMessage.EnsureSuccessStatusCode();
        QuestionResponse numericQuestion = (await numericUpdateResponseMessage.Content.ReadFromJsonAsync<QuestionResponse>())!;
        numericQuestion.Title.Should().Be("Blue numeric question");
        numericQuestion.ImageUrl.Should().BeNull();
        numericQuestion.Status.Should().Be("Draft");
        numericQuestion.IsActive.Should().BeTrue();
        numericQuestion.IsArchived.Should().BeFalse();
        numericQuestion.AnswerSchema.Kind.Should().Be("Numeric");
        numericQuestion.AnswerSchema.AcceptedAnswers.Should().BeEmpty();
        numericQuestion.AnswerSchema.ExpectedNumericValue.Should().Be(42.5m);
        numericQuestion.AnswerSchema.NumericTolerance.Should().Be(0.25m);

        List<QuestionResponse> questions = (await adminClient.GetFromJsonAsync<List<QuestionResponse>>("/api/admin/questions"))!;
        QuestionResponse persistedQuestion = questions.Single(x => x.Id == createdQuestion.Id);
        persistedQuestion.Title.Should().Be("Blue numeric question");
        persistedQuestion.ImageUrl.Should().BeNull();
        persistedQuestion.Status.Should().Be("Draft");
        persistedQuestion.AnswerSchema.Kind.Should().Be("Numeric");
        persistedQuestion.AnswerSchema.AcceptedAnswers.Should().BeEmpty();
        persistedQuestion.AnswerSchema.ExpectedNumericValue.Should().Be(42.5m);
        persistedQuestion.AnswerSchema.NumericTolerance.Should().Be(0.25m);
        persistedQuestion.SupportNotes.Should().Be("numeric support notes");

        config.SecondBlueQuestionId.HasValue.Should().BeTrue();
        Guid secondBlueQuestionId = config.SecondBlueQuestionId!.Value;
        QuestionPoolUpsertRequest createPoolRequest = new(
            config.BlueTagId,
            "Blue bonus pool",
            true,
            false,
            "initial pool",
            9,
            [
                new QuestionPoolEntryRequest(config.BlueQuestionId, 0, true, "classic"),
                new QuestionPoolEntryRequest(secondBlueQuestionId, 1, false, "backup"),
                new QuestionPoolEntryRequest(createdQuestion.Id, 2, true, "numeric")
            ]);
        HttpResponseMessage createPoolResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/pools", createPoolRequest);
        createPoolResponseMessage.EnsureSuccessStatusCode();
        QuestionPoolResponse createdPool = (await createPoolResponseMessage.Content.ReadFromJsonAsync<QuestionPoolResponse>())!;
        createdPool.Name.Should().Be("Blue bonus pool");
        createdPool.Entries.Select(x => x.QuestionId).Should().Equal(config.BlueQuestionId, secondBlueQuestionId, createdQuestion.Id);
        createdPool.Entries.Select(x => x.Position).Should().Equal(0, 1, 2);

        QuestionPoolUpsertRequest updatePoolRequest = new(
            config.BlueTagId,
            "Blue bonus pool v2",
            false,
            true,
            "updated pool",
            10,
            [
                new QuestionPoolEntryRequest(createdQuestion.Id, 0, true, "numeric first"),
                new QuestionPoolEntryRequest(config.BlueQuestionId, 1, false, "classic second"),
                new QuestionPoolEntryRequest(secondBlueQuestionId, 2, true, "backup third")
            ]);
        HttpResponseMessage updatePoolResponseMessage = await adminClient.PutAsJsonAsync($"/api/admin/pools/{createdPool.Id}", updatePoolRequest);
        updatePoolResponseMessage.EnsureSuccessStatusCode();
        QuestionPoolResponse updatedPool = (await updatePoolResponseMessage.Content.ReadFromJsonAsync<QuestionPoolResponse>())!;
        updatedPool.Name.Should().Be("Blue bonus pool v2");
        updatedPool.IsActive.Should().BeFalse();
        updatedPool.IsArchived.Should().BeTrue();
        updatedPool.Description.Should().Be("updated pool");
        updatedPool.Entries.Select(x => x.QuestionId).Should().Equal(createdQuestion.Id, config.BlueQuestionId, secondBlueQuestionId);
        updatedPool.Entries.Select(x => x.Position).Should().Equal(0, 1, 2);
        updatedPool.Entries.Select(x => x.IsEnabled).Should().Equal(true, false, true);

        List<QuestionPoolResponse> pools = (await adminClient.GetFromJsonAsync<List<QuestionPoolResponse>>("/api/admin/pools"))!;
        QuestionPoolResponse persistedPool = pools.Single(x => x.Id == createdPool.Id);
        persistedPool.Name.Should().Be("Blue bonus pool v2");
        persistedPool.IsActive.Should().BeFalse();
        persistedPool.IsArchived.Should().BeTrue();
        persistedPool.Description.Should().Be("updated pool");
        persistedPool.Entries.Select(x => x.QuestionId).Should().Equal(createdQuestion.Id, config.BlueQuestionId, secondBlueQuestionId);
        persistedPool.Entries.Select(x => x.Position).Should().Equal(0, 1, 2);

        var poolEntriesFromDb = await factory.WithDbContextAsync(
            async db => await db.QuestionPools
                .AsNoTracking()
                .Where(x => x.Id == createdPool.Id)
                .Select(
                    x => x.Entries
                        .OrderBy(e => e.Position)
                        .Select(e => new { e.QuestionId, e.Position, e.IsEnabled, e.Notes })
                        .ToList())
                .SingleAsync());
        poolEntriesFromDb.Select(x => x.QuestionId).Should().Equal(createdQuestion.Id, config.BlueQuestionId, secondBlueQuestionId);
        poolEntriesFromDb.Select(x => x.Position).Should().Equal(0, 1, 2);
        poolEntriesFromDb.Select(x => x.IsEnabled).Should().Equal(true, false, true);
        poolEntriesFromDb.Select(x => x.Notes).Should().Equal("numeric first", "classic second", "backup third");

        QrCodeUpsertRequest createQrRequest = new(config.BlueTagId, "blueextra1", "Blue extra QR", 2, true, "extra qr");
        HttpResponseMessage createQrResponseMessage = await adminClient.PostAsJsonAsync("/api/admin/qr", createQrRequest);
        createQrResponseMessage.EnsureSuccessStatusCode();
        QrCodeResponse createdQr = (await createQrResponseMessage.Content.ReadFromJsonAsync<QrCodeResponse>())!;
        createdQr.Slug.Should().Be("blueextra1");
        createdQr.Label.Should().Be("Blue extra QR");
        createdQr.SlotIndex.Should().Be(2);

        QrCodeUpsertRequest updateQrRequest = new(config.RedTagId, "redextra1", "Red extra QR", 4, false, "updated qr");
        HttpResponseMessage updateQrResponseMessage = await adminClient.PutAsJsonAsync($"/api/admin/qr/{createdQr.Id}", updateQrRequest);
        updateQrResponseMessage.EnsureSuccessStatusCode();
        QrCodeResponse updatedQr = (await updateQrResponseMessage.Content.ReadFromJsonAsync<QrCodeResponse>())!;
        updatedQr.TagId.Should().Be(config.RedTagId);
        updatedQr.Slug.Should().Be("redextra1");
        updatedQr.Label.Should().Be("Red extra QR");
        updatedQr.SlotIndex.Should().Be(4);
        updatedQr.IsActive.Should().BeFalse();
        updatedQr.Notes.Should().Be("updated qr");

        List<QrCodeResponse> qrCodes = (await adminClient.GetFromJsonAsync<List<QrCodeResponse>>("/api/admin/qr"))!;
        QrCodeResponse persistedQr = qrCodes.Single(x => x.Id == createdQr.Id);
        persistedQr.TagId.Should().Be(config.RedTagId);
        persistedQr.Slug.Should().Be("redextra1");
        persistedQr.Label.Should().Be("Red extra QR");
        persistedQr.SlotIndex.Should().Be(4);
        persistedQr.IsActive.Should().BeFalse();
        persistedQr.Notes.Should().Be("updated qr");

        List<RoutingPreviewRowResponse> preview = (await adminClient.GetFromJsonAsync<List<RoutingPreviewRowResponse>>("/api/admin/routing/preview"))!;
        preview.Should().Contain(x => x.QrCodeId == createdQr.Id && x.QrSlug == "redextra1");
    }
}
