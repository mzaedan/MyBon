using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Barang.Requests;

/// <summary>
/// Form input for writing a new item into the stock list.
/// </summary>
public class CreateBarangRequest
{
    [Required(ErrorMessage = "Nama barang wajib diisi")]
    [MaxLength(200, ErrorMessage = "Nama barang maksimal 200 karakter")]
    public string Nama { get; set; } = null!;

    [Required(ErrorMessage = "Satuan wajib diisi")]
    [MaxLength(30, ErrorMessage = "Satuan maksimal 30 karakter")]
    public string Satuan { get; set; } = null!;

    /// <summary>
    /// Price as typed: "78.000" or "78,5". Kept as text because form binding parses a decimal with the
    /// server culture, which would read "78.000" as 78. The service owns the parsing.
    /// </summary>
    [Required(ErrorMessage = "Harga wajib diisi")]
    [MaxLength(20, ErrorMessage = "Harga maksimal 20 karakter")]
    public string Harga { get; set; } = null!;
}
