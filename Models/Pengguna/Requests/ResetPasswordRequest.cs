using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Pengguna.Requests;

/// <summary>
/// Form input for an admin setting a new password on an existing account.
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    public int PenggunaId { get; set; }

    [Required(ErrorMessage = "Kata sandi baru wajib diisi")]
    [MinLength(8, ErrorMessage = "Kata sandi baru minimal 8 karakter")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}