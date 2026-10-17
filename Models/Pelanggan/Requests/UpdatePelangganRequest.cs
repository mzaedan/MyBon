using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Pelanggan.Requests;

/// <summary>
/// Form input for editing an existing customer.
/// </summary>
public class UpdatePelangganRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(200, ErrorMessage = "Nama maksimal 200 karakter")]
    public string Nama { get; set; } = null!;

    [MaxLength(30, ErrorMessage = "Nomor HP maksimal 30 karakter")]
    public string? NomorHp { get; set; }

    public string? Alamat { get; set; }
}