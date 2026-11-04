using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    var clientId = builder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    // In development, allow running without Google credentials (for testing endpoints)
    if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(clientId))
    {
        Console.WriteLine("Warning: Google ClientId not configured. Authentication endpoints will not work.");
        return;
    }

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        throw new InvalidOperationException(
            "Google ClientId and ClientSecret must be configured in appsettings.json or environment variables. " +
            "Set Authentication:Google:ClientId and Authentication:Google:ClientSecret");
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Serve static files from wwwroot (React build output)
app.UseStaticFiles();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

// API endpoints
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
    .WithName("GetWeatherForecast")
    .AllowAnonymous();

// Database health check
app.MapGet("/db-health", (ApplicationDbContext context) =>
{
    try
    {
        context.Database.CanConnect();
        return Results.Ok(new { status = "healthy", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Database Connection Failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
})
    .WithName("DatabaseHealthCheck")
    .AllowAnonymous();

// Authentication endpoints
app.MapGet("/login", (string? returnUrl = "/") =>
{
    var properties = new AuthenticationProperties { RedirectUri = returnUrl };
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
})
    .WithName("Login")
    .AllowAnonymous();

app.MapGet("/logout", (HttpContext context, string? returnUrl = "/") =>
{
    context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
    return Results.Redirect(returnUrl ?? "/");
})
    .WithName("Logout")
    .AllowAnonymous();

app.MapGet("/auth/user", (HttpContext context) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new
        {
            isAuthenticated = true,
            name = context.User.Identity.Name,
            email = context.User.FindFirst("email")?.Value,
            claims = context.User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
    return Results.Unauthorized();
})
    .WithName("GetCurrentUser")
    .RequireAuthorization();

// SPA routing fallback - return index.html for non-API routes
app.MapFallbackToFile("index.html")
    .AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
