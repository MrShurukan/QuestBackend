using QuestBackend.Application.Abstractions;

namespace QuestBackend.Infrastructure.Auth;

public sealed class DisabledExternalParticipantAuthProvider : IExternalParticipantAuthProvider
{
    public string ProviderName => "disabled";

    public Task<ExternalParticipantIdentity> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("External participant authentication is not configured in this environment.");
    }
}
