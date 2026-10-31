using MyBon.Models.Common;

namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// Everything the Create form paints: the customers its sheet lists and the first page of stock items
/// its picker shows. The picker asks for the pages after this one through the search endpoint.
/// </summary>
public class BonFormDto
{
    public List<BonCustomerOptionDto> Customers { get; set; } = [];

    public PagedResult<BonBarangOptionDto> Barangs { get; set; } = new();
}
