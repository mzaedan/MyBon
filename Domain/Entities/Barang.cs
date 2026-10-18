using MyBon.Domain.Abstractions;

namespace MyBon.Domain.Entities;

/// <summary>
/// Item recorded in the debt notebook's stock list.
/// </summary>
public class Barang : BaseEntity
{
    public int Id { get; set; }

    public string Nama { get; set; } = null!;

    public string? Satuan { get; set; }

    public decimal Harga { get; set; }
}
