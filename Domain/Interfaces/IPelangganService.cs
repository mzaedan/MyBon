using MyBon.Domain.Entities;
using MyBon.Models.Common;
using MyBon.Models.Pelanggan.Requests;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Customer (pelanggan) data for the debt notebook.
/// </summary>
public interface IPelangganService
{
    /// <summary>Returns every customer, ordered by name.</summary>
    Task<ErrorOr<IReadOnlyList<Pelanggan>>> GetAllAsync();

    /// <summary>Returns the customer with the given identifier.</summary>
    Task<ErrorOr<Pelanggan>> GetByIdAsync(int id);

    /// <summary>Persists a new customer built from <paramref name="request"/>.</summary>
    Task<ErrorOr<Pelanggan>> CreateAsync(CreatePelangganRequest request);
}
