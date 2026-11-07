using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Interfaces;

namespace MyBon.Controllers;

public class DashboardController(IDashboardService dashboardService) : BaseController
{
    private readonly IDashboardService _dashboardService = dashboardService;

    public async Task<IActionResult> Index()
    {
        var result = await _dashboardService.GetSummaryAsync();

        return ToActionResult(result, data => View(data));
    }
}

