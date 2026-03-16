using System.Security.Claims;
using QuestBackend.Application.Shared;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Participants;

namespace QuestBackend.Api.Common;

internal static class AuthPrincipalFactory
{
    public static ClaimsPrincipal CreateAdminPrincipal(AdminUser admin)
    {
        Claim[] claims =
        [
            new(QuestAuthConstants.AdminIdClaim, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Login),
            new(ClaimTypes.Role, admin.Role.ToString()),
        ];

        ClaimsIdentity identity = new(claims, QuestAuthConstants.AdminScheme);
        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal CreateParticipantPrincipal(ParticipantUser participant)
    {
        Claim[] claims =
        [
            new(QuestAuthConstants.ParticipantIdClaim, participant.Id.ToString()),
            new(QuestAuthConstants.DisplayNameClaim, participant.DisplayName),
            new(ClaimTypes.Name, participant.DisplayName),
        ];

        ClaimsIdentity identity = new(claims, QuestAuthConstants.ParticipantScheme);
        return new ClaimsPrincipal(identity);
    }
}
