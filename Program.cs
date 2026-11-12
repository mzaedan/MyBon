using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using MyBon.Data;
using MyBon.Domain.Abstractions;
using MyBon.Domain.Entities;
using MyBon.Domain.Enums;
using MyBon.Domain.Interfaces;
using MyBon.Domain.Services;
using MyBon.Infrastructure.Contexts;
using MyBon.Infrastructure.Middleware;
using MyBon.Infrastructure.Persistence;
using MyBon.Models.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MyBonConnection")));

// Request context and password hashing
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContext, Context>();
builder.Services.AddScoped<IPasswordHasher<Pengguna>, PasswordHasher<Pengguna>>();

// Register services
builder.Services.AddScoped<IPelangganService, PelangganService>();
builder.Services.AddScoped<IBarangService, BarangService>();
builder.Services.AddScoped<IBonService, BonService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPenggunaService, PenggunaService>();

// Cookie authentication: the claims carry the account id, name, role, username, and — for a
// customer — the customer row every list is filtered by.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Ditolak";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Denial must not answer an AJAX call with a 302: the list partials read a JSON body.
        options.Events.OnRedirectToLogin = context =>
            ChallengeWithoutRedirect(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context =>
            ChallengeWithoutRedirect(context, StatusCodes.Status403Forbidden);
    });

builder.Services.AddAuthorization(options =>
{
    // Nothing is public unless it says so: every screen needs a signed-in account.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Five sign-in attempts per five minutes per client, so a script cannot guess passwords.
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<ContextMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

// Static assets fall under the authorization fallback policy, so an anonymous request for
// /css/site.css was answered with a redirect to /Auth/Login and the sign-in screen painted unstyled.
// Stylesheets, scripts, and images carry no data of their own; they stay public.
app.MapStaticAssets().AllowAnonymous();

// Short alias for the sign-in screen; /Auth/Login stays the canonical route.
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Auth", action = "Login" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// The first admin account is seeded outside design-time commands, so `dotnet ef` never runs it.
if (!EF.IsDesignTime)
    await PenggunaSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

app.Run();

/// <summary>
/// Answers a browser with the normal redirect, but an AJAX or JSON caller with the bare status and
/// an <see cref="ErrorOr"/> body, so a partial update never receives a sign-in page as HTML.
/// </summary>
static async Task ChallengeWithoutRedirect(
    RedirectContext<CookieAuthenticationOptions> context, int statusCode)
{
    if (!ExpectsJson(context.Request))
    {
        context.Response.Redirect(context.RedirectUri);
        return;
    }

    context.Response.StatusCode = statusCode;

    await context.Response.WriteAsJsonAsync(
        statusCode == StatusCodes.Status401Unauthorized
            ? ErrorOr.CreateFailure("Silakan masuk terlebih dahulu.", ErrorType.Unauthorized)
            : ErrorOr.CreateFailure("Kamu tidak punya akses ke halaman ini.", ErrorType.Forbidden));
}

static bool ExpectsJson(HttpRequest request) =>
    string.Equals(
        request.Headers["X-Requested-With"].ToString(),
        "XMLHttpRequest",
        StringComparison.OrdinalIgnoreCase)
    || request.Headers.Accept.Any(value =>
        value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
