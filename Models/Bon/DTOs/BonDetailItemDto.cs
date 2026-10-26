namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One line inside <see cref="BonDetailDto"/>. The name, unit and price are the snapshot taken when
/// the line was written, so changing a master price later never rewrites an old page.
/// </summary>
public class BonDetailItemDto
{
    public int Id { get; set; }

    public string NamaBarang { get; set; } = null!;

    public string? Satuan { get; set; }

    /// <summary>Price per unit at the moment the line was written.</summary>
    public decimal HargaSatuan { get; set; }

    /// <summary>Whole units recorded on the line, at least one.</summary>
    public int Jumlah { get; set; }

    /// <summary>What the line adds to the bon: <see cref="HargaSatuan"/> times <see cref="Jumlah"/>.</summary>
    public decimal Subtotal => HargaSatuan * Jumlah;

    /// <summary>Date the line was written, printed next to the line.</summary>
    public DateOnly Tanggal { get; set; }
}
