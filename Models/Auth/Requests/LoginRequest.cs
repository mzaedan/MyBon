using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Auth.Requests;

/// <summary>
/// Form input for the sign-in screen.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username wajib diisi")]
    [MaxLength(50, ErrorMessage = "Username maksimal 50 karakter")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Kata sandi wajib diisi")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    /// <summary>Keeps the cookie after the browser closes.</summary>
    public bool IngatSaya { get; set; }

    /// <summary>Local page the user asked for before being sent to the sign-in screen.</summary>
    public string? ReturnUrl { get; set; }
}