using MyBon.Domain.Abstractions;

namespace MyBon.Domain.Entities;

/// <summary>
/// One payment recorded against a bon. A page can carry several rows (installments), so the paid
/// amount is always the sum of this table, never a column on <see cref="Bon"/>.
/// </summary>
public class BonPayment : BaseEntity
{
    public int Id { get; set; }

    public int BonId { get; set; }

    public Bon? Bon { get; set; }

    public DateOnly Tanggal { get; set; }

    public decimal Jumlah { get; set; }

    public string? Catatan { get; set; }
}
