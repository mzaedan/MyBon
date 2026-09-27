using Microsoft.AspNetCore.Mvc;

namespace MyBon.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
