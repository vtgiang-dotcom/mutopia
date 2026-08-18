using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Components;
using OpenMU.PlayerWeb.Data;
using OpenMU.PlayerWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cookie authentication (simple, compatible with static SSR + interactive forms).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// OpenMU database (existing PostgreSQL schemas: config, data, guild).
var connectionString = builder.Configuration.GetConnectionString("OpenMu")
    ?? throw new InvalidOperationException("Connection string 'OpenMu' not found.");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Application services.
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<GuildService>();
builder.Services.AddScoped<ServerStatusService>();
builder.Services.AddScoped<BotService>();
builder.Services.AddScoped<VipService>();
builder.Services.AddScoped<WheelService>();
builder.Services.AddScoped<MarketplaceService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IS16AccountRepository, S16AccountRepository>();
builder.Services.AddHostedService<AccountSyncRetryService>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// HTTPS redirection disabled: container runs behind plain HTTP port mapping.
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapPost("/api/auth/login", async (HttpContext httpContext, AccountService accountService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var loginName = form["loginName"].ToString();
    var password = form["password"].ToString();

    var account = await accountService.AuthenticateAsync(loginName, password);
    if (account is null)
    {
        return Results.Redirect("/login?error=Invalid username or password.");
    }

    var isGm = await accountService.IsGameMasterAsync(account.Id);
    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, account.Id.ToString()),
        new(System.Security.Claims.ClaimTypes.Name, account.LoginName),
        new(System.Security.Claims.ClaimTypes.Role, isGm ? "GAME_MASTER" : "USER"),
    };

    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect("/account");
}).DisableAntiforgery().RequireRateLimiting("login");

app.MapGet("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapPost("/api/payment/webhook", async (HttpContext httpContext, PaymentService paymentService) =>
{
    using var reader = new System.IO.StreamReader(httpContext.Request.Body);
    var body = await reader.ReadToEndAsync();
    try {
        var payload = System.Text.Json.JsonDocument.Parse(body).RootElement;
        var result = await paymentService.HandleWebhookAsync(payload);
        return result ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
    } catch {
        return Results.BadRequest(new { success = false });
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
