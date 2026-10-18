using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Common;
using MyBon.Models.Pelanggan.Requests;

namespace MyBon.Controllers;

public class PelangganController(IPelangganService pelangganService) : BaseController
{
    private readonly IPelangganService _pelangganService = pelangganService;

    public async Task<IActionResult> Index(string? q)
    {
        // Only the first page is painted; the load-more button asks Search for the pages after it.
        var result = await _pelangganService.SearchAsync(q, 1);
        ViewData["SearchQuery"] = q?.Trim();

        return ToActionResult(result, data => View(data));
    }

    /// <summary>
    /// Renders one page of the card list. The list repaints itself through this action, so a keystroke
    /// and the load-more button both land here instead of the page reloading with every row up front.
    /// </summary>
    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        var result = await _pelangganService.SearchAsync(q, page);

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

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _pelangganService.GetByIdAsync(id);
        return ToActionResult(result, pelanggan =>
        {
            var request = new UpdatePelangganRequest
            {
                Id = pelanggan.Id,
                Nama = pelanggan.Nama,
                NomorHp = pelanggan.NomorHp,
                Alamat = pelanggan.Alamat
            };
            return View(request);
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Bind the id from the route: a posted Id field would otherwise win over the route value.
    public async Task<IActionResult> Edit([FromRoute] int id, UpdatePelangganRequest request)
    {
        if (id != request.Id)
            return ToActionResult(ErrorOr.CreateFailure(
                "Permintaan tidak valid.", ErrorType.Validation));

        if (!ModelState.IsValid)
            return InvalidInputResult(request);

        var result = await _pelangganService.UpdateAsync(id, request);

        return ToActionResult(
            result,
            updated =>
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

    public async Task<IActionResult> Delete(int id)
    {
        var result = await _pelangganService.GetByIdAsync(id);
        return ToActionResult(result, pelanggan => View(pelanggan));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _pelangganService.DeleteAsync(id);

        if (!result.Success || IsAjaxRequest)
            return ToActionResult(result);

        TempData["PelangganStatus"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
