namespace MyBon.Models.Dashboard.DTOs;

/// <summary>
/// One row of the Dashboard's **Belum lunas** list: a customer and what the notebook still expects
/// from them.
/// </summary>
public class DashboardDebtItemDto
{
    public int PelangganId { get; set; }

    public string Nama { get; set; } = null!;

    /// <summary>
    /// What this customer still owes: each of their bons' lines minus its payments, added up over the
    /// pages that still owe money. A settled page never cancels another page's debt.
    /// </summary>
    public decimal SisaHutang { get; set; }
}
