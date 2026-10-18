using MyBon.Domain.Entities;
using MyBon.Models.Barang.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Item (barang) data for the debt notebook's stock list.
/// </summary>
public interface IBarangService
{
    /// <summary>
    /// Returns one page of the items whose name contains <paramref name="keyword"/>, ordered by entry date
    /// (newest first). Units are not searched, so a keyword typed from the stock list only ever matches item
    /// names. A blank keyword reads as the whole stock list. Paging runs in the database, so a request never
    /// loads the rows it does not paint.
    /// </summary>
    Task<ErrorOr<PagedResult<Barang>>> SearchAsync(string? keyword, int page);

    /// <summary>Returns the item with the given identifier.</summary>
    Task<ErrorOr<Barang>> GetByIdAsync(int id);

    /// <summary>Writes a new item into the stock list.</summary>
    Task<ErrorOr<Barang>> CreateAsync(CreateBarangRequest request);

    /// <summary>Updates an existing item identified by <paramref name="id"/> using <paramref name="request"/>.</summary>
    Task<ErrorOr<Barang>> UpdateAsync(int id, UpdateBarangRequest request);

    /// <summary>Deletes the item with the given identifier.</summary>
    Task<ErrorOr> DeleteAsync(int id);
}
