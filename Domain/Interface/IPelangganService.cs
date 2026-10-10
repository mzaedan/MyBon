using MyBon.Models;
using MyBon.Models.Common;

namespace MyBon.Domain.Interface;

public interface IPelangganService
{
    Task<ErrorOr<IReadOnlyList<Pelanggan>>> GetAllAsync();
    Task<ErrorOr<Pelanggan>> GetByIdAsync(int id);
}