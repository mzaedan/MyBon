namespace MyBon.Models.Bon.DTOs;

/// <summary>
/// Everything the Create form paints: the customers its sheet lists and the stock items it can add
/// as lines. Built once per GET so the page arrives with both sheets filled.
/// </summary>
public class BonFormDto
{
    public List<BonCustomerOptionDto> Customers { get; set; } = [];

    public List<BonBarangOptionDto> Barangs { get; set; } = [];
}
