using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Barang.Requests;
using MyBon.Models.Common;

namespace MyBon.Controllers;

public class BarangController(IBarangService barangService) : BaseController
{
    private readonly IBarangService _barangService = barangService;

    public async Task<IActionResult> Index(string? q)
    {
        // Only the first page is painted; the load-more button asks Search for the pages after it.
        var result = await _barangService.SearchAsync(q, 1);
        ViewData["SearchQuery"] = q?.Trim();

        return ToActionResult(result, data => View(data));
    }

    /// <summary>
    /// Renders one page of the card list. The list repaints itself through this action, so a keystroke
    /// and the load-more button both land here instead of the page reloading with every row up front.
    /// </summary>
    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        var result = await _barangService.SearchAsync(q, page);

        if (IsAjaxRequest)
        {
            if (!result.Success || result.Data is null)
                return Json(result);

            return PartialView("_Cards", result.Data);
        }

        ViewData["SearchQuery"] = q?.Trim();
        return ToActionResult(result, data => View(nameof(Index), data));
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

    public async Task<IActionResult> Details(int id)
    {
        var result = await _barangService.GetByIdAsync(id);

        return ToActionResult(result, barang =>
        {
            ViewData["Title"] = barang.Nama;
            ViewData["BackHref"] = Url.Action(nameof(Index), "Barang");

            return View(barang);
        });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _barangService.GetByIdAsync(id);

        return ToActionResult(result, barang => View(new UpdateBarangRequest
        {
            Id = barang.Id,
            Nama = barang.Nama,
            Satuan = barang.Satuan,
            // The field shows the price the way the list does, so the shopkeeper edits 34.000, not 34000.00.
            Harga = FormatHarga(barang.Harga)
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Bind the id from the route: a posted Id field would otherwise win over the route value.
    public async Task<IActionResult> Edit([FromRoute] int id, UpdateBarangRequest request)
    {
        if (id != request.Id)
            return ToActionResult(ErrorOr.CreateFailure(
                "Permintaan tidak valid.", ErrorType.Validation));

        if (!ModelState.IsValid)
            return InvalidInputResult(request);

        var result = await _barangService.UpdateAsync(id, request);

        return ToActionResult(
            result,
            updated =>
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

    public async Task<IActionResult> Delete(int id)
    {
        var result = await _barangService.GetByIdAsync(id);

        return ToActionResult(result, barang =>
        {
            ViewData["Title"] = "Hapus barang";
            ViewData["BackHref"] = Url.Action(nameof(Details), "Barang", new { id = barang.Id });

            return View(barang);
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _barangService.DeleteAsync(id);

        if (!result.Success || IsAjaxRequest)
            return ToActionResult(result);

        TempData["BarangStatus"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Prices read like the list: dots group thousands and a comma marks decimals (34.000, 1.234,5), so the
    /// edit form hands the service a value it already knows how to read.
    /// </summary>
    private static string FormatHarga(decimal harga) =>
        harga.ToString("#,##0.##", CultureInfo.GetCultureInfo("id-ID"));
}

