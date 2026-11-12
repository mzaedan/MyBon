using MyBon.Domain.Enums;

namespace MyBon.Models.Pengguna.DTOs;

/// <summary>
/// One account as the management list paints it. Carries no password hash, so a list or a JSON
/// response can never expose the credential.
/// </summary>
public class PenggunaDto
{
    public int Id { get; set; }

    public string Nama { get; set; } = null!;

    public string Username { get; set; } = null!;

    public PeranPengguna Peran { get; set; }

    public int? PelangganId { get; set; }

    public bool Aktif { get; set; }

    public DateTime? TerakhirMasuk { get; set; }

    public bool IsAdmin => Peran == PeranPengguna.Admin;
}