using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyBon.Domain.Constants;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Models.Auth.Requests;
using MyBon.Models.Common;

namespace MyBon.Controllers;

/// <summary>
/// Sign-in, sign-out, the refusal page, and the password a signed-in user changes for themselves.
/// </summary>
public class AuthController(
    IAuthService authService,
    IPenggunaService penggunaService) : BaseController
{
    /// <summary>How long a normal cookie lives before the user has to sign in again.</summary>
    private const int SessionHours = 8;

    /// <summary>How long **Ingat saya** keeps the cookie alive.</summary>
    private const int RememberDays = 30;

    private readonly IAuthService _authService = authService;
    private readonly IPenggunaService _penggunaService = penggunaService;

    /// <summary>Paints the sign-in screen, or sends an already signed-in user to their landing page.</summary>
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (Context?.Identity.IsAuthenticated == true)
            return RedirectToRoleLanding(Context.Identity.Peran);

        return View(new LoginRequest { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Verifies the credentials, writes the claims cookie, then returns to the local page the user
    /// originally asked for or to the landing page of their role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _authService.MasukAsync(request);

        if (!result.Success || result.Data is null)
        {
            AddErrorsToModelState(result);
            return View(request);
        }

        await SignInAsync(result.Data, request.IngatSaya);

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
            return Redirect(request.ReturnUrl);

        return RedirectToRoleLanding(result.Data.Peran);
    }

    /// <summary>Clears the cookie. A GET is not accepted, so a link cannot sign the user out.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    /// <summary>The page a signed-in user without the right role is sent to.</summary>
    [Authorize]
    public IActionResult Ditolak() => View();

    /// <summary>Paints the form for changing the signed-in user's own password.</summary>
    [Authorize]
    public IActionResult GantiPassword() => View(new GantiPasswordRequest());

    /// <summary>Verifies the old password and stores the new one.</summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GantiPassword(GantiPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _penggunaService.GantiPasswordAsync(request);

        if (!result.Success)
        {
            AddErrorsToModelState(result);
            return View(request);
        }

        TempData["AuthStatus"] = result.Message;

        return RedirectToAction(nameof(GantiPassword));
    }

    /// <summary>
    /// Writes the claims the rest of the app reads: account id, name, role, username, and — for a
    /// customer — the customer row the list is filtered by.
    /// </summary>
    private async Task SignInAsync(Pengguna pengguna, bool ingatSaya)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, pengguna.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, pengguna.Nama),
            new(ClaimTypes.Role, pengguna.Peran.ToString()),
            new("usr", pengguna.Username)
        };

        if (pengguna.PelangganId is int pelangganId)
            claims.Add(new Claim("pid", pelangganId.ToString(CultureInfo.InvariantCulture)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties
        {
            IsPersistent = ingatSaya,
            ExpiresUtc = ingatSaya
                ? DateTimeOffset.UtcNow.AddDays(RememberDays)
                : DateTimeOffset.UtcNow.AddHours(SessionHours)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    /// <summary>Sends the user to the home screen of their role.</summary>
    private IActionResult RedirectToRoleLanding(PeranPengguna? peran) =>
        peran == PeranPengguna.Pelanggan
            ? RedirectToAction("Index", "Bon")
            : RedirectToAction("Index", "Dashboard");
}
