using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Bon.Requests;
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
            request.Lines = await _bonService.ResolveLinesAsync(request.Items);

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

        // The re-rendered page keeps both sheets filled and every line it already picked, so a rejected
        // line can be corrected in place.
        await LoadFormOptionsAsync(request);
        request.Lines = await _bonService.ResolveLinesAsync(request.Items);

        return View(request);
    }

    /// <summary>
    /// Answers the Create form's item picker with one page of stock items, so the page asks for the
    /// rows it paints instead of carrying the whole stock list. AJAX callers get the rows as HTML; a
    /// failed lookup answers the <c>ErrorOr</c> JSON.
    /// </summary>
    public async Task<IActionResult> SearchBarang(string? q, int page = 1)
    {
        var result = await _bonService.SearchBarangAsync(q, page);

        if (IsAjaxRequest)
        {
            if (!result.Success || result.Data is null)
                return Json(result);

            return PartialView("_BarangRows", result.Data);
        }

        return ToActionResult(result, data => PartialView("_BarangRows", data));
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
    /// Writes one pick from the Tambah barang sheet into the bon and sends the shopkeeper back to the
    /// page, so Ringkasan, Daftar barang, and the sisa hint repaint from the notebook. A rejected pick
    /// reports its message on the page instead of answering a bare status code.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(AddBonItemRequest request)
    {
        if (!ModelState.IsValid || request.BarangId is null)
        {
            if (request.BonId <= 0)
                return RedirectToAction(nameof(Index));

            TempData["BonError"] = "Barang wajib dipilih.";
            return RedirectToAction(nameof(Details), new { id = request.BonId });
        }

        var result = await _bonService.AddItemAsync(request);

        if (result.Success)
        {
            TempData["BonStatus"] = result.Message;
        }
        else
        {
            // The field message reads better on the page than the generic "Validation failed.".
            var message = result.Errors?.SelectMany(entry => entry.Value).FirstOrDefault();
            TempData["BonError"] = message ?? result.Message ?? "Gagal menambah barang.";
        }

        // A bon that no longer exists has no page to return to, so the notebook itself answers.
        if (!result.Success && result.ErrorType == ErrorType.NotFound)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Details), new { id = request.BonId });
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
