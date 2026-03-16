using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;

namespace QuestBackend.Infrastructure.Http;

public sealed class HttpContextCurrentPrincipal : ICurrentPrincipal
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentPrincipal(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? AdminUserId => GetGuidClaim(QuestAuthConstants.AdminIdClaim);

    public Guid? ParticipantUserId => GetGuidClaim(QuestAuthConstants.ParticipantIdClaim);

    public string? ParticipantDisplayName => _httpContextAccessor.HttpContext?.User.FindFirstValue(QuestAuthConstants.DisplayNameClaim);

    public bool IsAdminAuthenticated => _httpContextAccessor.HttpContext?.User.HasClaim(x => x.Type == QuestAuthConstants.AdminIdClaim) == true;

    public bool IsParticipantAuthenticated => _httpContextAccessor.HttpContext?.User.HasClaim(x => x.Type == QuestAuthConstants.ParticipantIdClaim) == true;

    public string? CorrelationId => _httpContextAccessor.HttpContext?.TraceIdentifier;

    private Guid? GetGuidClaim(string claimType)
    {
        string? value = _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out Guid id) ? id : null;
    }
}
