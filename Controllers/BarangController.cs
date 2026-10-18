using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;
using MyBon.Models.Barang.Requests;

namespace MyBon.Controllers;

public class BarangController(IBarangService barangService) : BaseController
{
    private readonly IBarangService _barangService = barangService;

    public async Task<IActionResult> Index()
    {
        var result = await _barangService.GetAllAsync();

        return ToActionResult(result, data => View(data));
    }

    public IActionResult Create()
    {
        return View(new CreateBarangRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBarangRequest request)
    {
        if (!ModelState.IsValid)
            return InvalidInputResult(request);

        var result = await _barangService.CreateAsync(request);

        return ToActionResult(
            result,
            created =>
            {
                TempData["BarangStatus"] = result.Message;
                return RedirectToAction(nameof(Index));
            },
            failure =>
            {
                AddErrorsToModelState(failure);
                return View(request);
            });
    }
}

