using MyBon.Domain.Entities;
using MyBon.Models.Common;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Item (barang) data for the debt notebook's stock list.
/// </summary>
public interface IBarangService
{
    /// <summary>Returns every item, ordered by entry date (newest first).</summary>
    Task<ErrorOr<IReadOnlyList<Barang>>> GetAllAsync();
}
