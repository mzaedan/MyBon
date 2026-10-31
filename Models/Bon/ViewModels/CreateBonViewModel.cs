using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyBon.Models.Bon.DTOs;
using MyBon.Models.Bon.Requests;
using MyBon.Models.Common;

namespace MyBon.Models.Bon.ViewModels;

/// <summary>
/// The Create form: the request the form posts plus the rows its two sheets paint. It inherits the
/// request so the inputs keep their plain names (<c>PelangganId</c>, <c>Items[0].BarangId</c>) on
/// both GET and POST.
/// </summary>
public class CreateBonViewModel : CreateBonRequest
{
    /// <summary>Rows painted by the customer sheet; the controller fills them, the client never posts them.</summary>
    [BindNever]
    public List<BonCustomerOptionDto> Customers { get; set; } = [];

    /// <summary>Rows painted by the item picker; the controller fills them, the client never posts them.</summary>
    [BindNever]
    public PagedResult<BonBarangOptionDto> Barangs { get; set; } = new();

    /// <summary>
    /// Lines a rejected POST had already picked. The controller reads them back from the stock list,
    /// so the form repaints every line instead of starting over. The client never posts this list.
    /// </summary>
    [BindNever]
    public List<BonLineDto> Lines { get; set; } = [];
}
