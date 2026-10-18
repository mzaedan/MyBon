using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Common;
using MyBon.Models.Pelanggan.Requests;

namespace MyBon.Domain.Services;

/// <summary>
/// Customer data access for the debt notebook.
/// </summary>
public class PelangganService(
    AppDbContext dbContext,
    ILogger<PelangganService> logger) : IPelangganService
{
    /// <summary>Escape character handed to Postgres ILIKE so a typed <c>%</c> or <c>_</c> matches literally.</summary>
    private const string LikeEscapeCharacter = "\\";

    /// <summary>
    /// Rows painted per request. The client only sends the page number, so a caller cannot ask
    /// the database for the whole notebook.
    /// </summary>
    private const int PageSize = 8;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<PelangganService> _logger = logger;

    public async Task<ErrorOr<PagedResult<Pelanggan>>> SearchAsync(string? keyword, int page)
    {
        var term = Normalize(keyword);
        var currentPage = page < 1 ? 1 : page;

        var query = _dbContext.Pelanggans.AsNoTracking();

        if (term is not null)
        {
            // Only the customer name is searched; the phone number and address are never matched.
            var pattern = ToLikePattern(term);
            query = query.Where(p => EF.Functions.ILike(p.Nama, pattern, LikeEscapeCharacter));
        }

        // CreatedAt alone ties when rows are written in the same tick, so Id breaks the tie and keeps
        // the order stable across requests; without it a page could repeat or skip a customer.
        var ordered = query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id);

        var total = await ordered.CountAsync();
        var pelanggans = await ordered
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return ErrorOr<PagedResult<Pelanggan>>.CreateSuccess(
            term is null
                ? "Berhasil memuat daftar pelanggan"
                : total == 0
                    ? $"Tidak ada pelanggan yang cocok dengan \"{term}\""
                    : $"Ditemukan {total} pelanggan",
            new PagedResult<Pelanggan>
            {
                Items = pelanggans,
                Total = total,
                Page = currentPage,
                PageSize = PageSize
            });
    }

    public async Task<ErrorOr<Pelanggan>> GetByIdAsync(int id)
    {
        var pelanggan = await _dbContext.Pelanggans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pelanggan is null)
            return ErrorOr<Pelanggan>.CreateFailure(
                "Pelanggan tidak ditemukan", ErrorType.NotFound);

        return ErrorOr<Pelanggan>.CreateSuccess(
            "Pelanggan ditemukan", pelanggan);
    }

    public async Task<ErrorOr<Pelanggan>> CreateAsync(CreatePelangganRequest request)
    {
        var pelanggan = new Pelanggan
        {
            Nama = (request.Nama ?? string.Empty).Trim(),
            NomorHp = Normalize(request.NomorHp),
            Alamat = Normalize(request.Alamat),
            // Npgsql needs a UTC kind for the timestamp with time zone column.
            CreatedAt = DateTime.UtcNow
        };

        if (pelanggan.Nama.Length == 0)
            return ErrorOr<Pelanggan>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["Nama"] = ["Nama wajib diisi"]
            });

        _dbContext.Pelanggans.Add(pelanggan);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        // EF's transient-failure strategy wraps provider errors, so walk the inner chain.
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _dbContext.Entry(pelanggan).State = EntityState.Detached;
            _logger.LogError(ex, "Gagal menyimpan pelanggan {Nama}.", pelanggan.Nama);

            return ErrorOr<Pelanggan>.CreateFailure(
                "Gagal menyimpan pelanggan. Coba simpan lagi.", ErrorType.Unexpected);
        }

        return ErrorOr<Pelanggan>.CreateSuccess(
            "Pelanggan tersimpan di buku", pelanggan);
    }

    public async Task<ErrorOr<Pelanggan>> UpdateAsync(int id, UpdatePelangganRequest request)
    {
        var pelanggan = await _dbContext.Pelanggans
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pelanggan is null)
            return ErrorOr<Pelanggan>.CreateFailure(
                "Pelanggan tidak ditemukan", ErrorType.NotFound);

        pelanggan.Nama = (request.Nama ?? string.Empty).Trim();
        pelanggan.NomorHp = Normalize(request.NomorHp);
        pelanggan.Alamat = Normalize(request.Alamat);

        if (pelanggan.Nama.Length == 0)
            return ErrorOr<Pelanggan>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["Nama"] = ["Nama wajib diisi"]
            });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _logger.LogError(ex, "Gagal memperbarui pelanggan {Id}.", id);

            return ErrorOr<Pelanggan>.CreateFailure(
                "Gagal memperbarui pelanggan. Coba simpan lagi.", ErrorType.Unexpected);
        }

        return ErrorOr<Pelanggan>.CreateSuccess(
            "Data pelanggan diperbarui", pelanggan);
    }

    public async Task<ErrorOr> DeleteAsync(int id)
    {
        var pelanggan = await _dbContext.Pelanggans
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pelanggan is null)
            return ErrorOr.CreateFailure(
                "Pelanggan tidak ditemukan", ErrorType.NotFound);

        _dbContext.Pelanggans.Remove(pelanggan);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _dbContext.Entry(pelanggan).State = EntityState.Detached;
            _logger.LogError(ex, "Gagal menghapus pelanggan {Id}.", id);

            return ErrorOr.CreateFailure(
                "Gagal menghapus pelanggan. Coba hapus lagi.", ErrorType.Unexpected);
        }

        return ErrorOr.CreateSuccess(
            "Pelanggan dihapus dari buku");
    }

    /// <summary>Wraps <paramref name="term"/> in wildcards, escaping the wildcards the user typed.</summary>
    private static string ToLikePattern(string term) =>
        $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsDatabaseFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or DbUpdateException)
                return true;
        }

        return false;
    }
}
