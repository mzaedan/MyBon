using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Bon.Requests;

/// <summary>
/// One pick from the Tambah barang sheet on Details: the bon the line belongs to and the stock item
/// the shopkeeper tapped. One pick writes one unit. The price is never posted — the service copies
/// it, together with the name and unit, from the stock list.
/// </summary>
public class AddBonItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Bon wajib dipilih")]
    public int BonId { get; set; }

    [Required(ErrorMessage = "Barang wajib dipilih")]
    public int? BarangId { get; set; }
}
