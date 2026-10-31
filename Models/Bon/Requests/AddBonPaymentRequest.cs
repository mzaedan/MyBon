using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Bon.Requests;

/// <summary>
/// One save from the Catat pembayaran sheet on Details: the bon the money belongs to, the amount as it
/// was typed, the day the money was handed over, and the optional note.
/// </summary>
public class AddBonPaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Bon wajib dipilih")]
    public int BonId { get; set; }

    /// <summary>
    /// Money handed over, as typed: "50.000" or "50,5". Kept as text because form binding parses a
    /// decimal with the server culture, which would read "50.000" as 50. The service owns the parsing.
    /// </summary>
    [MaxLength(20, ErrorMessage = "Jumlah bayar maksimal 20 karakter")]
    public string? Jumlah { get; set; }

    [Required(ErrorMessage = "Tanggal wajib diisi")]
    public DateOnly? Tanggal { get; set; }

    /// <summary>Optional note stored beside the payment; the column holds 200 characters.</summary>
    [MaxLength(200, ErrorMessage = "Catatan maksimal 200 karakter")]
    public string? Catatan { get; set; }
}
