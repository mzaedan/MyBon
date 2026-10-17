using Microsoft.AspNetCore.Mvc;
using MyBon.Domain.Enums;
using MyBon.Models.Common;

namespace MyBon.Controllers;

/// <summary>
/// Base class for every MVC controller. Centralises the translation of
/// <see cref="ErrorOr"/> results into action results so controllers never map
/// business errors to HTTP status codes by hand.
/// </summary>
public abstract class BaseController : Controller
{
    private const string AjaxRequestedWithValue = "XMLHttpRequest";
    private const string JsonContentType = "application/json";

    /// <summary>Gets a value indicating whether the current request expects a JSON response.</summary>
    protected bool IsAjaxRequest =>
        string.Equals(
            Request.Headers["X-Requested-With"].ToString(),
            AjaxRequestedWithValue,
            StringComparison.OrdinalIgnoreCase)
        || Request.Headers.Accept.Any(accept =>
            accept?.Contains(JsonContentType, StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>
    /// Converts an <see cref="ErrorOr"/> that carries no data into an action result.
    /// </summary>
    protected IActionResult ToActionResult(ErrorOr result)
    {
        if (IsAjaxRequest)
            return Json(result);

        return result.Success ? Ok() : FailureResult(result);
    }

    /// <summary>
    /// Converts an <see cref="ErrorOr{T}"/> into an action result, returning the data to the client as-is.
    /// </summary>
    protected IActionResult ToActionResult<T>(ErrorOr<T> result)
    {
        if (IsAjaxRequest)
            return Json(result);

        if (!result.Success || result.Data is null)
            return FailureResult(result);

        return Ok(result.Data);
    }

    /// <summary>
    /// Converts an <see cref="ErrorOr{T}"/> into an action result and runs
    /// <paramref name="onSuccess"/> when the operation succeeded (for example to render a view).
    /// AJAX callers always receive the <see cref="ErrorOr{T}"/> payload as JSON.
    /// </summary>
    protected IActionResult ToActionResult<T>(ErrorOr<T> result, Func<T, IActionResult> onSuccess)
    {
        if (IsAjaxRequest)
            return Json(result);

        if (!result.Success || result.Data is null)
            return FailureResult(result);

        return onSuccess(result.Data);
    }

    /// <summary>
    /// Converts an <see cref="ErrorOr{T}"/> into an action result, giving the caller full control of
    /// both branches — used when a failed page request must re-render its form instead of returning a status code.
    /// AJAX callers always receive the <see cref="ErrorOr{T}"/> payload as JSON.
    /// </summary>
    protected IActionResult ToActionResult<T>(
        ErrorOr<T> result,
        Func<T, IActionResult> onSuccess,
        Func<ErrorOr<T>, IActionResult> onFailure)
    {
        if (IsAjaxRequest)
            return Json(result);

        if (!result.Success || result.Data is null)
            return onFailure(result);

        return onSuccess(result.Data);
    }

    /// <summary>
    /// Handles a model that failed data annotation validation: page requests get the same view back so the
    /// messages render next to the inputs, AJAX requests get a 400 with the errors grouped by field.
    /// </summary>
    protected IActionResult InvalidInputResult<T>(T model)
    {
        if (!IsAjaxRequest)
            return View(model);

        return StatusCode(StatusCodes.Status400BadRequest, BuildValidationErrors());
    }

    /// <summary>
    /// Copies the field level errors of a failed <see cref="ErrorOr"/> into <see cref="ModelState"/>
    /// so validation messages can be rendered next to the inputs.
    /// </summary>
    protected void AddErrorsToModelState(ErrorOr result)
    {
        if (result.Errors is not { Count: > 0 })
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Terjadi kesalahan.");
            return;
        }

        foreach (var (field, messages) in result.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(field, message);
        }
    }

    /// <summary>Returns a 500 response for an unexpected failure.</summary>
    protected IActionResult ServerError() => StatusCode(StatusCodes.Status500InternalServerError);

    private ErrorOr BuildValidationErrors() =>
        ErrorOr.CreateValidationFailure(ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray()));

    private IActionResult FailureResult(ErrorOr result) => result.ErrorType switch
    {
        ErrorType.NotFound => NotFound(),
        ErrorType.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized),
        ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
        ErrorType.Validation => StatusCode(StatusCodes.Status400BadRequest),
        ErrorType.Conflict => StatusCode(StatusCodes.Status409Conflict),
        _ => ServerError()
    };
}
