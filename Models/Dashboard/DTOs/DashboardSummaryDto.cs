namespace MyBon.Models.Dashboard.DTOs;

/// <summary>
/// The figures the Dashboard paints. Every number is read from the notebook's own rows — the item and
/// payment lines — so the page never carries a figure the notebook cannot reproduce.
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// What every customer still owes across the notebook: each bon's lines minus its payments, added
    /// up over the pages that still owe money. Settled pages add nothing.
    /// </summary>
    public decimal TotalHutang { get; set; }

    /// <summary>
    /// How many units were sold across the notebook: every unit written on a bon line, whether or not
    /// its page is paid. Goods handed over are sold even when the money is still owed, so payments never
    /// subtract from this figure.
    /// </summary>
    public int TotalProdukTerjual { get; set; }

    /// <summary>
    /// The customers who still owe money, largest debt first, capped by the Dashboard's row count. A
    /// customer whose bons are all paid never appears, so the list only ever shows live debts.
    /// </summary>
    public List<DashboardDebtItemDto> BelumLunas { get; set; } = [];
}
