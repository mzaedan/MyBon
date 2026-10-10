using Microsoft.EntityFrameworkCore;
using MyBon.Data;
using MyBon.Domain.Enums;
using MyBon.Domain.Interface;
using MyBon.Models;
using MyBon.Models.Common;

namespace MyBon.Domain.Service;

public class PelangganService : IPelangganService
{
    private readonly AppDbContext _db;

    public PelangganService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<IReadOnlyList<Pelanggan>>> GetAllAsync()
    {
        var pelanggans = await _db.Pelanggans
            .OrderBy(p => p.Nama)
            .AsNoTracking()
            .ToListAsync();

        return ErrorOr<IReadOnlyList<Pelanggan>>.CreateSuccess(
            "Berhasil memuat daftar pelanggan", pelanggans);
    }

    public async Task<ErrorOr<Pelanggan>> GetByIdAsync(int id)
    {
        var pelanggan = await _db.Pelanggans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pelanggan is null)
            return ErrorOr<Pelanggan>.CreateFailure(
                "Pelanggan tidak ditemukan", ErrorType.NotFound);

        return ErrorOr<Pelanggan>.CreateSuccess(
            "Pelanggan ditemukan", pelanggan);
    }
}