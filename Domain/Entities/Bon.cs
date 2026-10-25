using MyBon.Domain.Abstractions;

namespace MyBon.Domain.Entities;

/// <summary>
/// One page of the debt notebook: the customer it belongs to, the day it was written, and every item
/// and payment recorded under it. Totals are never stored; they are read from <see cref="Items"/> and
/// <see cref="Payments"/> so a page can never disagree with its own lines.
/// </summary>
public class Bon : BaseEntity
{
    public int Id { get; set; }

    /// <summary>Human readable number such as <c>BN-001</c>, assigned by the database sequence.</summary>
    public string Nomor { get; set; } = null!;

    public int PelangganId { get; set; }

    public Pelanggan? Pelanggan { get; set; }

    /// <summary>Date written on the page. The notebook keeps no time of day.</summary>
    public DateOnly Tanggal { get; set; }

    public string? Catatan { get; set; }

    public List<BonItem> Items { get; set; } = [];

    public List<BonPayment> Payments { get; set; } = [];
}
