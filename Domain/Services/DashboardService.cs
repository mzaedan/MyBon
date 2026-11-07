using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Interfaces;
using MyBon.Models.Common;
using MyBon.Models.Dashboard.DTOs;

namespace MyBon.Domain.Services;

/// <summary>
/// Dashboard figures read from the notebook. The debt total is summed from the item and payment lines
/// the same way the Bon list and Details read them, so the summary and its pages can never disagree.
/// </summary>
public class DashboardService(AppDbContext dbContext) : IDashboardService
{
    /// <summary>
    /// Rows the Dashboard's **Belum lunas** list paints: only the biggest debts fit on the summary, so
    /// the notebook is ranked by what each customer owes and the list stops at this count.
    /// </summary>
    private const int TopDebtCustomerCount = 3;

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<ErrorOr<DashboardSummaryDto>> GetSummaryAsync()
    {
        // A page owes its lines minus its payments. A settled page adds nothing, and a page whose
        // payments somehow passed its total is skipped instead of lowering another page's debt.
        // The sums run in the database, so the page never loads every line to add them here.
        var totalHutang = await _dbContext.Bons
            .AsNoTracking()
            .Select(bon => new
            {
                TotalBelanja = bon.Items.Sum(item => (decimal?)(item.HargaSatuan * item.Jumlah)) ?? 0m,
                TotalBayar = bon.Payments.Sum(payment => (decimal?)payment.Jumlah) ?? 0m
            })
            .Where(bon => bon.TotalBelanja > bon.TotalBayar)
            .SumAsync(bon => bon.TotalBelanja - bon.TotalBayar);

        // Every unit written on a bon line was sold, whether or not its page is paid, so this sum reads
        // the whole notebook and never looks at bon_payment.
        var totalProdukTerjual = await _dbContext.BonItems.SumAsync(item => (int?)item.Jumlah) ?? 0;

        // Only the pages that still owe money count, and a settled page never cancels another page's
        // debt for the same customer, so every bon is clamped before the customers are ranked.
        var belumLunas = await _dbContext.Bons
            .AsNoTracking()
            .Select(bon => new
            {
                bon.PelangganId,
                Nama = bon.Pelanggan!.Nama,
                Sisa = (bon.Items.Sum(item => (decimal?)(item.HargaSatuan * item.Jumlah)) ?? 0m)
                     - (bon.Payments.Sum(payment => (decimal?)payment.Jumlah) ?? 0m)
            })
            .Where(bon => bon.Sisa > 0)
            .GroupBy(bon => new { bon.PelangganId, bon.Nama })
            .Select(group => new DashboardDebtItemDto
            {
                PelangganId = group.Key.PelangganId,
                Nama = group.Key.Nama,
                SisaHutang = group.Sum(bon => bon.Sisa)
            })
            // The biggest debt sits on top, and the name breaks the tie so the order never reshuffles
            // when two customers owe the same amount.
            .OrderByDescending(debt => debt.SisaHutang)
            .ThenBy(debt => debt.Nama)
            .Take(TopDebtCustomerCount)
            .ToListAsync();

        return ErrorOr<DashboardSummaryDto>.CreateSuccess(
            "Berhasil memuat ringkasan bon",
            new DashboardSummaryDto
            {
                TotalHutang = totalHutang,
                TotalProdukTerjual = totalProdukTerjual,
                BelumLunas = belumLunas
            });
    }
}
