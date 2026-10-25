using MyBon.Domain.Abstractions;

namespace MyBon.Domain.Entities;

/// <summary>
/// One item line written under a bon. The name, unit and price are copied at the moment the line is
/// written, so changing a master price later never rewrites an old page.
/// </summary>
public class BonItem : BaseEntity
{
    public int Id { get; set; }

    public int BonId { get; set; }

    public Bon? Bon { get; set; }

    /// <summary>
    /// Null once the stock item is deleted. The snapshot fields below keep the line readable, so
    /// deleting an item never erases an old page.
    /// </summary>
    public int? BarangId { get; set; }

    public Barang? Barang { get; set; }

    /// <summary>Item name copied from the stock list when the line was written.</summary>
    public string NamaBarang { get; set; } = null!;

    /// <summary>Unit copied from the stock list when the line was written.</summary>
    public string? Satuan { get; set; }

    /// <summary>Price per unit copied from the stock list when the line was written.</summary>
    public decimal HargaSatuan { get; set; }

    /// <summary>Whole units only, at least one.</summary>
    public int Jumlah { get; set; } = 1;

    /// <summary>Date the line was written, printed next to the line on Details.</summary>
    public DateOnly Tanggal { get; set; }
}
