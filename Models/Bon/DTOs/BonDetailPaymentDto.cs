namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One payment row inside <see cref="BonDetailDto"/>. A page can carry several rows (installments);
/// the <c>Cicilan n</c> label is rendered from the row order, never stored.
/// </summary>
public class BonDetailPaymentDto
{
    public int Id { get; set; }

    public DateOnly Tanggal { get; set; }

    public decimal Jumlah { get; set; }

    public string? Catatan { get; set; }
}
