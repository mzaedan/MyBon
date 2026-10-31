using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Bon.Requests;
using MyBon.Models.Bon.ViewModels;
using MyBon.Models.Common;

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
    /// Answers the item picker with one page of stock items, so the page asks for the rows it paints
    /// instead of carrying the whole stock list. <paramref name="stepper"/> adds the quantity stepper
    /// the Tambah barang sheet on Details needs; the Create picker takes the same rows without it.
    /// AJAX callers get the rows as HTML; a failed lookup answers the <c>ErrorOr</c> JSON.
    /// </summary>
    public async Task<IActionResult> SearchBarang(string? q, int page = 1, bool stepper = false)
    {
        // The partial asks this flag back, so the rows it paints always match the sheet that asked.
        ViewData["Stepper"] = stepper;

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
    /// Writes one pick from the Tambah barang sheet into the bon — the count its stepper showed — and
    /// sends the shopkeeper back to the page, so Ringkasan, Daftar barang, and the sisa hint repaint
    /// from the notebook. A rejected pick reports its message on the page instead of answering a bare
    /// status code.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(AddBonItemRequest request)
    {
        if (!ModelState.IsValid || request.BarangId is null)
        {
            if (request.BonId <= 0)
                return RedirectToAction(nameof(Index));

            // The field message reads better on the page than a hardcoded one: a missing barang and a
            // count outside the stepper's range each say what went wrong.
            TempData["BonError"] = FirstModelStateMessage() ?? "Barang wajib dipilih.";
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
            TempData["BonError"] = FirstErrorOrMessage(result) ?? result.Message ?? "Gagal menambah barang.";
        }

        // A bon that no longer exists has no page to return to, so the notebook itself answers.
        if (!result.Success && result.ErrorType == ErrorType.NotFound)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Details), new { id = request.BonId });
    }

    /// <summary>
    /// Records one save from the Catat pembayaran sheet — the amount typed, its date, and the note — and
    /// sends the shopkeeper back to the page, so Ringkasan, Riwayat pembayaran, and the sisa hint repaint
    /// from the notebook. A rejected field rides back under its own field in the sheet; a failure with no
    /// field (a missing bon, a database error) still reports on the page banner.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(AddBonPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            if (request.BonId <= 0)
                return RedirectToAction(nameof(Index));

            // Each message keeps its field, so the sheet explains itself where the field sits.
            TempData["BonPayJumlahError"] = ModelStateMessage(nameof(request.Jumlah));
            TempData["BonPayTanggalError"] = ModelStateMessage(nameof(request.Tanggal));

            // A rejected bind with no field message still has to say something, so the banner answers it.
            if (TempData["BonPayJumlahError"] is null && TempData["BonPayTanggalError"] is null)
                TempData["BonError"] = FirstModelStateMessage() ?? "Gagal mencatat pembayaran.";

            return RedirectToAction(nameof(Details), new { id = request.BonId });
        }

        var result = await _bonService.AddPaymentAsync(request);

        if (result.Success)
        {
            TempData["BonStatus"] = result.Message;
        }
        else if (result.ErrorType == ErrorType.Validation)
        {
            // The service keys its messages by field, so the sheet, not the page banner, answers them.
            TempData["BonPayJumlahError"] = FieldMessage(result, "Jumlah");
            TempData["BonPayTanggalError"] = FieldMessage(result, "Tanggal");

            if (TempData["BonPayJumlahError"] is null && TempData["BonPayTanggalError"] is null)
                TempData["BonError"] = result.Message ?? "Gagal mencatat pembayaran.";
        }
        else
        {
            // A missing bon and a database failure are page-level, not field validation.
            TempData["BonError"] = result.Message ?? "Gagal mencatat pembayaran.";
        }

        // A bon that no longer exists has no page to return to, so the notebook itself answers.
        if (!result.Success && result.ErrorType == ErrorType.NotFound)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Details), new { id = request.BonId });
    }

    /// <summary>
    /// Reads the first message the model state carries for a rejected POST, so the page shows what the
    /// field said instead of a hardcoded fallback.
    /// </summary>
    private string? FirstModelStateMessage() =>
        ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

    /// <summary>
    /// Reads the message the model state holds for one field of a rejected POST, so the payment sheet can
    /// paint it under that field instead of the page banner.
    /// </summary>
    private string? ModelStateMessage(string fieldName) =>
        ModelState.TryGetValue(fieldName, out var entry)
            ? entry.Errors
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
            : null;

    /// <summary>Reads the message an <see cref="ErrorOr"/> carries for one field, if it carries any.</summary>
    private static string? FieldMessage(ErrorOr result, string fieldName) =>
        result.Errors is not null && result.Errors.TryGetValue(fieldName, out var messages)
            ? messages.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
            : null;

    /// <summary>Reads the first field message an <see cref="ErrorOr"/> carries.</summary>
    private static string? FirstErrorOrMessage(ErrorOr result) =>
        result.Errors?.SelectMany(entry => entry.Value).FirstOrDefault();

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
