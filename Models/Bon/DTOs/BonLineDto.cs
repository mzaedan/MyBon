namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One item line the Create form paints again after a rejected POST: the stock item as it stands now
/// plus the quantity that was typed, so a validation error never drops what was already picked.
/// </summary>
public class BonLineDto
{
    public int BarangId { get; set; }

    public string Nama { get; set; } = null!;

    public string? Satuan { get; set; }

    public decimal Harga { get; set; }

    public int Jumlah { get; set; }
}
