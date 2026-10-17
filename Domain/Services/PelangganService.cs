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
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<PelangganService> _logger = logger;

    public async Task<ErrorOr<IReadOnlyList<Pelanggan>>> GetAllAsync()
    {
        var pelanggans = await _dbContext.Pelanggans
            .OrderBy(p => p.Nama)
            .AsNoTracking()
            .ToListAsync();

        return ErrorOr<IReadOnlyList<Pelanggan>>.CreateSuccess(
            "Berhasil memuat daftar pelanggan", pelanggans);
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
