using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc; // Added for [FromQuery]
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using TaskFlow.API.Middleware;
using TaskFlow.API.Services;
using TaskFlow.API.Models;
using TaskFlow.API.Endpoints;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Persistence;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Application.Validators;
using TaskFlow.Domain;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Services;
using FluentValidation;
using AspNetCoreRateLimit;

const string AdminRole = "Admin";
const string AddressRouteTemplate = "/api/addresses/{id}";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON options to handle PATCH requests properly
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Match the React client's camelCase JSON
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Register application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TaskFlow.Application.Services.CartService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CheckoutService>();

// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IUserDirectory, UserDirectory>();
builder.Services.AddScoped<IAddressLookup, AddressLookup>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPaymentIssueRepository, PaymentIssueRepository>();

// Register email service
builder.Services.AddHttpClient<ResendEmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});

var resendOptions = builder.Services.AddOptions<ResendOptions>()
    .Bind(builder.Configuration.GetSection("Resend"));
if (!builder.Environment.IsDevelopment())
{
    // ResendEmailService is the registered IEmailService outside Development: fail fast if not configured.
    resendOptions.Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey) && !string.IsNullOrWhiteSpace(o.FromEmail),
        "Resend:ApiKey and Resend:FromEmail must be configured in production.").ValidateOnStart();
}

// Fail fast on invalid order settings instead of silently mispricing orders.
builder.Services.AddOptions<OrderSettings>()
    .Bind(builder.Configuration.GetSection("OrderSettings"))
    .Validate(s => s.TaxRate >= 0m && s.TaxRate < 1m, "OrderSettings:TaxRate must be >= 0 and < 1.")
    .Validate(s => s.FreeShippingThreshold >= 0m, "OrderSettings:FreeShippingThreshold must be >= 0.")
    .Validate(s => s.ShippingCost >= 0m, "OrderSettings:ShippingCost must be >= 0.")
    .ValidateOnStart();

builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}

// Register payment gateway
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<StripeCheckoutService>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

// Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddHttpContextAccessor();

// Per-user fixed-window limit for Stripe checkout session creation.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("checkout", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Configure Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLongForDevelopment123!";
        Console.WriteLine("Warning: Using default JWT key for development. Set Jwt:Key in configuration for production.");
    }
    else
    {
        throw new InvalidOperationException("JWT Key must be configured in production environment");
    }
}

// Validate Stripe configuration in production
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
var stripeWebhookSecret = builder.Configuration["Stripe:WebhookSecret"];
var appPublicUrl = builder.Configuration["App:PublicUrl"]?.TrimEnd('/');
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrEmpty(stripeSecretKey))
    {
        throw new InvalidOperationException("Stripe:SecretKey must be configured in production environment");
    }
    if (string.IsNullOrEmpty(stripeWebhookSecret))
    {
        throw new InvalidOperationException("Stripe:WebhookSecret must be configured in production environment");
    }

    if (string.IsNullOrWhiteSpace(appPublicUrl) || !Uri.TryCreate(appPublicUrl, UriKind.Absolute, out var publicUri)
        || (publicUri.Scheme != Uri.UriSchemeHttps && publicUri.Scheme != Uri.UriSchemeHttp))
    {
        throw new InvalidOperationException("App:PublicUrl must be configured as an absolute http(s) URL in production environment");
    }
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskFlow";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TaskFlowUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
})
// Add Google Authentication Here:
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";

    options.CallbackPath = "/signin-google";

    if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(appPublicUrl))
    {
        var googleRedirectUri = new Uri(new Uri(appPublicUrl), "/signin-google").ToString();
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.RedirectUri = googleRedirectUri;
            return Task.CompletedTask;
        };
    }
});

builder.Services.AddAuthorization();

// Configure Stripe SDK once at startup.
if (!string.IsNullOrEmpty(stripeSecretKey))
{
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
}

var app = builder.Build();

// The app runs behind Nginx Proxy Manager, which sets X-Forwarded-* headers.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Use rate limiting
app.UseIpRateLimiting();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Global exception handling
app.UseMiddleware<GlobalExceptionHandler>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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

// ============ PRODUCT ENDPOINTS ============

// Get all products with filtering and pagination
app.MapGet("/api/products", async (ProductService productService,
    int page = 1,
    int pageSize = 20,
    Guid? categoryId = null,
    string? search = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? sortBy = null) =>
{
    var products = await productService.GetAllProductsAsync(page, pageSize, categoryId, search, minPrice, maxPrice, sortBy);
    return Results.Ok(products);
})
    .WithName("GetAllProducts")
    .AllowAnonymous();

// Get product by ID
app.MapGet("/api/products/{id}", async (Guid id, ProductService productService) =>
{
    var product = await productService.GetProductByIdAsync(id);
    return product != null ? Results.Ok(product) : Results.NotFound();
})
    .WithName("GetProductById")
    .AllowAnonymous();

// Get product by slug
app.MapGet("/api/products/slug/{slug}", async (string slug, ProductService productService) =>
{
    var product = await productService.GetProductBySlugAsync(slug);
    return product != null ? Results.Ok(product) : Results.NotFound();
})
    .WithName("GetProductBySlug")
    .AllowAnonymous();

// Create product (Admin only)
app.MapPost("/api/products", async (CreateProductDto dto, IValidator<CreateProductDto> validator, ProductService productService) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var product = await productService.CreateProductAsync(dto);
    return Results.Created($"/api/products/{product.Id}", product);
})
    .WithName("CreateProduct")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Update product (Admin only)
app.MapPut("/api/products/{id}", async (Guid id, UpdateProductDto dto, IValidator<UpdateProductDto> validator, ProductService productService) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var product = await productService.UpdateProductAsync(id, dto);
    return product != null ? Results.Ok(product) : Results.NotFound();
})
    .WithName("UpdateProduct")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Delete product (Admin only)
app.MapDelete("/api/products/{id}", async (Guid id, ProductService productService) =>
{
    var success = await productService.DeleteProductAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
})
    .WithName("DeleteProduct")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Get featured products
app.MapGet("/api/products/featured", async (ProductService productService, int limit = 10) =>
{
    var products = await productService.GetFeaturedProductsAsync(limit);
    return Results.Ok(products);
})
    .WithName("GetFeaturedProducts")
    .AllowAnonymous();

// Get new arrivals
app.MapGet("/api/products/new-arrivals", async (ProductService productService, int limit = 10) =>
{
    var products = await productService.GetNewArrivalsAsync(limit);
    return Results.Ok(products);
})
    .WithName("GetNewArrivals")
    .AllowAnonymous();

// ============ CATEGORY ENDPOINTS ============

// Get all categories
app.MapGet("/api/categories", async (CategoryService categoryService) =>
{
    var categories = await categoryService.GetAllCategoriesAsync();
    return Results.Ok(categories);
})
    .WithName("GetAllCategories")
    .AllowAnonymous();

// Get category by ID
app.MapGet("/api/categories/{id}", async (Guid id, CategoryService categoryService) =>
{
    var category = await categoryService.GetCategoryByIdAsync(id);
    return category != null ? Results.Ok(category) : Results.NotFound();
})
    .WithName("GetCategoryById")
    .AllowAnonymous();

// Get category by slug
app.MapGet("/api/categories/slug/{slug}", async (string slug, CategoryService categoryService) =>
{
    var category = await categoryService.GetCategoryBySlugAsync(slug);
    return category != null ? Results.Ok(category) : Results.NotFound();
})
    .WithName("GetCategoryBySlug")
    .AllowAnonymous();

// Create category (Admin only)
app.MapPost("/api/categories", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, CategoryService categoryService) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var category = await categoryService.CreateCategoryAsync(dto);
    return Results.Created($"/api/categories/{category.Id}", category);
})
    .WithName("CreateCategory")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Update category (Admin only)
app.MapPut("/api/categories/{id}", async (Guid id, UpdateCategoryDto dto, IValidator<UpdateCategoryDto> validator, CategoryService categoryService) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var category = await categoryService.UpdateCategoryAsync(id, dto);
    return category != null ? Results.Ok(category) : Results.NotFound();
})
    .WithName("UpdateCategory")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Delete category (Admin only)
app.MapDelete("/api/categories/{id}", async (Guid id, CategoryService categoryService) =>
{
    var success = await categoryService.DeleteCategoryAsync(id);
    return success ? Results.NoContent() : Results.BadRequest("Cannot delete category with products or subcategories");
})
    .WithName("DeleteCategory")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// ============ AUTH ENDPOINTS ============

// Register
app.MapPost("/api/auth/register", async (RegisterDto dto, AuthService authService) =>
{
    var result = await authService.RegisterAsync(dto);
    if (result == null)
    {
        return Results.BadRequest(new { error = "Email already exists" });
    }
    return Results.Ok(result);
})
    .WithName("Register")
    .AllowAnonymous();

// Login
app.MapPost("/api/auth/login", async (LoginDto dto, AuthService authService) =>
{
    var result = await authService.LoginAsync(dto);
    if (result == null)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(result);
})
    .WithName("JwtLogin")
    .AllowAnonymous();

// Get current user
app.MapGet("/api/auth/me", async (HttpContext context, AuthService authService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await authService.GetUserByIdAsync(userId);
    return user != null ? Results.Ok(user) : Results.NotFound();
})
    .WithName("GetCurrentUser")
    .RequireAuthorization();

// Update profile
app.MapPut("/api/auth/me", async (HttpContext context, UpdateUserProfileDto dto, AuthService authService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await authService.UpdateUserAsync(userId, dto);
    return user != null ? Results.Ok(user) : Results.NotFound();
})
    .WithName("UpdateProfile")
    .RequireAuthorization();

// Change password
app.MapPost("/api/auth/change-password", async (HttpContext context, ChangePasswordDto dto, AuthService authService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var success = await authService.ChangePasswordAsync(userId, dto);
    return success ? Results.Ok(new { message = "Password changed successfully" }) : Results.BadRequest(new { error = "Current password is incorrect" });
})
    .WithName("ChangePassword")
    .RequireAuthorization();

// Refresh access token using a refresh token
app.MapPost("/api/auth/refresh", async (RefreshTokenRequestDto dto, AuthService authService) =>
{
    var result = await authService.RefreshAsync(dto.RefreshToken);
    return result == null ? Results.Unauthorized() : Results.Ok(result);
})
    .WithName("RefreshToken")
    .AllowAnonymous();

// Logout: revoke the refresh token
app.MapPost("/api/auth/logout", async (HttpContext context, AuthService authService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
    {
        await authService.RevokeRefreshTokenAsync(userId);
    }
    return Results.Ok(new { message = "Logged out" });
})
    .WithName("Logout")
    .RequireAuthorization();

// Request a password reset email (always returns 200 to avoid user enumeration)
app.MapPost("/api/auth/forgot-password", async (ForgotPasswordDto dto, AuthService authService) =>
{
    await authService.RequestPasswordResetAsync(dto.Email);
    return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
})
    .WithName("ForgotPassword")
    .AllowAnonymous();

// Reset password using the emailed token
app.MapPost("/api/auth/reset-password", async (ResetPasswordDto dto, AuthService authService) =>
{
    var success = await authService.ResetPasswordAsync(dto.Token, dto.Password);
    return success
        ? Results.Ok(new { message = "Password reset successfully" })
        : Results.BadRequest(new { error = "Invalid or expired reset link" });
})
    .WithName("ResetPassword")
    .AllowAnonymous();

// Verify email using the emailed token
app.MapGet("/api/auth/verify-email", async (string token, AuthService authService) =>
{
    var success = await authService.VerifyEmailAsync(token);
    return success
        ? Results.Ok(new { message = "Email verified successfully" })
        : Results.BadRequest(new { error = "Invalid or expired verification link" });
})
    .WithName("VerifyEmail")
    .AllowAnonymous();

// ============ CART ENDPOINTS ============

// Get cart
app.MapGet("/api/cart", async (HttpContext context, CartService cartService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var cart = await cartService.GetCartAsync(userId);
    return Results.Ok(cart);
})
    .WithName("GetCart")
    .RequireAuthorization();

// Add to cart
app.MapPost("/api/cart/items", async (HttpContext context, AddToCartDto dto, CartService cartService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var cart = await cartService.AddToCartAsync(userId, dto);
    return Results.Ok(cart);
})
    .WithName("AddToCart")
    .RequireAuthorization();

// Update cart item
app.MapPut("/api/cart/items/{itemId}", async (HttpContext context, Guid itemId, UpdateCartItemDto dto, CartService cartService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var cart = await cartService.UpdateCartItemAsync(userId, itemId, dto);
    return Results.Ok(cart);
})
    .WithName("UpdateCartItem")
    .RequireAuthorization();

// Remove from cart
app.MapDelete("/api/cart/items/{itemId}", async (HttpContext context, Guid itemId, CartService cartService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var cart = await cartService.RemoveFromCartAsync(userId, itemId);
    return Results.Ok(cart);
})
    .WithName("RemoveFromCart")
    .RequireAuthorization();

// Clear cart
app.MapDelete("/api/cart", async (HttpContext context, CartService cartService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    await cartService.ClearCartAsync(userId);
    return Results.Ok(new { message = "Cart cleared" });
})
    .WithName("ClearCart")
    .RequireAuthorization();

// ============ ADDRESS ENDPOINTS ============

// Get addresses
app.MapGet("/api/addresses", async (HttpContext context, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var addresses = await addressService.GetAddressesAsync(userId);
    return Results.Ok(addresses);
})
    .WithName("GetAddresses")
    .RequireAuthorization();

// Get address by ID
app.MapGet(AddressRouteTemplate, async (HttpContext context, Guid id, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var address = await addressService.GetAddressByIdAsync(userId, id);
    return address != null ? Results.Ok(address) : Results.NotFound();
})
    .WithName("GetAddressById")
    .RequireAuthorization();

// Create address
app.MapPost("/api/addresses", async (HttpContext context, CreateAddressDto dto, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var address = await addressService.CreateAddressAsync(userId, dto);
    return Results.Created($"/api/addresses/{address.Id}", address);
})
    .WithName("CreateAddress")
    .RequireAuthorization();

// Update address (full replacement - requires all fields)
app.MapPut(AddressRouteTemplate, async (HttpContext context, Guid id, UpdateAddressDto dto, IValidator<UpdateAddressDto> validator, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var address = await addressService.UpdateAddressAsync(userId, id, dto);
    return address != null ? Results.Ok(address) : Results.NotFound();
})
    .WithName("UpdateAddress")
    .RequireAuthorization();

// Patch address (partial update - only updates provided fields)
app.MapPatch(AddressRouteTemplate, async (HttpContext context, Guid id, PatchAddressDto dto, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var address = await addressService.PatchAddressAsync(userId, id, dto);
    return address != null ? Results.Ok(address) : Results.NotFound();
})
    .WithName("PatchAddress")
    .RequireAuthorization();

// Delete address
app.MapDelete(AddressRouteTemplate, async (HttpContext context, Guid id, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var success = await addressService.DeleteAddressAsync(userId, id);
    return success ? Results.NoContent() : Results.NotFound();
})
    .WithName("DeleteAddress")
    .RequireAuthorization();

// Get default address
app.MapGet("/api/addresses/default/{type}", async (HttpContext context, string type, AddressService addressService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var address = await addressService.GetDefaultAddressAsync(userId, type);
    return address != null ? Results.Ok(address) : Results.NotFound();
})
    .WithName("GetDefaultAddress")
    .RequireAuthorization();

// ============ ORDER ENDPOINTS ============

// Get checkout summary
app.MapGet("/api/checkout", async (HttpContext context, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var checkout = await orderService.GetCheckoutSummaryAsync(userId);
    return checkout != null ? Results.Ok(checkout) : Results.NotFound();
})
    .WithName("GetCheckout")
    .RequireAuthorization();

// Create order
app.MapPost("/api/orders", async (HttpContext context, CreateOrderDto dto, IValidator<CreateOrderDto> validator, CheckoutService checkoutService, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }

    var result = await checkoutService.CreateOrderAsync(userId, dto.ShippingAddressId, dto.PaymentMethod.ToString());
    if (!result.IsSuccess)
    {
        return Results.BadRequest(new { error = result.Message, code = result.Error?.ToString() });
    }

    var order = result.OrderId.HasValue ? await orderService.GetOrderByIdAsync(userId, result.OrderId.Value) : null;
    return order != null ? Results.Created($"/api/orders/{order.Id}", order) : Results.BadRequest("Cannot create order");
})
    .WithName("CreateOrder")
    .RequireAuthorization();

// Get user's orders
app.MapGet("/api/orders", async (HttpContext context, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var orders = await orderService.GetUserOrdersAsync(userId);
    return Results.Ok(orders);
})
    .WithName("GetUserOrders")
    .RequireAuthorization();

// Get order by ID
app.MapGet("/api/orders/{id}", async (HttpContext context, Guid id, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var order = await orderService.GetOrderByIdAsync(userId, id);
    return order != null ? Results.Ok(order) : Results.NotFound();
})
    .WithName("GetOrderById")
    .RequireAuthorization();

// Cancel order
app.MapPost("/api/orders/{id}/cancel", async (HttpContext context, Guid id, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var success = await orderService.CancelOrderAsync(userId, id);
    return success ? Results.Ok(new { message = "Order cancelled" }) : Results.BadRequest("Cannot cancel this order");
})
    .WithName("CancelOrder")
    .RequireAuthorization();

// Confirm cash-on-delivery order (marks Paid, decrements stock atomically)
app.MapPost("/api/orders/{id}/cash-on-delivery", async (HttpContext context, Guid id, CheckoutService checkoutService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var result = await checkoutService.ProcessCashOnDeliveryAsync(id, userId);
    return result switch
    {
        CashOnDeliveryResult.Confirmed => Results.Ok(new { message = "Order confirmed" }),
        CashOnDeliveryResult.OrderNotFound => Results.NotFound(new { error = "Order not found" }),
        CashOnDeliveryResult.OutOfStock => Results.Conflict(new { error = "Insufficient stock to fulfil this order" }),
        CashOnDeliveryResult.NotPending => Results.Conflict(new { error = "Order is not pending" }),
        CashOnDeliveryResult.NotCashOnDelivery => Results.BadRequest(new { error = "Order is not cash on delivery" }),
        _ => Results.BadRequest(new { error = "Cannot confirm order" })
    };
})
    .WithName("ConfirmCashOnDelivery")
    .RequireAuthorization();

// Admin: Get all orders
app.MapGet("/api/admin/orders", async (OrderService orderService) =>
{
    var orders = await orderService.GetAllOrdersAsync();
    return Results.Ok(orders);
})
    .WithName("GetAllOrders")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Admin: Update order status
app.MapPut("/api/admin/orders/{id}/status", async (Guid id, string status, OrderService orderService) =>
{
    var success = await orderService.UpdateOrderStatusAsync(id, status);
    return success ? Results.Ok(new { message = "Order status updated" }) : Results.NotFound();
})
    .WithName("UpdateOrderStatus")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Admin: Update shipping tracking
app.MapPut("/api/admin/orders/{id}/tracking", async (Guid id, string trackingNumber, OrderService orderService) =>
{
    var success = await orderService.UpdateOrderTrackingAsync(id, trackingNumber);
    return success ? Results.Ok(new { message = "Tracking updated" }) : Results.NotFound();
})
    .WithName("UpdateOrderTracking")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Admin: Refund a paid Stripe order
app.MapPost("/api/admin/orders/{id}/refund", async (Guid id, OrderService orderService) =>
{
    var (ok, error) = await orderService.RefundOrderAsync(id);
    return ok
        ? Results.Ok(new { message = "Order refunded" })
        : error == "Order not found"
            ? Results.NotFound()
            : Results.BadRequest(new { error });
})
    .WithName("RefundOrder")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Admin: Get dashboard statistics
app.MapGet("/api/admin/stats", async (ApplicationDbContext context) =>
{
    var totalOrders = await context.Orders.CountAsync();
    var totalRevenue = await context.Orders.Where(o => o.Status == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Delivered").SumAsync(o => o.Total);
    var pendingOrders = await context.Orders.CountAsync(o => o.Status == "Pending");
    var processingOrders = await context.Orders.CountAsync(o => o.Status == "Processing");
    var shippedOrders = await context.Orders.CountAsync(o => o.Status == "Shipped");
    var deliveredOrders = await context.Orders.CountAsync(o => o.Status == "Delivered");
    var cancelledOrders = await context.Orders.CountAsync(o => o.Status == "Cancelled");
    var totalProducts = await context.Products.CountAsync();
    var lowStockProducts = await context.Products.CountAsync(p => p.Stock < 10);

    return Results.Ok(new
    {
        totalOrders,
        totalRevenue,
        pendingOrders,
        processingOrders,
        shippedOrders,
        deliveredOrders,
        cancelledOrders,
        totalProducts,
        lowStockProducts
    });
})
    .WithName("GetAdminStats")
    .RequireAuthorization(policy => policy.RequireRole(AdminRole));

// Map payment issue endpoints
app.MapPaymentIssueEndpoints();

// Create Stripe checkout session
app.MapPost("/api/stripe/checkout", async (HttpContext context, CreateCheckoutRequest body, StripeCheckoutService checkout) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var result = await checkout.CreateSessionAsync(body.OrderId, userId);
    return result.Ok
        ? Results.Ok(new { url = result.Url })
        : result.Error == "NotFound"
            ? Results.NotFound()
            : Results.BadRequest(new { error = result.Error });
})
    .WithName("CreateStripeCheckout")
    .RequireAuthorization()
    .RequireRateLimiting("checkout");

// Stripe webhook endpoint
app.MapPost("/api/stripe/webhook", async (HttpRequest request, IPaymentGateway paymentGateway, CheckoutService checkoutService) =>
{
    using var reader = new StreamReader(request.Body);
    var jsonPayload = await reader.ReadToEndAsync();
    var sigHeader = request.Headers["Stripe-Signature"];

    // Validate signature header exists
    if (string.IsNullOrEmpty(sigHeader))
    {
        Console.WriteLine("Webhook rejected: Missing Stripe-Signature header");
        return Results.StatusCode(400);
    }

    // Verify signature
    if (!paymentGateway.VerifyWebhookSignature(jsonPayload, sigHeader!))
    {
        Console.WriteLine("Webhook rejected: Invalid signature");
        return Results.StatusCode(400);
    }

    // Extract payment details
    var (paymentIntentId, orderId) = paymentGateway.GetPaymentDetailsFromEvent(jsonPayload, sigHeader!);
    if (paymentIntentId != null && orderId.HasValue)
    {
        // Extract actual Stripe event ID for idempotency
        try
        {
            var stripeEvent = Stripe.EventUtility.ConstructEvent(
                jsonPayload,
                sigHeader,
                builder.Configuration["Stripe:WebhookSecret"] ?? "",
                300
            );
            
            var result = await checkoutService.ProcessStripePaymentAsync(orderId.Value, paymentIntentId, stripeEvent.Id);

            // All successful results (Processed, AlreadyProcessed, StockConflict) return 200
            // Stripe should not retry these
            // Rejected and OrderNotFound also return 200 to avoid indefinite retries
            // but are logged at error level for admin review
            return Results.Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Webhook processing error: {e.Message}");
            return Results.StatusCode(500);
        }
    }

    return Results.Ok();
})
    .AllowAnonymous();

// Legacy Authentication endpoints (for Google OAuth)
app.MapGet("/login", (string? returnUrl = "/") =>
{
    var properties = new AuthenticationProperties { RedirectUri = returnUrl };
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
})
    .WithName("Login")
    .AllowAnonymous();

app.MapGet("/logout", (HttpContext context, string? returnUrl = "/") =>
{
    context.SignOutAsync(JwtBearerDefaults.AuthenticationScheme).Wait();
    return Results.Redirect(returnUrl ?? "/");
})
    .WithName("LogoutRedirect")
    .AllowAnonymous();

// SPA routing fallback - return index.html for non-API routes
app.MapFallbackToFile("index.html")
    .AllowAnonymous();

await app.RunAsync();