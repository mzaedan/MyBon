using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyBon.Models.Bon.DTOs;
using MyBon.Models.Bon.Requests;

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
    public List<BonBarangOptionDto> Barangs { get; set; } = [];
}
