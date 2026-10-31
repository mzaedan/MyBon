using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Bon.Requests;

/// <summary>
/// One pick from the Tambah barang sheet on Details: the bon the line belongs to, the stock item the
/// shopkeeper tapped, and how many units the row's stepper showed. The price is never posted — the
/// service copies it, together with the name and unit, from the stock list.
/// </summary>
public class AddBonItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Bon wajib dipilih")]
    public int BonId { get; set; }

    [Required(ErrorMessage = "Barang wajib dipilih")]
    public int? BarangId { get; set; }

    /// <summary>Units the pick writes; a request without a stepper still posts one unit.</summary>
    [Range(1, 9999, ErrorMessage = "Jumlah minimal 1 dan maksimal 9999")]
    public int Jumlah { get; set; } = 1;
}
