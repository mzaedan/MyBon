using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Auth.Requests;

/// <summary>
/// Form input for a signed-in user changing their own password.
/// </summary>
public class GantiPasswordRequest
{
    [Required(ErrorMessage = "Kata sandi lama wajib diisi")]
    [DataType(DataType.Password)]
    public string PasswordLama { get; set; } = null!;

    [Required(ErrorMessage = "Kata sandi baru wajib diisi")]
    [MinLength(8, ErrorMessage = "Kata sandi baru minimal 8 karakter")]
    [DataType(DataType.Password)]
    public string PasswordBaru { get; set; } = null!;

    [Required(ErrorMessage = "Konfirmasi kata sandi wajib diisi")]
    [Compare(nameof(PasswordBaru), ErrorMessage = "Konfirmasi kata sandi tidak sama")]
    [DataType(DataType.Password)]
    public string KonfirmasiPassword { get; set; } = null!;
}