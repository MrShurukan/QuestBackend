using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;

namespace QuestBackend.Application.Routing;

public sealed class QuestionRoutingResolver : IQuestionRoutingResolver
{
    private readonly IQuestDbContext _dbContext;

    public QuestionRoutingResolver(IQuestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuestionRoutingResolution> ResolveAsync(Guid qrCodeId, CancellationToken cancellationToken = default)
    {
        QrCode? qrCode = await _dbContext.QrCodes
            .AsNoTracking()
            .Include(x => x.Tag)
            .SingleOrDefaultAsync(x => x.Id == qrCodeId, cancellationToken);

        if (qrCode is null)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.NoPoolMatch, null, null, "QR code was not found.");
        }

        if (!qrCode.IsActive)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.InactiveQr, qrCode, null, "QR code is inactive.");
        }

        if (!qrCode.Tag.IsActive)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.InactiveTag, qrCode, null, "Tag is inactive.");
        }

        RoutingProfile? profile = await GetCurrentRoutingProfileAsync(cancellationToken);
        if (profile is null)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.NoPoolMatch, qrCode, null, "No active routing profile configured.");
        }

        QrBindingOverride? activeOverride = await _dbContext.QrBindingOverrides
            .AsNoTracking()
            .Where(x => x.QrCodeId == qrCode.Id && x.IsActive && (x.ScopeProfileId == null || x.ScopeProfileId == profile.Id))
            .OrderByDescending(x => x.Id)
            .Include(x => x.Question)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeOverride?.Question is { IsActive: true, IsArchived: false } overrideQuestion)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.OverrideResolved, qrCode, overrideQuestion, "Resolved by QR override.");
        }

        RoutingProfileTagState? tagState = await _dbContext.RoutingProfileTagStates
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoutingProfileId == profile.Id && x.TagId == qrCode.TagId, cancellationToken);

        if (tagState is null || !tagState.IsEnabled || tagState.ActivePoolId is null)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.NoPoolMatch, qrCode, null, "No active pool assigned for the tag.");
        }

        List<QuestionPoolEntry> entries = await _dbContext.QuestionPoolEntries
            .AsNoTracking()
            .Where(x => x.PoolId == tagState.ActivePoolId.Value && x.IsEnabled)
            .Include(x => x.Question)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);

        List<Question> activeQuestions = entries
            .Select(x => x.Question)
            .Where(x => x.IsActive && !x.IsArchived && x.Status == QuestionStatus.Active)
            .ToList();

        if (activeQuestions.Count == 0)
        {
            return new QuestionRoutingResolution(QrScanResolutionResult.NoPoolMatch, qrCode, null, "No active questions in the selected pool.");
        }

        int slot = Mod(qrCode.SlotIndex + tagState.RotationOffset, activeQuestions.Count);
        Question question = activeQuestions[slot];
        return new QuestionRoutingResolution(QrScanResolutionResult.Resolved, qrCode, question, "Resolved by pool rotation.");
    }

    private async Task<RoutingProfile?> GetCurrentRoutingProfileAsync(CancellationToken cancellationToken)
    {
        GlobalSettings? settings = await _dbContext.GlobalSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        RoutingProfile? profile = null;

        if (settings?.CurrentRoutingProfileId is Guid currentProfileId)
        {
            profile = await _dbContext.RoutingProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == currentProfileId, cancellationToken);
        }

        profile ??= await _dbContext.RoutingProfiles
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return profile;
    }

    private static int Mod(int value, int size)
    {
        int result = value % size;
        return result < 0 ? result + size : result;
    }
}
