namespace MyBon.Domain.Abstractions;

/// <summary>
/// Per-request context. Services take this through constructor injection; controllers read it from
/// <c>BaseController.Context</c>; Razor views inject it directly.
/// </summary>
public interface IContext
{
    IIdentityContext Identity { get; }
}