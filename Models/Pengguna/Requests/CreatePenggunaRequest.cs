using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Pengguna.Requests;

/// <summary>
/// Form input for creating an account. <see cref="PelangganId"/> empty creates an admin account;
/// a value creates the customer account tied to that customer.
/// </summary>
public class CreatePenggunaRequest
{
    [Required(ErrorMessage = "Username wajib diisi")]
    [MaxLength(50, ErrorMessage = "Username maksimal 50 karakter")]
    [RegularExpression(
        @"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username hanya huruf, angka, titik, garis bawah, atau strip")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(200, ErrorMessage = "Nama maksimal 200 karakter")]
    public string Nama { get; set; } = null!;

    [Required(ErrorMessage = "Kata sandi wajib diisi")]
    [MinLength(8, ErrorMessage = "Kata sandi minimal 8 karakter")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    /// <summary>Customer the account belongs to, or <c>null</c> for an admin account.</summary>
    public int? PelangganId { get; set; }
}