using Microsoft.AspNetCore.Mvc;

namespace MyBon.Controllers;

/// <summary>
/// Serves the sign-in screens. The static slice only paints the form; authentication is planned in
/// <c>docs/plans/plan-login.md</c>.
/// </summary>
public class AuthController : BaseController
{
    /// <summary>Paints the sign-in screen.</summary>
    public IActionResult Login()
    {
        return View();
    }
}
