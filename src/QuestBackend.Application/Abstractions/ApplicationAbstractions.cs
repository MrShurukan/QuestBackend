using Microsoft.EntityFrameworkCore;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Application.Abstractions;

public interface IQuestDbContext
{
    DbSet<AdminUser> AdminUsers { get; }

    DbSet<ParticipantUser> ParticipantUsers { get; }

    DbSet<Team> Teams { get; }

    DbSet<TeamMembership> TeamMemberships { get; }

    DbSet<QuestionTag> QuestionTags { get; }

    DbSet<Question> Questions { get; }

    DbSet<QuestionPool> QuestionPools { get; }

    DbSet<QuestionPoolEntry> QuestionPoolEntries { get; }

    DbSet<QrCode> QrCodes { get; }

    DbSet<RoutingProfile> RoutingProfiles { get; }

    DbSet<RoutingProfileTagState> RoutingProfileTagStates { get; }

    DbSet<QrBindingOverride> QrBindingOverrides { get; }

    DbSet<QuestDayState> QuestDayStates { get; }

    DbSet<TeamQuestionState> TeamQuestionStates { get; }

    DbSet<TeamAnswerAttempt> TeamAnswerAttempts { get; }

    DbSet<QrScanEvent> QrScanEvents { get; }

    DbSet<EnigmaProfile> EnigmaProfiles { get; }

    DbSet<EnigmaRotorDefinition> EnigmaRotorDefinitions { get; }

    DbSet<TeamRotorReward> TeamRotorRewards { get; }

    DbSet<EnigmaAttempt> EnigmaAttempts { get; }

    DbSet<TeamEnigmaDraft> TeamEnigmaDrafts { get; }

    DbSet<GlobalSettings> GlobalSettings { get; }

    DbSet<AdminAuditLog> AdminAuditLogs { get; }

    DbSet<ConfigSnapshot> ConfigSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentPrincipal
{
    Guid? AdminUserId { get; }

    Guid? ParticipantUserId { get; }

    string? ParticipantDisplayName { get; }

    bool IsAdminAuthenticated { get; }

    bool IsParticipantAuthenticated { get; }

    string? CorrelationId { get; }
}

public interface IExternalParticipantAuthProvider
{
    string ProviderName { get; }

    Task<ExternalParticipantIdentity> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public interface ISlugGenerator
{
    string Generate(int length = 8);
}

public interface IAuditWriter
{
    Task WriteAsync(string actionType, string entityType, string entityId, string diffJson, string? reason, CancellationToken cancellationToken = default);
}

public interface IConfigSnapshotService
{
    Task<ConfigSnapshot> CreateSnapshotAsync(string snapshotType, string? comment, CancellationToken cancellationToken = default);
}

public interface IQuestionRoutingResolver
{
    Task<QuestionRoutingResolution> ResolveAsync(Guid qrCodeId, CancellationToken cancellationToken = default);
}

public interface IAnswerEvaluator
{
    AnswerEvaluationResult Evaluate(Question question, string rawAnswer);
}

public interface IEnigmaEvaluator
{
    EnigmaEvaluationResult Evaluate(EnigmaProfile profile, IReadOnlyDictionary<Guid, int> rotorPositions);
}

public interface IQuestDayLifecycleGate
{
    Task<QuestLifecycleDecision> GetDecisionAsync(CancellationToken cancellationToken = default);
}

public sealed record ExternalParticipantIdentity(string Provider, string ProviderSubject, string DisplayName, string? AvatarUrl);

public sealed record QuestionRoutingResolution(
    QrScanResolutionResult Result,
    QrCode? QrCode,
    Question? Question,
    string Message);

public sealed record AnswerEvaluationResult(
    bool IsCorrect,
    string NormalizedAnswer,
    string EvaluationSnapshotJson);

public sealed record EnigmaEvaluationResult(
    bool IsSuccess,
    string EvaluationSnapshotJson);

public sealed record QuestLifecycleDecision(
    QuestDayStatus Status,
    string Message,
    QuestDayState QuestDayState,
    bool AllowsUnlock,
    bool AllowsSubmissions);
