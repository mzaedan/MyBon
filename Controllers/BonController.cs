using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;
using MyBon.Models.Bon.ViewModels;

namespace MyBon.Controllers;

public class BonController(IBonService bonService) : BaseController
{
    private readonly IBonService _bonService = bonService;

    public async Task<IActionResult> Index(string? q)
    {
        var result = await _bonService.SearchAsync(q, 1);
        ViewData["SearchQuery"] = q?.Trim();

        return ToActionResult(result, data => View(data));
    }

    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        var result = await _bonService.SearchAsync(q, page);

        if (IsAjaxRequest)
        {
            if (!result.Success || result.Data is null)
                return Json(result);

            return PartialView("_Cards", result.Data);
        }

        ViewData["SearchQuery"] = q?.Trim();
        return ToActionResult(result, data => View(nameof(Index), data));
    }

    public async Task<IActionResult> Create()
    {
        var form = await _bonService.GetFormAsync();

        return ToActionResult(form, data => View(new CreateBonViewModel
        {
            // The notebook starts every new page on today's date; the field stays editable.
            Tanggal = DateOnly.FromDateTime(DateTime.Today),
            Customers = data.Customers,
            Barangs = data.Barangs
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBonViewModel request)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest)
                return InvalidInputResult(request);

            await LoadFormOptionsAsync(request);
            return View(request);
        }

        var result = await _bonService.CreateAsync(request);

        if (result.Success)
        {
            TempData["BonStatus"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        if (IsAjaxRequest)
            return Json(result);

        AddErrorsToModelState(result);

        // The re-rendered page keeps both sheets filled, so a rejected line can be corrected in place.
        await LoadFormOptionsAsync(request);

        return View(request);
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _bonService.GetDetailAsync(id);

        return ToActionResult(result, bon =>
        {
            ViewData["Title"] = bon.Nomor;
            ViewData["BackHref"] = Url.Action(nameof(Index), "Bon");

            return View(bon);
        });
    }

    /// <summary>
    /// Refills the customer sheet and item picker after a rejected POST. A failed lookup is reported on the
    /// form instead of throwing, so the page can still be corrected.
    /// </summary>
    private async Task LoadFormOptionsAsync(CreateBonViewModel request)
    {
        var form = await _bonService.GetFormAsync();

        if (!form.Success || form.Data is null)
        {
            ModelState.AddModelError(
                string.Empty, form.Message ?? "Gagal memuat pilihan pelanggan dan barang.");
            return;
        }

        request.Customers = form.Data.Customers;
        request.Barangs = form.Data.Barangs;
    }
}
