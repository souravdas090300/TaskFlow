using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TaskFlow.API.Services;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;
using TaskFlow.Infrastructure;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON options to handle PATCH requests properly
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use exact property names
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<StripeService>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

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
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

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
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

// Delete product (Admin only)
app.MapDelete("/api/products/{id}", async (Guid id, ProductService productService) =>
{
    var success = await productService.DeleteProductAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
})
    .WithName("DeleteProduct")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

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
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

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
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

// Delete category (Admin only)
app.MapDelete("/api/categories/{id}", async (Guid id, CategoryService categoryService) =>
{
    var success = await categoryService.DeleteCategoryAsync(id);
    return success ? Results.NoContent() : Results.BadRequest("Cannot delete category with products or subcategories");
})
    .WithName("DeleteCategory")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

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
app.MapGet("/api/addresses/{id}", async (HttpContext context, Guid id, AddressService addressService) =>
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
app.MapPut("/api/addresses/{id}", async (HttpContext context, Guid id, UpdateAddressDto dto, IValidator<UpdateAddressDto> validator, AddressService addressService) =>
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
app.MapPatch("/api/addresses/{id}", async (HttpContext context, Guid id, PatchAddressDto dto, AddressService addressService) =>
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
app.MapDelete("/api/addresses/{id}", async (HttpContext context, Guid id, AddressService addressService) =>
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
app.MapPost("/api/orders", async (HttpContext context, CreateOrderDto dto, IValidator<CreateOrderDto> validator, OrderService orderService) =>
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

    var order = await orderService.CreateOrderAsync(userId, dto);
    return order != null ? Results.Created($"/api/orders/{order.Id}", order) : Results.BadRequest("Cannot create order with empty cart");
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

// Create Stripe checkout session
app.MapPost("/api/stripe/checkout", async (HttpContext context, CreateCheckoutSessionDto dto, StripeService stripeService, OrderService orderService) =>
{
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    // Fetch the order to get the amount and order number
    var order = await orderService.GetOrderByIdAsync(userId, dto.OrderId);
    if (order == null)
    {
        return Results.NotFound("Order not found");
    }

    var session = await stripeService.CreateCheckoutSessionAsync(dto.OrderId, order.Total, dto.SuccessUrl, dto.CancelUrl, order.OrderNumber);
    return session != null ? Results.Ok(session) : Results.BadRequest("Failed to create checkout session");
})
    .WithName("CreateStripeCheckout")
    .RequireAuthorization();

// Stripe webhook endpoint
app.MapPost("/api/stripe/webhook", async (HttpRequest request, StripeService stripeService, OrderService orderService) =>
{
    using var reader = new StreamReader(request.Body);
    var jsonPayload = await reader.ReadToEndAsync();
    var sigHeader = request.Headers["Stripe-Signature"];

    if (!stripeService.VerifyWebhookSignature(jsonPayload, sigHeader))
    {
        return Results.StatusCode(400);
    }

    var (paymentIntentId, orderId) = stripeService.GetPaymentDetailsFromEvent(jsonPayload, sigHeader);
    if (paymentIntentId != null && orderId.HasValue)
    {
        var eventId = Guid.NewGuid().ToString(); // In production, use the actual event ID
        await orderService.ProcessStripePaymentAsync(orderId.Value, paymentIntentId, eventId);
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
    .WithName("Logout")
    .AllowAnonymous();

// SPA routing fallback - return index.html for non-API routes
app.MapFallbackToFile("index.html")
    .AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
