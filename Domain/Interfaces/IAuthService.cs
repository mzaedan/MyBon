using MyBon.Domain.Entities;
using MyBon.Models.Auth.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Verifies sign-in credentials. The account itself is loaded by
/// <see cref="IPenggunaService"/> for account management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Checks the username and password and stamps the last sign-in. Every failure answers the same
    /// message with <c>ErrorType.Unauthorized</c>, so the form never hints which half was wrong.
    /// </summary>
    Task<ErrorOr<Pengguna>> MasukAsync(LoginRequest request);
}