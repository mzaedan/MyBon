using MyBon.Models.Common;
using MyBon.Models.Dashboard.DTOs;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Reads the figures the Dashboard paints straight from the notebook, so the page shows what the bons
/// actually say instead of a hardcoded number.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns the debt every customer still owes: each bon's lines minus its payments, added up over
    /// the pages that still owe money. Totals stay derived, exactly like the Bon list and Details.
    /// </summary>
    Task<ErrorOr<DashboardSummaryDto>> GetSummaryAsync();
}
