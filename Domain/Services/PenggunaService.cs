using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Abstractions;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Auth.Requests;
using MyBon.Models.Common;
using MyBon.Models.Pengguna.DTOs;
using MyBon.Models.Pengguna.Requests;

namespace MyBon.Domain.Services;

/// <summary>
/// Account management for the admin, plus the change-own-password path shared by both roles.
/// Business refusals are returned as <see cref="ErrorOr"/> failures, never as exceptions.
/// </summary>
public class PenggunaService(
    AppDbContext dbContext,
    IPasswordHasher<Pengguna> passwordHasher,
    IContext context,
    ILogger<PenggunaService> logger) : IPenggunaService
{
    private const int MinPasswordLength = 8;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<Pengguna> _passwordHasher = passwordHasher;
    private readonly IContext _context = context;
    private readonly ILogger<PenggunaService> _logger = logger;

    public async Task<ErrorOr<List<PenggunaDto>>> DaftarAsync()
    {
        var items = await _dbContext.Penggunas
            .AsNoTracking()
            .OrderBy(p => p.Peran)
            .ThenBy(p => p.Nama)
            .Select(p => new PenggunaDto
            {
                Id = p.Id,
                Nama = p.Nama,
                Username = p.Username,
                Peran = p.Peran,
                PelangganId = p.PelangganId,
                Aktif = p.Aktif,
                TerakhirMasuk = p.TerakhirMasuk
            })
            .ToListAsync();

        return ErrorOr<List<PenggunaDto>>.CreateSuccess("Berhasil memuat daftar pengguna", items);
    }

    public async Task<ErrorOr<PenggunaDto>> GetByIdAsync(int id)
    {
        var pengguna = await _dbContext.Penggunas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pengguna is null)
            return ErrorOr<PenggunaDto>.CreateFailure("Akun tidak ditemukan", ErrorType.NotFound);

        return ErrorOr<PenggunaDto>.CreateSuccess("Akun ditemukan", ToDto(pengguna));
    }

    public async Task<ErrorOr<PenggunaDto>> GetByPelangganIdAsync(int pelangganId)
    {
        var pengguna = await _dbContext.Penggunas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PelangganId == pelangganId);

        if (pengguna is null)
            return ErrorOr<PenggunaDto>.CreateFailure(
                "Pelanggan ini belum punya akun", ErrorType.NotFound);

        return ErrorOr<PenggunaDto>.CreateSuccess("Akun ditemukan", ToDto(pengguna));
    }

    public async Task<ErrorOr<PenggunaDto>> BuatAsync(CreatePenggunaRequest request)
    {
        var username = NormalizeUsername(request.Username);
        var nama = (request.Nama ?? string.Empty).Trim();

        if (username.Length == 0)
            return ErrorOr<PenggunaDto>.CreateValidationFailure(
                new Dictionary<string, string[]> { ["Username"] = ["Username wajib diisi"] });

        if (nama.Length == 0)
            return ErrorOr<PenggunaDto>.CreateValidationFailure(
                new Dictionary<string, string[]> { ["Nama"] = ["Nama wajib diisi"] });

        if (request.Password is null || request.Password.Length < MinPasswordLength)
            return ErrorOr<PenggunaDto>.CreateValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["Password"] = [$"Kata sandi minimal {MinPasswordLength} karakter"]
                });

        if (await _dbContext.Penggunas.AnyAsync(p => p.Username == username))
            return ErrorOr<PenggunaDto>.CreateFailure("Username sudah dipakai", ErrorType.Conflict);

        Pelanggan? pelanggan = null;

        if (request.PelangganId is int pelangganId)
        {
            pelanggan = await _dbContext.Pelanggans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pelangganId);

            if (pelanggan is null)
                return ErrorOr<PenggunaDto>.CreateFailure(
                    "Pelanggan tidak ditemukan", ErrorType.NotFound);

            if (await _dbContext.Penggunas.AnyAsync(p => p.PelangganId == pelangganId))
                return ErrorOr<PenggunaDto>.CreateFailure(
                    "Pelanggan ini sudah punya akun", ErrorType.Conflict);
        }

        var pengguna = new Pengguna
        {
            Username = username,
            Nama = nama,
            Peran = pelanggan is null ? PeranPengguna.Admin : PeranPengguna.Pelanggan,
            PelangganId = pelanggan?.Id,
            Aktif = true
        };

        pengguna.PasswordHash = _passwordHasher.HashPassword(pengguna, request.Password);

        _dbContext.Penggunas.Add(pengguna);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            // Two saves racing on the same username or the same customer must answer as a business
            // conflict, not as a raw unique-index error.
            _logger.LogWarning(exception, "Gagal menyimpan akun baru untuk username {Username}", username);
            _dbContext.Entry(pengguna).State = EntityState.Detached;

            return ErrorOr<PenggunaDto>.CreateFailure(
                "Username sudah dipakai atau pelanggan ini sudah punya akun", ErrorType.Conflict);
        }

        return ErrorOr<PenggunaDto>.CreateSuccess("Akun berhasil dibuat", ToDto(pengguna));
    }

    public async Task<ErrorOr> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var pengguna = await _dbContext.Penggunas
            .FirstOrDefaultAsync(p => p.Id == request.PenggunaId);

        if (pengguna is null)
            return ErrorOr.CreateFailure("Akun tidak ditemukan", ErrorType.NotFound);

        if (request.Password is null || request.Password.Length < MinPasswordLength)
            return ErrorOr.CreateValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["Password"] = [$"Kata sandi minimal {MinPasswordLength} karakter"]
                });

        pengguna.PasswordHash = _passwordHasher.HashPassword(pengguna, request.Password);
        await _dbContext.SaveChangesAsync();

        return ErrorOr.CreateSuccess("Kata sandi berhasil diatur ulang");
    }

    public async Task<ErrorOr> SetAktifAsync(int id, bool aktif)
    {
        var pengguna = await _dbContext.Penggunas.FirstOrDefaultAsync(p => p.Id == id);

        if (pengguna is null)
            return ErrorOr.CreateFailure("Akun tidak ditemukan", ErrorType.NotFound);

        if (pengguna.Aktif == aktif)
            return ErrorOr.CreateSuccess(aktif ? "Akun sudah aktif" : "Akun sudah nonaktif");

        if (!aktif && pengguna.Id == _context.Identity.Id)
            return ErrorOr.CreateFailure("Akun sendiri tidak bisa dinonaktifkan", ErrorType.Conflict);

        if (!aktif && pengguna.Peran == PeranPengguna.Admin)
        {
            var activeAdmins = await _dbContext.Penggunas
                .CountAsync(p => p.Peran == PeranPengguna.Admin && p.Aktif);

            if (activeAdmins <= 1)
                return ErrorOr.CreateFailure(
                    "Admin terakhir tidak bisa dinonaktifkan", ErrorType.Conflict);
        }

        pengguna.Aktif = aktif;
        await _dbContext.SaveChangesAsync();

        return ErrorOr.CreateSuccess(aktif ? "Akun diaktifkan" : "Akun dinonaktifkan");
    }

    public async Task<ErrorOr> GantiPasswordAsync(GantiPasswordRequest request)
    {
        var pengguna = await _dbContext.Penggunas
            .FirstOrDefaultAsync(p => p.Id == _context.Identity.Id);

        if (pengguna is null)
            return ErrorOr.CreateFailure("Akun tidak ditemukan", ErrorType.NotFound);

        if (string.IsNullOrEmpty(request.PasswordLama))
            return ErrorOr.CreateValidationFailure(
                new Dictionary<string, string[]> { ["PasswordLama"] = ["Kata sandi lama wajib diisi"] });

        var verification = _passwordHasher.VerifyHashedPassword(
            pengguna, pengguna.PasswordHash, request.PasswordLama);

        if (verification == PasswordVerificationResult.Failed)
            return ErrorOr.CreateValidationFailure(
                new Dictionary<string, string[]> { ["PasswordLama"] = ["Kata sandi lama salah"] });

        if (request.PasswordBaru is null || request.PasswordBaru.Length < MinPasswordLength)
            return ErrorOr.CreateValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["PasswordBaru"] = [$"Kata sandi baru minimal {MinPasswordLength} karakter"]
                });

        pengguna.PasswordHash = _passwordHasher.HashPassword(pengguna, request.PasswordBaru);
        await _dbContext.SaveChangesAsync();

        return ErrorOr.CreateSuccess("Kata sandi berhasil diganti");
    }

    /// <summary>Sign-in key is stored lowercase so a typed uppercase letter still matches.</summary>
    private static string NormalizeUsername(string? username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    private static PenggunaDto ToDto(Pengguna pengguna) => new()
    {
        Id = pengguna.Id,
        Nama = pengguna.Nama,
        Username = pengguna.Username,
        Peran = pengguna.Peran,
        PelangganId = pengguna.PelangganId,
        Aktif = pengguna.Aktif,
        TerakhirMasuk = pengguna.TerakhirMasuk
    };
}