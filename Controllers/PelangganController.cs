using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;
using MyBon.Models.Pelanggan.Requests;

namespace MyBon.Controllers;

public class PelangganController(IPelangganService pelangganService) : BaseController
{
    private readonly IPelangganService _pelangganService = pelangganService;

    public async Task<IActionResult> Index()
    {
        var result = await _pelangganService.GetAllAsync();
        return ToActionResult(result, data => View(data));
    }

    public IActionResult Create()
    {
        return View(new CreatePelangganRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePelangganRequest request)
    {
        if (!ModelState.IsValid)
            return InvalidInputResult(request);

        var result = await _pelangganService.CreateAsync(request);

        return ToActionResult(
            result,
            created =>
            {
                TempData["PelangganStatus"] = result.Message;
                return RedirectToAction(nameof(Index));
            },
            failure =>
            {
                AddErrorsToModelState(failure);
                return View(request);
            });
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _pelangganService.GetByIdAsync(id);
        return ToActionResult(result, pelanggan =>
        {
            ViewData["Title"] = pelanggan.Nama;
            ViewData["BackHref"] = Url.Action(nameof(Index), "Pelanggan");

            return View(pelanggan);
        });
    }
}
