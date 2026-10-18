using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Barang.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Services;

/// <summary>
/// Item data access for the debt notebook's stock list.
/// </summary>
public class BarangService(
    AppDbContext dbContext,
    ILogger<BarangService> logger) : IBarangService
{
    /// <summary>Largest amount the numeric(14,2) harga column holds.</summary>
    private const decimal MaxHarga = 999_999_999_999.99m;

    /// <summary>Decimals the harga column keeps.</summary>
    private const int HargaScale = 2;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<BarangService> _logger = logger;

    public async Task<ErrorOr<IReadOnlyList<Barang>>> GetAllAsync()
    {
        var barangs = await _dbContext.Barangs
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return ErrorOr<IReadOnlyList<Barang>>.CreateSuccess(
            "Berhasil memuat daftar barang", barangs);
    }

    public async Task<ErrorOr<Barang>> CreateAsync(CreateBarangRequest request)
    {
        var nama = (request.Nama ?? string.Empty).Trim();

        if (nama.Length == 0)
            return ErrorOr<Barang>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["Nama"] = ["Nama barang wajib diisi"]
            });

        if (!TryParseHarga(request.Harga, out var harga))
            return ErrorOr<Barang>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["Harga"] = ["Harga harus angka, contoh 78.000"]
            });

        if (harga > MaxHarga)
            return ErrorOr<Barang>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["Harga"] = ["Harga terlalu besar"]
            });

        var barang = new Barang
        {
            Nama = nama,
            Satuan = Normalize(request.Satuan),
            // Round to the column scale so the saved row and the response report the same amount.
            Harga = decimal.Round(harga, HargaScale, MidpointRounding.AwayFromZero),
            // Npgsql needs a UTC kind for the timestamp with time zone column.
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Barangs.Add(barang);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        // EF's transient-failure strategy wraps provider errors, so walk the inner chain.
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _dbContext.Entry(barang).State = EntityState.Detached;
            _logger.LogError(ex, "Gagal menyimpan barang {Nama}.", barang.Nama);

            return ErrorOr<Barang>.CreateFailure(
                "Gagal menyimpan barang. Coba simpan lagi.", ErrorType.Unexpected);
        }

        return ErrorOr<Barang>.CreateSuccess(
            "Barang tersimpan di buku", barang);
    }

    /// <summary>
    /// Reads a typed price the way the notebook writes it: dots group thousands and a comma marks decimals,
    /// so "78.000" and "78,5" keep their meaning on a server running under any culture.
    /// </summary>
    private static bool TryParseHarga(string? value, out decimal harga)
    {
        harga = 0m;

        var text = (value ?? string.Empty).Trim();
        if (text.StartsWith("Rp", StringComparison.OrdinalIgnoreCase))
            text = text[2..].Trim();

        var digits = text
            .Where(character => char.IsDigit(character) || character is '.' or ',' or ' ')
            .ToArray();

        if (digits.Length == 0)
            return false;

        // A letter or a sign means the field is not a price at all, so nothing is silently dropped.
        if (digits.Length != text.Length)
            return false;

        var normalized = new string(digits)
            .Replace(".", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(',', '.');

        return decimal.TryParse(
            normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out harga);
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
