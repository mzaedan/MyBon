using System.Security.Claims;
using MyBon.Domain.Abstractions;
using MyBon.Domain.Enums;

namespace MyBon.Infrastructure.Contexts;

/// <summary>
/// Reads the signed-in user from the cookie claims once per request, so every service and view
/// works from one snapshot instead of re-reading <c>HttpContext.User</c>.
/// </summary>
public sealed class Context : IContext
{
    public Context(IHttpContextAccessor accessor)
        : this(accessor.HttpContext?.User)
    {
    }

    internal Context(ClaimsPrincipal? user)
    {
        Identity = new IdentityContext(user);
    }

    public IIdentityContext Identity { get; }
}

/// <summary>
/// Claim-backed identity. Every getter answers a safe default for an anonymous request, and
/// <see cref="Peran"/> stays <c>null</c> so nobody is ever treated as an admin by accident.
/// </summary>
internal sealed class IdentityContext(ClaimsPrincipal? user) : IIdentityContext
{
    private const string UsernameClaim = "usr";
    private const string PelangganIdClaim = "pid";

    public int Id => int.TryParse(FindValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string Username => FindValue(UsernameClaim) ?? string.Empty;

    public string FullName => FindValue(ClaimTypes.Name) ?? string.Empty;

    public string? Email => FindValue(ClaimTypes.Email);

    public PeranPengguna? Peran =>
        Enum.TryParse<PeranPengguna>(FindValue(ClaimTypes.Role), out var peran) ? peran : null;

    public int? PelangganId =>
        int.TryParse(FindValue(PelangganIdClaim), out var id) ? id : null;

    public bool IsAuthenticated => user?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string peran) => user?.IsInRole(peran) == true;

    private string? FindValue(string claimType) => user?.FindFirst(claimType)?.Value;
}