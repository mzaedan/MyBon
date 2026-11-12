using MyBon.Domain.Abstractions;

namespace MyBon.Infrastructure.Middleware;

/// <summary>
/// Registers the request's <see cref="IContext"/> after the cookie has been read, so every later
/// resolution — controller, service, or view — sees the same identity snapshot.
/// </summary>
public sealed class ContextMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext httpContext, IContext context)
    {
        httpContext.Items[typeof(IContext)] = context;
        await _next(httpContext);
    }
}