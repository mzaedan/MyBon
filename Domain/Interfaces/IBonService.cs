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
    /// Reads one page back for Details: the customer, the note, every line at the price it was
    /// written at, and every payment oldest first. A missing id is reported as not found, so the
    /// sheet never paints a page that is not in the notebook.
    /// </summary>
    Task<ErrorOr<BonDetailDto>> GetDetailAsync(int id);

    /// <summary>
    /// Returns the rows the Create form paints: every customer with what they still owe, and the first
    /// page of stock items its picker shows. The pages after that one come from
    /// <see cref="SearchBarangAsync"/>.
    /// </summary>
    Task<ErrorOr<BonFormDto>> GetFormAsync();

    /// <summary>
    /// Returns one page of the stock items the Create form's picker lists, matching
    /// <paramref name="keyword"/> against the item name only. Paging runs in the database, so a picker
    /// request never loads the rows it does not paint.
    /// </summary>
    Task<ErrorOr<PagedResult<BonBarangOptionDto>>> SearchBarangAsync(string? keyword, int page);

    /// <summary>
    /// Re-reads the lines a rejected form posted, with each item's current name, unit, and price, so
    /// the form can paint them again instead of dropping what was already picked. An id that no longer
    /// exists is skipped; validation has already reported it.
    /// </summary>
    Task<List<BonLineDto>> ResolveLinesAsync(IEnumerable<CreateBonItemRequest> items);

    /// <summary>
    /// Writes one picked stock item into an existing bon as a new line of one unit, dated today. The
    /// name, unit, and price are copied from the stock list, never posted by the client. A missing bon
    /// is reported as not found, so the sheet can never write into a page that is not there.
    /// </summary>
    Task<ErrorOr> AddItemAsync(AddBonItemRequest request);

    /// <summary>
    /// Records one payment against an existing bon: the amount as typed, the day it was handed over, and
    /// the optional note. The amount is refused when it is empty, not a number, or more than what the
    /// page still owes, so a bon can never turn into a credit. A missing bon is reported as not found.
    /// </summary>
    Task<ErrorOr> AddPaymentAsync(AddBonPaymentRequest request);

    /// <summary>
    /// Writes one bon with its item lines and, when money was paid up front, its first payment row.
    /// Prices are read from the stock list, never from <paramref name="request"/>.
    /// </summary>
    Task<ErrorOr<Bon>> CreateAsync(CreateBonRequest request);

    /// <summary>
    /// Removes a settled bon with every line and payment recorded under it. A page that is still owed
    /// money is refused, so a debt is never erased, and a missing id is reported as not found.
    /// </summary>
    Task<ErrorOr> DeleteAsync(int id);
}
