using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;

namespace MyBon.Controllers;

public class BarangController(IBarangService barangService) : BaseController
{
    private readonly IBarangService _barangService = barangService;

    public async Task<IActionResult> Index()
    {
        var result = await _barangService.GetAllAsync();

        return ToActionResult(result, data => View(data));
    }

    public IActionResult Create() => View();
}

