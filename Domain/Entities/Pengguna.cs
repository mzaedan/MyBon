using MyBon.Domain.Abstractions;
using MyBon.Domain.Enums;

namespace MyBon.Domain.Entities;

/// <summary>
/// One account that can sign in. An <see cref="PeranPengguna.Admin"/> account carries no customer
/// row; a <see cref="PeranPengguna.Pelanggan"/> account is tied to exactly one <see cref="Pelanggan"/>
/// and may only read that customer's bons.
/// </summary>
public class Pengguna : BaseEntity
{
    public int Id { get; set; }

    /// <summary>Sign-in key, stored lowercase so a typed uppercase letter still matches.</summary>
    public string Username { get; set; } = null!;

    /// <summary>Name painted in the shell header.</summary>
    public string Nama { get; set; } = null!;

    /// <summary>PBKDF2 hash produced by <c>IPasswordHasher&lt;Pengguna&gt;</c>; never the plaintext.</summary>
    public string PasswordHash { get; set; } = null!;

    public PeranPengguna Peran { get; set; }

    /// <summary>Customer this account reads, or <c>null</c> for an admin account.</summary>
    public int? PelangganId { get; set; }

    public Pelanggan? Pelanggan { get; set; }

    /// <summary>Whether the account may sign in. A disabled account keeps its bons and its history.</summary>
    public bool Aktif { get; set; } = true;

    public DateTime? TerakhirMasuk { get; set; }
}