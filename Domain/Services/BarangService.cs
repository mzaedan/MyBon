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

    public async Task<ErrorOr<Barang>> GetByIdAsync(int id)
    {
        var barang = await _dbContext.Barangs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barang is null)
            return ErrorOr<Barang>.CreateFailure(
                "Barang tidak ditemukan", ErrorType.NotFound);

        return ErrorOr<Barang>.CreateSuccess(
            "Barang ditemukan", barang);
    }

    public async Task<ErrorOr<Barang>> CreateAsync(CreateBarangRequest request)
    {
        var errors = ReadFields(request.Nama, request.Satuan, request.Harga,
            out var nama, out var satuan, out var harga);
        if (errors is not null)
            return ErrorOr<Barang>.CreateValidationFailure(errors);

        var barang = new Barang
        {
            Nama = nama,
            Satuan = satuan,
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

    public async Task<ErrorOr<Barang>> UpdateAsync(int id, UpdateBarangRequest request)
    {
        var barang = await _dbContext.Barangs
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barang is null)
            return ErrorOr<Barang>.CreateFailure(
                "Barang tidak ditemukan", ErrorType.NotFound);

        var errors = ReadFields(request.Nama, request.Satuan, request.Harga,
            out var nama, out var satuan, out var harga);
        if (errors is not null)
            return ErrorOr<Barang>.CreateValidationFailure(errors);

        barang.Nama = nama;
        barang.Satuan = satuan;
        // Round to the column scale so the saved row and the response report the same amount.
        barang.Harga = decimal.Round(harga, HargaScale, MidpointRounding.AwayFromZero);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _logger.LogError(ex, "Gagal memperbarui barang {Id}.", id);

            return ErrorOr<Barang>.CreateFailure(
                "Gagal memperbarui barang. Coba simpan lagi.", ErrorType.Unexpected);
        }

        return ErrorOr<Barang>.CreateSuccess(
            "Barang diperbarui di buku", barang);
    }

    /// <summary>
    /// Trims the text fields and reads the typed price, reporting the messages the form shows under each field
    /// when an item cannot be saved. Create and update take the same three fields, so they reject the same input
    /// with the same sentences.
    /// </summary>
    private static Dictionary<string, string[]>? ReadFields(
        string? nama,
        string? satuan,
        string? harga,
        out string name,
        out string? unit,
        out decimal price)
    {
        name = (nama ?? string.Empty).Trim();
        unit = Normalize(satuan);
        price = 0m;

        var errors = new Dictionary<string, string[]>();

        if (name.Length == 0)
            errors["Nama"] = ["Nama barang wajib diisi"];

        if (!TryParseHarga(harga, out price))
            errors["Harga"] = ["Harga harus angka, contoh 78.000"];
        else if (price > MaxHarga)
            errors["Harga"] = ["Harga terlalu besar"];

        return errors.Count == 0 ? null : errors;
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
