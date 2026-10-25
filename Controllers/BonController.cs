using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;

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

    public IActionResult Create() => View();

    public IActionResult Details() => View();
}
