using System.ComponentModel.DataAnnotations;

namespace MyBon.Models.Bon.Requests;

/// <summary>
/// One item line inside <see cref="CreateBonRequest"/>. The price is never posted: the service reads
/// it from the stock list, so a tampered page cannot set its own price.
/// </summary>
public class CreateBonItemRequest
{
    [Required(ErrorMessage = "Barang wajib dipilih")]
    public int? BarangId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal 1")]
    public int Jumlah { get; set; } = 1;
}
