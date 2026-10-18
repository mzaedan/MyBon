using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Entities;
using MyBon.Domain.Interfaces;
using MyBon.Models.Common;

namespace MyBon.Domain.Services;

/// <summary>
/// Item data access for the debt notebook's stock list.
/// </summary>
public class BarangService(AppDbContext dbContext) : IBarangService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<ErrorOr<IReadOnlyList<Barang>>> GetAllAsync()
    {
        var barangs = await _dbContext.Barangs
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return ErrorOr<IReadOnlyList<Barang>>.CreateSuccess(
            "Berhasil memuat daftar barang", barangs);
    }
}
