using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Barang.Requests;

/// <summary>
/// Form input for editing an existing item in the stock list.
/// </summary>
public class UpdateBarangRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama barang wajib diisi")]
    [MaxLength(200, ErrorMessage = "Nama barang maksimal 200 karakter")]
    public string Nama { get; set; } = null!;

    [MaxLength(30, ErrorMessage = "Satuan maksimal 30 karakter")]
    public string? Satuan { get; set; }

    /// <summary>
    /// Price as typed: "78.000" or "78,5". Kept as text because form binding parses a decimal with the
    /// server culture, which would read "78.000" as 78. The service owns the parsing.
    /// </summary>
    [Required(ErrorMessage = "Harga wajib diisi")]
    [MaxLength(20, ErrorMessage = "Harga maksimal 20 karakter")]
    public string Harga { get; set; } = null!;
}
