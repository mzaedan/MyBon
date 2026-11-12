using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;

namespace MyBon.Infrastructure.Persistence;

/// <summary>
/// Guarantees one admin account exists so a fresh database can be signed into. It runs only when
/// the table is empty and reads the password from configuration; it never invents one.
/// </summary>
public static class PenggunaSeeder
{
    private const string SeedUsername = "admin";
    private const string SeedName = "Admin Toko";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await dbContext.Penggunas.AnyAsync())
            return;

        var password = configuration["Auth:SeedPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Akun admin awal tidak dibuat: set Auth:SeedPassword (user-secrets) lalu jalankan ulang aplikasi.");
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Pengguna>>();

        var pengguna = new Pengguna
        {
            Username = SeedUsername,
            Nama = SeedName,
            Peran = PeranPengguna.Admin,
            PelangganId = null,
            Aktif = true
        };

        pengguna.PasswordHash = passwordHasher.HashPassword(pengguna, password);

        dbContext.Penggunas.Add(pengguna);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Akun admin awal dibuat dengan username {Username}.", SeedUsername);
    }
}