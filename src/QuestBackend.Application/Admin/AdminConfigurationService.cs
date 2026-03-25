using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Enigma;
using QuestBackend.Application.QuestDay;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.Admin;

public sealed class AdminConfigurationService
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;
    private readonly IConfigSnapshotService _configSnapshotService;
    private readonly IQuestionRoutingResolver _questionRoutingResolver;
    private readonly IQuestDbContext _dbContext;

    public AdminConfigurationService(
        IQuestDbContext dbContext,
        IAuditWriter auditWriter,
        IConfigSnapshotService configSnapshotService,
        IQuestionRoutingResolver questionRoutingResolver,
        IClock clock)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _configSnapshotService = configSnapshotService;
        _questionRoutingResolver = questionRoutingResolver;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TagResponse>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.QuestionTags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Select(x => new TagResponse(x.Id, x.Code, x.Name, x.Color, x.IsActive, x.SortOrder, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<TagResponse> CreateTagAsync(TagUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QuestionTag tag = new()
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Color = request.Color,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            Description = request.Description,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QuestionTags.AddAsync(tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreateTag", nameof(QuestionTag), tag.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return new TagResponse(tag.Id, tag.Code, tag.Name, tag.Color, tag.IsActive, tag.SortOrder, tag.Description);
    }

    public async Task<TagResponse> UpdateTagAsync(Guid id, TagUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QuestionTag tag = await _dbContext.QuestionTags.SingleAsync(x => x.Id == id, cancellationToken);
        tag.Code = request.Code.Trim();
        tag.Name = request.Name.Trim();
        tag.Color = request.Color;
        tag.IsActive = request.IsActive;
        tag.SortOrder = request.SortOrder;
        tag.Description = request.Description;
        tag.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateTag", nameof(QuestionTag), tag.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return new TagResponse(tag.Id, tag.Code, tag.Name, tag.Color, tag.IsActive, tag.SortOrder, tag.Description);
    }

    public async Task<IReadOnlyList<QuestionResponse>> GetQuestionsAsync(CancellationToken cancellationToken = default)
    {
        List<Question> questions = await _dbContext.Questions
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return questions.Select(ToQuestionResponse).ToList();
    }

    public async Task<QuestionResponse> CreateQuestionAsync(QuestionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        Question question = new()
        {
            TagId = request.TagId,
            Title = request.Title.Trim(),
            BodyRichText = request.BodyRichText,
            FooterHint = request.FooterHint,
            ImageUrl = request.ImageUrl,
            Status = ParseQuestionStatus(request.Status),
            IsActive = request.IsActive,
            IsArchived = request.IsArchived,
            SupportNotes = request.SupportNotes,
            AnswerSchema = ToAnswerSchema(request.AnswerSchema),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.Questions.AddAsync(question, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreateQuestion", nameof(Question), question.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToQuestionResponse(question);
    }

    public async Task<QuestionResponse> UpdateQuestionAsync(Guid id, QuestionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        Question question = await _dbContext.Questions.SingleAsync(x => x.Id == id, cancellationToken);
        question.TagId = request.TagId;
        question.Title = request.Title.Trim();
        question.BodyRichText = request.BodyRichText;
        question.FooterHint = request.FooterHint;
        question.ImageUrl = request.ImageUrl;
        question.Status = ParseQuestionStatus(request.Status);
        question.IsActive = request.IsActive;
        question.IsArchived = request.IsArchived;
        question.SupportNotes = request.SupportNotes;
        question.AnswerSchema = ToAnswerSchema(request.AnswerSchema);
        question.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateQuestion", nameof(Question), question.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToQuestionResponse(question);
    }

    public async Task<QuestionResponse> DuplicateQuestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Question question = await _dbContext.Questions
            .AsNoTracking()
            .SingleAsync(x => x.Id == id, cancellationToken);

        Question duplicate = new()
        {
            TagId = question.TagId,
            Title = $"{question.Title} (copy)",
            BodyRichText = question.BodyRichText,
            FooterHint = question.FooterHint,
            ImageUrl = question.ImageUrl,
            Status = QuestionStatus.Draft,
            IsActive = false,
            IsArchived = false,
            SupportNotes = question.SupportNotes,
            AnswerSchema = question.AnswerSchema,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.Questions.AddAsync(duplicate, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("DuplicateQuestion", nameof(Question), duplicate.Id.ToString(), AppJson.Serialize(new { sourceId = id }), null, cancellationToken);

        return ToQuestionResponse(duplicate);
    }

    public async Task<IReadOnlyList<QuestionPoolResponse>> GetPoolsAsync(CancellationToken cancellationToken = default)
    {
        List<QuestionPool> pools = await _dbContext.QuestionPools
            .AsNoTracking()
            .Include(x => x.Entries.OrderBy(e => e.Position))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return pools.Select(ToPoolResponse).ToList();
    }

    public async Task<QuestionPoolResponse> CreatePoolAsync(QuestionPoolUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QuestionPool pool = new()
        {
            TagId = request.TagId,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            IsArchived = request.IsArchived,
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
            Entries = request.Entries
                .OrderBy(x => x.Position)
                .Select(
                    x => new QuestionPoolEntry
                    {
                        QuestionId = x.QuestionId,
                        Position = x.Position,
                        IsEnabled = x.IsEnabled,
                        Notes = x.Notes,
                        CreatedAt = _clock.UtcNow,
                        UpdatedAt = _clock.UtcNow,
                    })
                .ToList(),
        };

        await _dbContext.QuestionPools.AddAsync(pool, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreatePool", nameof(QuestionPool), pool.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToPoolResponse(pool);
    }

    public async Task<QuestionPoolResponse> UpdatePoolAsync(Guid id, QuestionPoolUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QuestionPool pool = await _dbContext.QuestionPools
            .Include(x => x.Entries)
            .SingleAsync(x => x.Id == id, cancellationToken);

        pool.TagId = request.TagId;
        pool.Name = request.Name.Trim();
        pool.IsActive = request.IsActive;
        pool.IsArchived = request.IsArchived;
        pool.Description = request.Description;
        pool.SortOrder = request.SortOrder;
        pool.UpdatedAt = _clock.UtcNow;

        _dbContext.QuestionPoolEntries.RemoveRange(pool.Entries);
        pool.Entries = request.Entries
            .OrderBy(x => x.Position)
            .Select(
                x => new QuestionPoolEntry
                {
                    QuestionId = x.QuestionId,
                    Position = x.Position,
                    IsEnabled = x.IsEnabled,
                    Notes = x.Notes,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                })
            .ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdatePool", nameof(QuestionPool), pool.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToPoolResponse(pool);
    }

    public async Task<IReadOnlyList<QrCodeResponse>> GetQrCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.QrCodes
            .AsNoTracking()
            .OrderBy(x => x.Label)
            .Select(x => new QrCodeResponse(x.Id, x.TagId, x.Slug, x.Label, x.SlotIndex, x.IsActive, x.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<QrCodeResponse> CreateQrCodeAsync(QrCodeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QrCode qrCode = new()
        {
            TagId = request.TagId,
            Slug = request.Slug.Trim(),
            Label = request.Label.Trim(),
            SlotIndex = request.SlotIndex,
            IsActive = request.IsActive,
            Notes = request.Notes,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QrCodes.AddAsync(qrCode, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreateQrCode", nameof(QrCode), qrCode.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return new QrCodeResponse(qrCode.Id, qrCode.TagId, qrCode.Slug, qrCode.Label, qrCode.SlotIndex, qrCode.IsActive, qrCode.Notes);
    }

    public async Task<QrCodeResponse> UpdateQrCodeAsync(Guid id, QrCodeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        QrCode qrCode = await _dbContext.QrCodes.SingleAsync(x => x.Id == id, cancellationToken);
        qrCode.TagId = request.TagId;
        qrCode.Slug = request.Slug.Trim();
        qrCode.Label = request.Label.Trim();
        qrCode.SlotIndex = request.SlotIndex;
        qrCode.IsActive = request.IsActive;
        qrCode.Notes = request.Notes;
        qrCode.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateQrCode", nameof(QrCode), qrCode.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return new QrCodeResponse(qrCode.Id, qrCode.TagId, qrCode.Slug, qrCode.Label, qrCode.SlotIndex, qrCode.IsActive, qrCode.Notes);
    }

    public async Task<IReadOnlyList<RoutingProfileResponse>> GetRoutingProfilesAsync(CancellationToken cancellationToken = default)
    {
        List<RoutingProfile> profiles = await _dbContext.RoutingProfiles
            .AsNoTracking()
            .Include(x => x.TagStates.OrderBy(y => y.TagId))
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return profiles.Select(ToRoutingProfileResponse).ToList();
    }

    public async Task<RoutingProfileResponse> CreateRoutingProfileAsync(RoutingProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        RoutingProfile profile = new()
        {
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            Description = request.Description,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
            TagStates = request.TagStates
                .Select(
                    x => new RoutingProfileTagState
                    {
                        TagId = x.TagId,
                        ActivePoolId = x.ActivePoolId,
                        RotationOffset = x.RotationOffset,
                        SelectionMode = ParseSelectionMode(x.SelectionMode),
                        IsEnabled = x.IsEnabled,
                        CreatedAt = _clock.UtcNow,
                        UpdatedAt = _clock.UtcNow,
                    })
                .ToList(),
        };

        if (request.IsActive)
        {
            await ActivateRoutingProfileInternalAsync(profile, cancellationToken);
        }

        await _dbContext.RoutingProfiles.AddAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreateRoutingProfile", nameof(RoutingProfile), profile.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToRoutingProfileResponse(profile);
    }

    public async Task<RoutingProfileResponse> UpdateRoutingProfileAsync(Guid id, RoutingProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        RoutingProfile profile = await _dbContext.RoutingProfiles
            .Include(x => x.TagStates)
            .SingleAsync(x => x.Id == id, cancellationToken);

        profile.Name = request.Name.Trim();
        profile.Description = request.Description;
        profile.IsActive = request.IsActive;
        profile.UpdatedAt = _clock.UtcNow;

        _dbContext.RoutingProfileTagStates.RemoveRange(profile.TagStates);
        profile.TagStates = request.TagStates
            .Select(
                x => new RoutingProfileTagState
                {
                    TagId = x.TagId,
                    ActivePoolId = x.ActivePoolId,
                    RotationOffset = x.RotationOffset,
                    SelectionMode = ParseSelectionMode(x.SelectionMode),
                    IsEnabled = x.IsEnabled,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                })
            .ToList();

        if (request.IsActive)
        {
            await ActivateRoutingProfileInternalAsync(profile, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateRoutingProfile", nameof(RoutingProfile), profile.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToRoutingProfileResponse(profile);
    }

    public async Task ActivateRoutingProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _configSnapshotService.CreateSnapshotAsync("RoutingActivation", "Activate routing profile", cancellationToken);

        RoutingProfile profile = await _dbContext.RoutingProfiles.SingleAsync(x => x.Id == id, cancellationToken);
        await ActivateRoutingProfileInternalAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditWriter.WriteAsync("ActivateRoutingProfile", nameof(RoutingProfile), id.ToString(), AppJson.Serialize(new { id }), null, cancellationToken);
    }

    public async Task RotateTagPoolOffsetAsync(Guid tagId, int step, CancellationToken cancellationToken = default)
    {
        await _configSnapshotService.CreateSnapshotAsync("RoutingRotate", $"Rotate tag {tagId}", cancellationToken);

        RoutingProfile profile = await GetCurrentRoutingProfileEntityAsync(cancellationToken);
        RoutingProfileTagState tagState = await _dbContext.RoutingProfileTagStates
            .SingleAsync(x => x.RoutingProfileId == profile.Id && x.TagId == tagId, cancellationToken);

        tagState.RotationOffset += step;
        tagState.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("RotateTagPoolOffset", nameof(RoutingProfileTagState), tagState.Id.ToString(), AppJson.Serialize(new { tagId, step }), null, cancellationToken);
    }

    public async Task<QrBindingOverrideResponse> SetQrBindingOverrideAsync(QrBindingOverrideRequest request, CancellationToken cancellationToken = default)
    {
        await _configSnapshotService.CreateSnapshotAsync("RoutingOverride", $"Override QR {request.QrCodeId}", cancellationToken);

        QrBindingOverride overrideEntity = new()
        {
            QrCodeId = request.QrCodeId,
            QuestionId = request.QuestionId,
            ScopeProfileId = request.ScopeProfileId,
            IsActive = request.IsActive,
            Reason = request.Reason,
            CreatedOn = _clock.UtcNow,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QrBindingOverrides.AddAsync(overrideEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SetQrBindingOverride", nameof(QrBindingOverride), overrideEntity.Id.ToString(), AppJson.Serialize(request), request.Reason, cancellationToken);

        return new QrBindingOverrideResponse(
            overrideEntity.Id,
            overrideEntity.QrCodeId,
            overrideEntity.QuestionId,
            overrideEntity.ScopeProfileId,
            overrideEntity.IsActive,
            overrideEntity.Reason);
    }

    public async Task ClearQrBindingOverrideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        QrBindingOverride overrideEntity = await _dbContext.QrBindingOverrides.SingleAsync(x => x.Id == id, cancellationToken);
        overrideEntity.IsActive = false;
        overrideEntity.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("ClearQrBindingOverride", nameof(QrBindingOverride), id.ToString(), AppJson.Serialize(new { id }), null, cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingPreviewRowResponse>> PreviewRoutingMatrixAsync(CancellationToken cancellationToken = default)
    {
        List<QrCode> qrCodes = await _dbContext.QrCodes
            .AsNoTracking()
            .Include(x => x.Tag)
            .OrderBy(x => x.Tag.SortOrder)
            .ThenBy(x => x.Label)
            .ToListAsync(cancellationToken);

        List<RoutingPreviewRowResponse> rows = [];
        foreach (QrCode qrCode in qrCodes)
        {
            QuestionRoutingResolution resolution = await _questionRoutingResolver.ResolveAsync(qrCode.Id, cancellationToken);
            rows.Add(
                new RoutingPreviewRowResponse(
                    qrCode.Id,
                    qrCode.Label,
                    qrCode.Slug,
                    qrCode.TagId,
                    qrCode.Tag.Name,
                    resolution.Question?.Id,
                    resolution.Question?.Title,
                    resolution.Result.ToString()));
        }

        return rows;
    }

    public async Task<IReadOnlyList<EnigmaProfileResponse>> GetEnigmaProfilesAsync(CancellationToken cancellationToken = default)
    {
        List<EnigmaProfile> profiles = await _dbContext.EnigmaProfiles
            .AsNoTracking()
            .Include(x => x.RotorDefinitions.OrderBy(r => r.DisplayOrder))
            .ThenInclude(x => x.Tag)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return profiles.Select(ToEnigmaProfileResponse).ToList();
    }

    public async Task<EnigmaProfileResponse> CreateEnigmaProfileAsync(EnigmaProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        EnigmaProfile profile = new()
        {
            Name = request.Name.Trim(),
            Mode = ParseEnigmaMode(request.Mode),
            IsActive = request.IsActive,
            AttemptCooldownMinutes = request.AttemptCooldownMinutes,
            SuccessMessage = request.SuccessMessage,
            FailureMessage = request.FailureMessage,
            SecretCombinationJson = AppJson.Serialize(request.SecretCombination),
            ConfigJson = "{}",
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
            RotorDefinitions = request.Rotors
                .Select(
                    x => new EnigmaRotorDefinition
                    {
                        TagId = x.TagId,
                        Label = x.Label,
                        ColorOverride = x.ColorOverride,
                        DisplayOrder = x.DisplayOrder,
                        PositionMin = x.PositionMin,
                        PositionMax = x.PositionMax,
                        IsActive = x.IsActive,
                        CreatedAt = _clock.UtcNow,
                        UpdatedAt = _clock.UtcNow,
                    })
                .ToList(),
        };

        if (request.IsActive)
        {
            await ActivateEnigmaProfileInternalAsync(profile, cancellationToken);
        }

        await _dbContext.EnigmaProfiles.AddAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("CreateEnigmaProfile", nameof(EnigmaProfile), profile.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToEnigmaProfileResponse(profile);
    }

    public async Task<EnigmaProfileResponse> UpdateEnigmaProfileAsync(Guid id, EnigmaProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        // Do not Include rotors: loading them into the tracker then deleting/re-adding led to EnigmaRotorDefinition:Modified
        // on the second SaveChanges (UPDATE with stale concurrency token / identity map). Bulk-delete via SQL instead.
        EnigmaProfile profile = await _dbContext.EnigmaProfiles
            .SingleAsync(x => x.Id == id, cancellationToken);

        profile.Name = request.Name.Trim();
        profile.Mode = ParseEnigmaMode(request.Mode);
        profile.IsActive = request.IsActive;
        profile.AttemptCooldownMinutes = request.AttemptCooldownMinutes;
        profile.SuccessMessage = request.SuccessMessage;
        profile.FailureMessage = request.FailureMessage;
        profile.SecretCombinationJson = AppJson.Serialize(request.SecretCombination);
        profile.UpdatedAt = _clock.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _dbContext.EnigmaRotorDefinitions
                .Where(r => r.EnigmaProfileId == id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (EnigmaRotorDefinitionRequest x in request.Rotors)
            {
                profile.RotorDefinitions.Add(
                    new EnigmaRotorDefinition
                    {
                        EnigmaProfileId = profile.Id,
                        TagId = x.TagId,
                        Label = x.Label,
                        ColorOverride = x.ColorOverride,
                        DisplayOrder = x.DisplayOrder,
                        PositionMin = x.PositionMin,
                        PositionMax = x.PositionMax,
                        IsActive = x.IsActive,
                        CreatedAt = _clock.UtcNow,
                        UpdatedAt = _clock.UtcNow,
                    });
            }

            if (request.IsActive)
            {
                await ActivateEnigmaProfileInternalAsync(profile, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await _auditWriter.WriteAsync("UpdateEnigmaProfile", nameof(EnigmaProfile), profile.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToEnigmaProfileResponse(profile);
    }

    public async Task ActivateEnigmaProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnigmaProfile profile = await _dbContext.EnigmaProfiles.SingleAsync(x => x.Id == id, cancellationToken);
        await ActivateEnigmaProfileInternalAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("ActivateEnigmaProfile", nameof(EnigmaProfile), id.ToString(), AppJson.Serialize(new { id }), null, cancellationToken);
    }

    public async Task<GlobalSettingsResponse> GetGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        GlobalSettings settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        return ToGlobalSettingsResponse(settings);
    }

    public async Task<GlobalSettingsResponse> UpdateGlobalSettingsAsync(GlobalSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MaxTeamMembers is < 1 or > 100)
        {
            throw new AppException(400, "Лимит участников команды должен быть от 1 до 100.");
        }

        GlobalSettings settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        settings.AnswerCooldownMinutes = request.AnswerCooldownMinutes;
        settings.EnigmaCooldownMinutes = request.EnigmaCooldownMinutes;
        settings.MaxTeamMembers = request.MaxTeamMembers;
        settings.DefaultAnswerNormalization = request.DefaultAnswerNormalization;
        settings.CurrentQuestDayStateId = request.CurrentQuestDayStateId;
        settings.CurrentRoutingProfileId = request.CurrentRoutingProfileId;
        settings.CurrentEnigmaProfileId = request.CurrentEnigmaProfileId;
        settings.FlagsJson = request.FlagsJson;
        settings.Timezone = request.Timezone;
        settings.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateGlobalSettings", nameof(GlobalSettings), settings.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToGlobalSettingsResponse(settings);
    }

    private async Task ActivateRoutingProfileInternalAsync(RoutingProfile profile, CancellationToken cancellationToken)
    {
        // Bulk-deactivate other profiles in SQL so we do not attach every row with a possibly stale Version
        // (tracked updates + int concurrency tokens caused DbUpdateConcurrencyException on SaveChanges).
        DateTimeOffset now = _clock.UtcNow;
        await _dbContext.RoutingProfiles
            .Where(x => x.Id != profile.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.Version, x => x.Version + 1),
                cancellationToken);

        profile.IsActive = true;
        profile.ActivatedAt = now;
        profile.UpdatedAt = now;

        GlobalSettings settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        settings.CurrentRoutingProfileId = profile.Id;
        settings.UpdatedAt = now;
    }

    private async Task ActivateEnigmaProfileInternalAsync(EnigmaProfile profile, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;
        await _dbContext.EnigmaProfiles
            .Where(x => x.Id != profile.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.Version, x => x.Version + 1),
                cancellationToken);

        profile.IsActive = true;
        profile.UpdatedAt = now;

        GlobalSettings settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        settings.CurrentEnigmaProfileId = profile.Id;
        settings.UpdatedAt = now;
    }

    private async Task<RoutingProfile> GetCurrentRoutingProfileEntityAsync(CancellationToken cancellationToken)
    {
        GlobalSettings settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        RoutingProfile? profile = null;

        if (settings.CurrentRoutingProfileId is Guid profileId)
        {
            profile = await _dbContext.RoutingProfiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        }

        profile ??= await _dbContext.RoutingProfiles
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .FirstAsync(cancellationToken);

        return profile;
    }

    private async Task<GlobalSettings> GetOrCreateGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        GlobalSettings? settings = await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new GlobalSettings
        {
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.GlobalSettings.AddAsync(settings, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static QuestionStatus ParseQuestionStatus(string status) =>
        Enum.TryParse<QuestionStatus>(status, true, out QuestionStatus parsed)
            ? parsed
            : QuestionStatus.Draft;

    private static QuestionSelectionMode ParseSelectionMode(string selectionMode) =>
        Enum.TryParse<QuestionSelectionMode>(selectionMode, true, out QuestionSelectionMode parsed)
            ? parsed
            : QuestionSelectionMode.PoolSlotRotation;

    private static EnigmaMode ParseEnigmaMode(string mode) =>
        Enum.TryParse<EnigmaMode>(mode, true, out EnigmaMode parsed)
            ? parsed
            : EnigmaMode.SimpleCombination;

    private static AnswerSchema ToAnswerSchema(QuestionAnswerSchemaDto dto) =>
        new()
        {
            Kind = Enum.TryParse<AnswerValidationKind>(dto.Kind, true, out AnswerValidationKind parsedKind)
                ? parsedKind
                : AnswerValidationKind.NormalizedText,
            AcceptedAnswers = dto.AcceptedAnswers.ToList(),
            ExpectedNumericValue = dto.ExpectedNumericValue,
            NumericTolerance = dto.NumericTolerance,
            Normalization = new()
            {
                TrimWhitespace = dto.TrimWhitespace,
                IgnoreCase = dto.IgnoreCase,
                CollapseInnerWhitespace = dto.CollapseInnerWhitespace,
                RemovePunctuation = dto.RemovePunctuation,
            },
        };

    private static QuestionAnswerSchemaDto ToAnswerSchemaDto(AnswerSchema schema) =>
        new(
            schema.Kind.ToString(),
            schema.AcceptedAnswers,
            schema.ExpectedNumericValue,
            schema.NumericTolerance,
            schema.Normalization.TrimWhitespace,
            schema.Normalization.IgnoreCase,
            schema.Normalization.CollapseInnerWhitespace,
            schema.Normalization.RemovePunctuation);

    private static QuestionResponse ToQuestionResponse(Question question) =>
        new(
            question.Id,
            question.TagId,
            question.Title,
            question.BodyRichText,
            question.FooterHint,
            question.ImageUrl,
            question.Status.ToString(),
            question.IsActive,
            question.IsArchived,
            question.SupportNotes,
            ToAnswerSchemaDto(question.AnswerSchema));

    private static QuestionPoolResponse ToPoolResponse(QuestionPool pool) =>
        new(
            pool.Id,
            pool.TagId,
            pool.Name,
            pool.IsActive,
            pool.IsArchived,
            pool.Description,
            pool.SortOrder,
            pool.Entries
                .OrderBy(x => x.Position)
                .Select(x => new QuestionPoolEntryResponse(x.Id, x.QuestionId, x.Position, x.IsEnabled, x.Notes))
                .ToList());

    private static RoutingProfileResponse ToRoutingProfileResponse(RoutingProfile profile) =>
        new(
            profile.Id,
            profile.Name,
            profile.IsActive,
            profile.Description,
            profile.TagStates
                .OrderBy(x => x.TagId)
                .Select(
                    x => new RoutingProfileTagStateResponse(
                        x.Id,
                        x.TagId,
                        x.ActivePoolId,
                        x.RotationOffset,
                        x.SelectionMode.ToString(),
                        x.IsEnabled))
                .ToList());

    private static EnigmaProfileResponse ToEnigmaProfileResponse(EnigmaProfile profile)
    {
        Dictionary<Guid, int> secretCombination = AppJson.Deserialize<Dictionary<Guid, int>>(profile.SecretCombinationJson) ?? [];

        return new EnigmaProfileResponse(
            profile.Id,
            profile.Name,
            profile.Mode.ToString(),
            profile.IsActive,
            profile.AttemptCooldownMinutes,
            profile.SuccessMessage,
            profile.FailureMessage,
            secretCombination,
            profile.RotorDefinitions
                .OrderBy(x => x.DisplayOrder)
                .Select(
                    x => new EnigmaRotorDefinitionDto(
                        x.Id,
                        x.TagId,
                        x.Tag?.Name ?? string.Empty,
                        x.ColorOverride ?? x.Tag?.Color ?? "#000000",
                        x.ColorOverride,
                        x.Label,
                        x.DisplayOrder,
                        x.PositionMin,
                        x.PositionMax,
                        x.IsActive))
                .ToList());
    }

    private static GlobalSettingsResponse ToGlobalSettingsResponse(GlobalSettings settings) =>
        new(
            settings.Id,
            settings.AnswerCooldownMinutes,
            settings.EnigmaCooldownMinutes,
            settings.MaxTeamMembers,
            settings.DefaultAnswerNormalization,
            settings.CurrentQuestDayStateId,
            settings.CurrentRoutingProfileId,
            settings.CurrentEnigmaProfileId,
            settings.FlagsJson,
            settings.Timezone);
}
