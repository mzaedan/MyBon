using Microsoft.AspNetCore.Mvc;

namespace MyBon.Controllers;

public class BarangController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Create() => View();
}
