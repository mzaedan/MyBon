using Microsoft.AspNetCore.Mvc;

namespace MyBon.Controllers;

public class BonController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Create() => View();
}
