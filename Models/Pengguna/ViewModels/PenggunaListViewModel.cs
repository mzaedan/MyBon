using MyBon.Models.Pengguna.DTOs;

namespace MyBon.Models.Pengguna.ViewModels;

/// <summary>
/// Data the account management list paints: every account, newest admin first.
/// </summary>
public class PenggunaListViewModel
{
    public List<PenggunaDto> Items { get; set; } = [];

    /// <summary>Status notice carried from a finished POST.</summary>
    public string? Status { get; set; }

    /// <summary>Failure notice carried from a rejected POST.</summary>
    public string? Error { get; set; }
}