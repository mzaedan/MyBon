using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Bon.DTOs;
using MyBon.Models.Bon.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Services;

/// <summary>
/// Bon data access for the debt notebook: reads the Catat Bon list, builds the Create form's options,
/// and writes a page with its lines and payments.
/// </summary>
public class BonService(
    AppDbContext dbContext,
    ILogger<BonService> logger) : IBonService
{
    /// <summary>Escape character handed to Postgres ILIKE so a typed <c>%</c> or <c>_</c> matches literally.</summary>
    private const string LikeEscapeCharacter = "\\";

    /// <summary>
    /// Rows painted per request. The client only sends the page number, so a caller cannot ask
    /// the database for the whole notebook.
    /// </summary>
    private const int PageSize = 8;

    /// <summary>Largest amount the numeric(14,2) money columns hold.</summary>
    private const decimal MaxMoney = 999_999_999_999.99m;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<BonService> _logger = logger;

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

    public async Task<ErrorOr<BonDetailDto>> GetDetailAsync(int id)
    {
        var bon = await _dbContext.Bons
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BonDetailDto
            {
                Id = b.Id,
                Nomor = b.Nomor,
                NamaPelanggan = b.Pelanggan!.Nama,
                Tanggal = b.Tanggal,
                Catatan = b.Catatan,
                // The lines carry the price they were written at, so the summary agrees with them.
                TotalBelanja = b.Items.Sum(item => item.HargaSatuan * item.Jumlah),
                TotalBayar = b.Payments.Sum(payment => payment.Jumlah),
                // Oldest first, the way the history is read; the id breaks a same-day tie.
                Payments = b.Payments
                    .OrderBy(payment => payment.Tanggal)
                    .ThenBy(payment => payment.Id)
                    .Select(payment => new BonDetailPaymentDto
                    {
                        Id = payment.Id,
                        Tanggal = payment.Tanggal,
                        Jumlah = payment.Jumlah,
                        Catatan = payment.Catatan
                    })
                    .ToList(),
                Items = b.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new BonDetailItemDto
                    {
                        Id = item.Id,
                        NamaBarang = item.NamaBarang,
                        Satuan = item.Satuan,
                        HargaSatuan = item.HargaSatuan,
                        Jumlah = item.Jumlah,
                        Tanggal = item.Tanggal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (bon is null)
            return ErrorOr<BonDetailDto>.CreateFailure("Bon tidak ditemukan.", ErrorType.NotFound);

        return ErrorOr<BonDetailDto>.CreateSuccess("Berhasil memuat detail bon", bon);
    }

    public async Task<ErrorOr<BonFormDto>> GetFormAsync()
    {
        // Both sheets list the whole notebook, ordered by name so the pickers read like an index.
        // The debt of a customer is summed from their bons, never stored on the customer.
        var customers = await _dbContext.Pelanggans
            .AsNoTracking()
            .OrderBy(p => p.Nama)
            .ThenBy(p => p.Id)
            .Select(p => new BonCustomerOptionDto
            {
                Id = p.Id,
                Nama = p.Nama,
                NomorHp = p.NomorHp,
                TotalBelanja = _dbContext.Bons
                    .Where(b => b.PelangganId == p.Id)
                    .SelectMany(b => b.Items)
                    .Sum(item => (decimal?)(item.HargaSatuan * item.Jumlah)) ?? 0m,
                TotalBayar = _dbContext.BonPayments
                    .Where(payment => payment.Bon!.PelangganId == p.Id)
                    .Sum(payment => (decimal?)payment.Jumlah) ?? 0m
            })
            .ToListAsync();

        var barangs = await _dbContext.Barangs
            .AsNoTracking()
            .OrderBy(b => b.Nama)
            .ThenBy(b => b.Id)
            .Select(b => new BonBarangOptionDto
            {
                Id = b.Id,
                Nama = b.Nama,
                Satuan = b.Satuan,
                Harga = b.Harga
            })
            .ToListAsync();

        return ErrorOr<BonFormDto>.CreateSuccess(
            "Berhasil memuat pilihan pelanggan dan barang",
            new BonFormDto
            {
                Customers = customers,
                Barangs = barangs
            });
    }

    public async Task<ErrorOr<Bon>> CreateAsync(CreateBonRequest request)
    {
        var items = request.Items;
        var errors = new Dictionary<string, string[]>();

        if (request.PelangganId is null)
            errors["PelangganId"] = ["Pelanggan wajib dipilih"];

        if (request.Tanggal is null)
            errors["Tanggal"] = ["Tanggal wajib diisi"];

        if (!TryParseMoney(request.TotalBayar, out var totalBayar))
        {
            errors["TotalBayar"] = ["Total bayar harus angka, contoh 50.000"];
        }
        else
        {
            // The money columns keep two decimals, so validation compares the amount the database stores.
            totalBayar = Math.Round(totalBayar, 2, MidpointRounding.AwayFromZero);

            if (totalBayar > MaxMoney)
                errors["TotalBayar"] = ["Total bayar terlalu besar"];
        }

        if (items.Count == 0)
            errors["Items"] = ["Tambahkan minimal satu barang"];

        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].BarangId is null)
                errors[$"Items[{index}].BarangId"] = ["Barang wajib dipilih"];

            if (items[index].Jumlah < 1)
                errors[$"Items[{index}].Jumlah"] = ["Jumlah minimal 1"];
        }

        if (errors.Count > 0)
            return ErrorOr<Bon>.CreateValidationFailure(errors);

        // Every value below is present because the block above rejected the empties.
        var pelangganId = request.PelangganId!.Value;
        var tanggal = request.Tanggal!.Value;

        var pelangganExists = await _dbContext.Pelanggans
            .AsNoTracking()
            .AnyAsync(p => p.Id == pelangganId);

        if (!pelangganExists)
            return ErrorOr<Bon>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["PelangganId"] = ["Pelanggan tidak ditemukan"]
            });

        var barangIds = items.Select(item => item.BarangId!.Value).Distinct().ToList();
        var barangs = await _dbContext.Barangs
            .AsNoTracking()
            .Where(b => barangIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        var lines = new List<BonItem>(items.Count);
        var totalBelanja = 0m;

        // Prices and names are copied from the stock list, so the page keeps the price it was written
        // at even after a master price changes, and a tampered form cannot choose its own price.
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (!barangs.TryGetValue(item.BarangId!.Value, out var barang))
            {
                errors[$"Items[{index}].BarangId"] = ["Barang tidak ditemukan"];
                continue;
            }

            totalBelanja += barang.Harga * item.Jumlah;

            lines.Add(new BonItem
            {
                BarangId = barang.Id,
                NamaBarang = barang.Nama,
                Satuan = barang.Satuan,
                HargaSatuan = barang.Harga,
                Jumlah = item.Jumlah,
                Tanggal = tanggal
            });
        }

        if (errors.Count > 0)
            return ErrorOr<Bon>.CreateValidationFailure(errors);

        if (totalBayar > totalBelanja)
            return ErrorOr<Bon>.CreateValidationFailure(new Dictionary<string, string[]>
            {
                ["TotalBayar"] = ["Total bayar tidak boleh lebih dari total belanja"]
            });

        var bon = new Bon
        {
            PelangganId = pelangganId,
            Tanggal = tanggal,
            Catatan = Normalize(request.Catatan),
            Items = lines
        };

        // Paid money has one source: the first payment is a bon_payment row too, not a column on the bon.
        if (totalBayar > 0)
            bon.Payments.Add(new BonPayment
            {
                Tanggal = tanggal,
                Jumlah = totalBayar
            });

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.Bons.Add(bon);

        try
        {
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            _logger.LogError(ex, "Gagal menyimpan bon untuk pelanggan {PelangganId}.", pelangganId);

            return ErrorOr<Bon>.CreateFailure(
                "Gagal menyimpan bon. Coba simpan lagi.", ErrorType.Unexpected);
        }

        return ErrorOr<Bon>.CreateSuccess($"Bon {bon.Nomor} tersimpan", bon);
    }

    /// <summary>
    /// Reads the typed total the way the notebook writes money: dots group thousands and a comma marks
    /// decimals, so "50.000" and "50,5" keep their meaning under any server culture. An empty field is
    /// zero, not an error.
    /// </summary>
    private static bool TryParseMoney(string? value, out decimal amount)
    {
        amount = 0m;

        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
            return true;

        if (text.StartsWith("Rp", StringComparison.OrdinalIgnoreCase))
            text = text[2..].Trim();

        var digits = text
            .Where(character => char.IsDigit(character) || character is '.' or ',' or ' ')
            .ToArray();

        if (digits.Length == 0)
            return false;

        // A letter or a sign means the field is not money at all, so nothing is silently dropped.
        if (digits.Length != text.Length)
            return false;

        var normalized = new string(digits)
            .Replace(".", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(',', '.');

        return decimal.TryParse(
            normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
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
