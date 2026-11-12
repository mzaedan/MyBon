using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Auth.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Services;

/// <summary>
/// Verifies sign-in credentials against the local <c>pengguna</c> table.
/// </summary>
public class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<Pengguna> passwordHasher,
    ILogger<AuthService> logger) : IAuthService
{
    /// <summary>
    /// One message for every failure: a missing account, a disabled one, and a wrong password all
    /// answer the same, so the form cannot be used to discover which usernames exist.
    /// </summary>
    private const string InvalidCredentialsMessage = "Username atau password salah";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<Pengguna> _passwordHasher = passwordHasher;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<ErrorOr<Pengguna>> MasukAsync(LoginRequest request)
    {
        var username = NormalizeUsername(request.Username);

        var pengguna = await _dbContext.Penggunas
            .FirstOrDefaultAsync(p => p.Username == username);

        if (pengguna is null || !pengguna.Aktif || string.IsNullOrEmpty(request.Password))
            return Reject(username);

        var verification = _passwordHasher.VerifyHashedPassword(
            pengguna, pengguna.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
            return Reject(username);

        // An old hash upgrades on the next successful sign-in, so the stored format never ages out.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            pengguna.PasswordHash = _passwordHasher.HashPassword(pengguna, request.Password);

        pengguna.TerakhirMasuk = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ErrorOr<Pengguna>.CreateSuccess("Berhasil masuk", pengguna);
    }

    /// <summary>Sign-in key is stored lowercase so a typed uppercase letter still matches.</summary>
    private static string NormalizeUsername(string? username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    private ErrorOr<Pengguna> Reject(string username)
    {
        // Only the username reaches the log; the password and the hash never do.
        _logger.LogWarning("Percobaan masuk gagal untuk username {Username}", username);

        return ErrorOr<Pengguna>.CreateFailure(InvalidCredentialsMessage, ErrorType.Unauthorized);
    }
}