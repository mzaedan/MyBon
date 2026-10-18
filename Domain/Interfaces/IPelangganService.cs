using MyBon.Domain.Entities;
using MyBon.Models.Common;
using MyBon.Models.Pelanggan.Requests;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Customer (pelanggan) data for the debt notebook.
/// </summary>
public interface IPelangganService
{
    /// <summary>
    /// Returns one page of the customers whose name contains <paramref name="keyword"/>, ordered by entry
    /// date (newest first). Phone numbers and addresses are not searched. A blank keyword reads as the whole
    /// notebook. Paging runs in the database, so a request never loads the rows it does not paint.
    /// </summary>
    Task<ErrorOr<PagedResult<Pelanggan>>> SearchAsync(string? keyword, int page);

    /// <summary>Returns the customer with the given identifier.</summary>
    Task<ErrorOr<Pelanggan>> GetByIdAsync(int id);

    /// <summary>Persists a new customer built from <paramref name="request"/>.</summary>
    Task<ErrorOr<Pelanggan>> CreateAsync(CreatePelangganRequest request);

    /// <summary>Updates an existing customer identified by <paramref name="id"/> using <paramref name="request"/>.</summary>
    Task<ErrorOr<Pelanggan>> UpdateAsync(int id, UpdatePelangganRequest request);

    /// <summary>Deletes the customer with the given identifier.</summary>
    Task<ErrorOr> DeleteAsync(int id);
}
