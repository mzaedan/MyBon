using MyBon.Models.Bon.DTOs;
using MyBon.Models.Common;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Bon (debt page) data for the notebook's Catat Bon list.
/// </summary>
public interface IBonService
{
    /// <summary>
    /// Returns one page of the bons whose number or customer name contains <paramref name="keyword"/>,
    /// ordered by bon date (newest first). Item names and notes are not searched. A blank keyword reads
    /// as the whole notebook. Paging runs in the database, so a request never loads the rows it does
    /// not paint.
    /// </summary>
    Task<ErrorOr<PagedResult<BonListItemDto>>> SearchAsync(string? keyword, int page);
}
