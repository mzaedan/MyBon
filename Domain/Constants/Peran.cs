using MyBon.Domain.Enums;

namespace MyBon.Domain.Constants;

/// <summary>
/// Role names handed to <c>[Authorize(Roles = ...)]</c>. The attribute needs a compile-time
/// constant, so the names live here and mirror <see cref="PeranPengguna"/>.
/// </summary>
public static class Peran
{
    /// <summary>Role of the shop owner: every screen and every write action.</summary>
    public const string Admin = nameof(PeranPengguna.Admin);

    /// <summary>Role of a customer: their own bons, read-only.</summary>
    public const string Pelanggan = nameof(PeranPengguna.Pelanggan);
}