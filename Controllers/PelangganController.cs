using Microsoft.AspNetCore.Mvc;

namespace MyBon.Controllers;

public class PelangganController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Create() => View();

    public IActionResult Details() => View();
}
