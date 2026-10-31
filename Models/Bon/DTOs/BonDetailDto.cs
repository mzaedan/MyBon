using MyBon.Models.Common;

namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One bon read back for the Details sheet: the page header, the customer, the note, and every
/// line and payment recorded under it. Totals are computed from those rows, never stored on the
/// bon, so the sheet can never disagree with its own history.
/// </summary>
public class BonDetailDto
{
    public int Id { get; set; }

    /// <summary>Human readable number such as <c>BN-001</c>, used as the page title.</summary>
    public string Nomor { get; set; } = null!;

    public string NamaPelanggan { get; set; } = null!;

    public DateOnly Tanggal { get; set; }

    public string? Catatan { get; set; }

    /// <summary>Sum of every line's <c>harga_satuan x jumlah</c>, at the prices recorded on the page.</summary>
    public decimal TotalBelanja { get; set; }

    /// <summary>Sum of every payment recorded against the bon.</summary>
    public decimal TotalBayar { get; set; }

    /// <summary>What is still owed: <see cref="TotalBelanja"/> minus <see cref="TotalBayar"/>.</summary>
    public decimal SisaHutang => TotalBelanja - TotalBayar;

    /// <summary>Whether the page is fully paid.</summary>
    public bool Lunas => SisaHutang <= 0;

    /// <summary>Label the sheet prints next to the bon date.</summary>
    public string Status => Lunas ? "Lunas" : "Belum lunas";

    /// <summary>Item lines in the order they were written.</summary>
    public List<BonDetailItemDto> Items { get; set; } = [];

    /// <summary>Payments oldest first, the way the history is read.</summary>
    public List<BonDetailPaymentDto> Payments { get; set; } = [];

    /// <summary>
    /// First page of the stock list the Tambah barang sheet paints. The sheet asks the server for the
    /// pages after this one, so the detail page never carries the whole stock list.
    /// </summary>
    public PagedResult<BonBarangOptionDto> Barangs { get; set; } = new();
}
