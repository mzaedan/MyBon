using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Bon.Requests;

/// <summary>
/// Form input for writing a new bon: the customer, the day it is written, the optional note, the
/// money paid up front, and the item lines recorded under it.
/// </summary>
public class CreateBonRequest
{
    [Required(ErrorMessage = "Pelanggan wajib dipilih")]
    public int? PelangganId { get; set; }

    [Required(ErrorMessage = "Tanggal wajib diisi")]
    public DateOnly? Tanggal { get; set; }

    public string? Catatan { get; set; }

    /// <summary>
    /// Money paid when the page is written, as typed: "50.000" or "50,5". Kept as text because form
    /// binding parses a decimal with the server culture, which would read "50.000" as 50. An empty
    /// field means nothing has been paid yet. The service owns the parsing.
    /// </summary>
    [MaxLength(20, ErrorMessage = "Total bayar maksimal 20 karakter")]
    public string? TotalBayar { get; set; }

    public List<CreateBonItemRequest> Items { get; set; } = [];
}
