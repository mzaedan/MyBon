using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interface;
using MyBon.Models;

namespace MyBon.Controllers;

public class PelangganController : Controller
{
    private readonly IPelangganService _pelangganService;

    public PelangganController(IPelangganService pelangganService)
    {
        _pelangganService = pelangganService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _pelangganService.GetAllAsync();

        if (!result.Success)
            return View(Array.Empty<Pelanggan>());

        return View(result.Data);
    }

    public IActionResult Create() => View();

    public async Task<IActionResult> Details(int id)
    {
        var result = await _pelangganService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound();

        ViewData["Title"] = result.Data!.Nama;
        ViewData["BackHref"] = Url.Action("Index", "Pelanggan");

        return View(result.Data);
    }
}
