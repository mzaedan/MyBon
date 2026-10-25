namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One row in the item picker: the stock item's name, unit, and current price. The price is shown
/// only; the service re-reads it from the stock list when the bon is saved.
/// </summary>
public class BonBarangOptionDto
{
    public int Id { get; set; }

    public string Nama { get; set; } = null!;

    public string? Satuan { get; set; }

    public decimal Harga { get; set; }
}
