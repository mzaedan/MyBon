using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Constants;
using MyBon.Domain.Interfaces;
using MyBon.Models.Common;
using MyBon.Models.Pengguna.Requests;
using MyBon.Models.Pengguna.ViewModels;

namespace MyBon.Controllers;

/// <summary>
/// Account management: who can sign in, what role they hold, and whether they are still active.
/// Admin only — a customer never reaches this screen.
/// </summary>
[Authorize(Roles = Peran.Admin)]
public class PenggunaController(
    IPenggunaService penggunaService,
    IPelangganService pelangganService) : BaseController
{
    private readonly IPenggunaService _penggunaService = penggunaService;
    private readonly IPelangganService _pelangganService = pelangganService;

    public async Task<IActionResult> Index()
    {
        var result = await _penggunaService.DaftarAsync();

        return ToActionResult(result, data => View(new PenggunaListViewModel
        {
            Items = data,
            Status = TempData["PenggunaStatus"] as string,
            Error = TempData["PenggunaError"] as string
        }));
    }

    /// <summary>
    /// Paints the create form. With `?pelangganId=` it creates the customer's account; without it,
    /// it creates another admin account.
    /// </summary>
    public async Task<IActionResult> Create(int? pelangganId)
    {
        var namaPelanggan = await LoadCustomerAsync(pelangganId);
        SetCreateTitle(pelangganId);

        return View(new CreatePenggunaRequest
        {
            PelangganId = pelangganId,
            Nama = namaPelanggan ?? string.Empty
        });
    }

    /// <summary>Creates the account and returns to the customer page or to the account list.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePenggunaRequest request)
    {
        if (!ModelState.IsValid)
            return await RejectedCreateAsync(request);

        var result = await _penggunaService.BuatAsync(request);

        if (!result.Success || result.Data is null)
        {
            AddErrorsToModelState(result);
            return await RejectedCreateAsync(request);
        }

        TempData["PenggunaStatus"] = result.Message;

        return request.PelangganId is int pelangganId
            ? RedirectToAction("Details", "Pelanggan", new { id = pelangganId })
            : RedirectToAction(nameof(Index));
    }

    /// <summary>Sets a new password on the chosen account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _penggunaService.ResetPasswordAsync(request);
        SetNotice(result);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Enables or disables the chosen account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAktif(int id, bool aktif)
    {
        var result = await _penggunaService.SetAktifAsync(id, aktif);
        SetNotice(result);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Re-renders the create form with its messages and, when creating a customer account, the customer name.</summary>
    private async Task<IActionResult> RejectedCreateAsync(CreatePenggunaRequest request)
    {
        await LoadCustomerAsync(request.PelangganId);
        SetCreateTitle(request.PelangganId);

        return View(request);
    }

    private async Task<string?> LoadCustomerAsync(int? pelangganId)
    {
        if (pelangganId is not int id)
            return null;

        var pelanggan = await _pelangganService.GetByIdAsync(id);

        if (!pelanggan.Success || pelanggan.Data is null)
            return null;

        ViewData["NamaPelanggan"] = pelanggan.Data.Nama;

        return pelanggan.Data.Nama;
    }

    private void SetCreateTitle(int? pelangganId) =>
        ViewData["Title"] = pelangganId is null ? "Buat akun admin" : "Buat akun pelanggan";

    private void SetNotice(ErrorOr result)
    {
        if (result.Success)
            TempData["PenggunaStatus"] = result.Message;
        else
            TempData["PenggunaError"] = result.Message ?? "Gagal memperbarui akun.";
    }
}