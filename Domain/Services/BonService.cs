using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Interfaces;
using MyBon.Models.Bon.DTOs;
using MyBon.Models.Common;

namespace MyBon.Domain.Services;

/// <summary>
/// Bon data access for the debt notebook's Catat Bon list.
/// </summary>
public class BonService(AppDbContext dbContext) : IBonService
{
    /// <summary>Escape character handed to Postgres ILIKE so a typed <c>%</c> or <c>_</c> matches literally.</summary>
    private const string LikeEscapeCharacter = "\\";

    /// <summary>
    /// Rows painted per request. The client only sends the page number, so a caller cannot ask
    /// the database for the whole notebook.
    /// </summary>
    private const int PageSize = 8;

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<ErrorOr<PagedResult<BonListItemDto>>> SearchAsync(string? keyword, int page)
    {
        var term = Normalize(keyword);
        var currentPage = page < 1 ? 1 : page;

        var query = _dbContext.Bons.AsNoTracking();

        if (term is not null)
        {
            // The list searches the page number and the customer name; item names and notes are never
            // part of the predicate, so a keyword typed from the list only ever matches those two.
            var pattern = ToLikePattern(term);
            query = query.Where(b =>
                EF.Functions.ILike(b.Nomor, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(b.Pelanggan!.Nama, pattern, LikeEscapeCharacter));
        }

        // The newest page sits on top. Id breaks the tie when two bons share a date, so a page can
        // never repeat or skip a bon across requests.
        var ordered = query
            .OrderByDescending(b => b.Tanggal)
            .ThenByDescending(b => b.Id);

        var total = await ordered.CountAsync();
        var bons = await ordered
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new BonListItemDto
            {
                Id = b.Id,
                Nomor = b.Nomor,
                NamaPelanggan = b.Pelanggan!.Nama,
                Tanggal = b.Tanggal,
                // The lines carry the price they were written at, so changing a master price later
                // never rewrites an old page.
                TotalBelanja = b.Items.Sum(item => item.HargaSatuan * item.Jumlah),
                TotalBayar = b.Payments.Sum(payment => payment.Jumlah)
            })
            .ToListAsync();

        return ErrorOr<PagedResult<BonListItemDto>>.CreateSuccess(
            term is null
                ? "Berhasil memuat daftar bon"
                : total == 0
                    ? $"Tidak ada bon yang cocok dengan \"{term}\""
                    : $"Ditemukan {total} bon",
            new PagedResult<BonListItemDto>
            {
                Items = bons,
                Total = total,
                Page = currentPage,
                PageSize = PageSize
            });
    }

    /// <summary>Wraps <paramref name="term"/> in wildcards, escaping the wildcards the user typed.</summary>
    private static string ToLikePattern(string term) =>
        $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
