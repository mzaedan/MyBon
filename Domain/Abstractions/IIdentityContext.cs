using MyBon.Domain.Enums;

namespace MyBon.Domain.Abstractions;

/// <summary>
/// The signed-in user for the current request, read once from the cookie claims. Services and views
/// depend on this instead of touching <c>HttpContext.User</c>.
/// </summary>
public interface IIdentityContext
{
    /// <summary>Account identifier, or <c>0</c> when nobody is signed in.</summary>
    int Id { get; }

    /// <summary>Sign-in key, or an empty string when nobody is signed in.</summary>
    string Username { get; }

    /// <summary>Name shown in the header, or an empty string when nobody is signed in.</summary>
    string FullName { get; }

    /// <summary>Email claim when the source provides one.</summary>
    string? Email { get; }

    /// <summary>Role carried by the cookie, or <c>null</c> for an anonymous request.</summary>
    PeranPengguna? Peran { get; }

    /// <summary>Customer the signed-in account reads, or <c>null</c> for an admin or anonymous request.</summary>
    int? PelangganId { get; }

    bool IsAuthenticated { get; }

    /// <summary>Whether the signed-in user holds <paramref name="peran"/>.</summary>
    bool IsInRole(string peran);
}