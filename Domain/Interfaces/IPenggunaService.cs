using MyBon.Models.Auth.Requests;
using MyBon.Models.Common;
using MyBon.Models.Pengguna.DTOs;
using MyBon.Models.Pengguna.Requests;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Account management for the admin. Every read returns a <see cref="PenggunaDto"/> so the password
/// hash never leaves the database.
/// </summary>
public interface IPenggunaService
{
    /// <summary>Lists every account, admin rows first, then by name.</summary>
    Task<ErrorOr<List<PenggunaDto>>> DaftarAsync();

    /// <summary>Reads one account for the management screens.</summary>
    Task<ErrorOr<PenggunaDto>> GetByIdAsync(int id);

    /// <summary>Reads the account tied to <paramref name="pelangganId"/>, or <c>NotFound</c> when the customer has none.</summary>
    Task<ErrorOr<PenggunaDto>> GetByPelangganIdAsync(int pelangganId);

    /// <summary>
    /// Creates an admin account when <see cref="CreatePenggunaRequest.PelangganId"/> is empty, or the
    /// customer's account when it holds a value. A taken username and a customer who already has an
    /// account are reported as <c>Conflict</c>.
    /// </summary>
    Task<ErrorOr<PenggunaDto>> BuatAsync(CreatePenggunaRequest request);

    /// <summary>Sets a new password on an existing account.</summary>
    Task<ErrorOr> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Enables or disables an account. An admin cannot disable their own account, and the last active
    /// admin cannot be disabled.
    /// </summary>
    Task<ErrorOr> SetAktifAsync(int id, bool aktif);

    /// <summary>Changes the password of the signed-in account after verifying the old one.</summary>
    Task<ErrorOr> GantiPasswordAsync(GantiPasswordRequest request);
}