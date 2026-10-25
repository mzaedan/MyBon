using MyBon.Domain.Entities;
using MyBon.Models.Bon.DTOs;
using MyBon.Models.Bon.Requests;
using MyBon.Models.Common;

namespace MyBon.Domain.Interfaces;

/// <summary>
/// Bon (debt page) data for the notebook: the Catat Bon list, the rows the Create form paints, and the
/// write that records a page with its lines and payments.
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

    /// <summary>
    /// Returns the rows the Create form paints: every customer with what they still owe, and every
    /// stock item with its current price. Both sheets are filled from this one call.
    /// </summary>
    Task<ErrorOr<BonFormDto>> GetFormAsync();

    /// <summary>
    /// Writes one bon with its item lines and, when money was paid up front, its first payment row.
    /// Prices are read from the stock list, never from <paramref name="request"/>.
    /// </summary>
    Task<ErrorOr<Bon>> CreateAsync(CreateBonRequest request);
}
