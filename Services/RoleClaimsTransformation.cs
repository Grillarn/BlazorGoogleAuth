using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace BlazorGoogleAuth.Services;

/// <summary>
/// Körs varje gång ClaimsPrincipal byggs upp (dvs. efter att Google-inloggningen
/// satt sina egna claims). Här slår vi i databasen (via IUserRoleService) och
/// mappar in ClaimTypes.Role, så att vanliga [Authorize(Roles = "...")] och
/// AuthorizeView Roles="..." fungerar precis som med t.ex. ASP.NET Identity.
///
/// Detta är också stället där en ny användare provisioneras i databasen
/// automatiskt, första gången den loggar in med Google.
/// </summary>
public class RoleClaimsTransformation : IClaimsTransformation
{
    private readonly IUserRoleService _userRoleService;

    public RoleClaimsTransformation(IUserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return principal;
        }

        // Undvik att lägga till samma roller flera gånger om transformationen
        // körs mer än en gång under samma request/circuit.
        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return principal;
        }

        var email = identity.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            return principal;
        }

        var displayName = identity.FindFirst(ClaimTypes.Name)?.Value;
        var roles = await _userRoleService.GetOrProvisionRolesAsync(email, displayName);

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return principal;
    }
}
