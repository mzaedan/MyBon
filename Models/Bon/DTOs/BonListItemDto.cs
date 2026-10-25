namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One card on the Catat Bon list: the page header together with the totals the card paints.
/// The totals are summed from the item and payment lines, never stored on the bon itself.
/// </summary>
public class BonListItemDto
{
    public int Id { get; set; }

    /// <summary>Human readable number such as <c>BN-001</c>.</summary>
    public string Nomor { get; set; } = null!;

    public string NamaPelanggan { get; set; } = null!;

    public DateOnly Tanggal { get; set; }

    /// <summary>Sum of every line's <c>harga_satuan x jumlah</c>, at the prices recorded on the page.</summary>
    public decimal TotalBelanja { get; set; }

    /// <summary>Sum of every payment recorded against the bon.</summary>
    public decimal TotalBayar { get; set; }

    /// <summary>What is still owed: <see cref="TotalBelanja"/> minus <see cref="TotalBayar"/>.</summary>
    public decimal SisaHutang => TotalBelanja - TotalBayar;

    /// <summary>Whether the page is fully paid.</summary>
    public bool Lunas => SisaHutang <= 0;

    /// <summary>Label the card prints next to the number.</summary>
    public string Status => Lunas ? "Lunas" : "Belum lunas";
}
