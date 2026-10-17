using MyBon.Domain.Abstractions;

namespace MyBon.Domain.Entities;

/// <summary>
/// Customer recorded in the debt notebook.
/// </summary>
public class Pelanggan : BaseEntity
{
    public int Id { get; set; }

    public string Nama { get; set; } = null!;

    public string? NomorHp { get; set; }

    public string? Alamat { get; set; }
}
