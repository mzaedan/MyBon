namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// One row in the customer sheet: the name, the phone number, and what the customer still owes
/// across every bon they own.
/// </summary>
public class BonCustomerOptionDto
{
    public int Id { get; set; }

    public string Nama { get; set; } = null!;

    public string? NomorHp { get; set; }

    /// <summary>Sum of every line's <c>harga_satuan x jumlah</c> on this customer's bons.</summary>
    public decimal TotalBelanja { get; set; }

    /// <summary>Sum of every payment recorded against this customer's bons.</summary>
    public decimal TotalBayar { get; set; }

    /// <summary>What the customer still owes: <see cref="TotalBelanja"/> minus <see cref="TotalBayar"/>.</summary>
    public decimal SisaHutang => TotalBelanja - TotalBayar;

    /// <summary>Whether every bon of this customer is fully paid.</summary>
    public bool Lunas => SisaHutang <= 0;
}
