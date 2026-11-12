using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Constants;
using MyBon.Domain.Interfaces;

namespace MyBon.Controllers;

/// <summary>The shop summary. Admin only: a customer never sees the whole notebook.</summary>
[Authorize(Roles = Peran.Admin)]
public class DashboardController(IDashboardService dashboardService) : BaseController
{
    private readonly IDashboardService _dashboardService = dashboardService;

    public async Task<IActionResult> Index()
    {
        var result = await _dashboardService.GetSummaryAsync();

        return ToActionResult(result, data => View(data));
    }
}

