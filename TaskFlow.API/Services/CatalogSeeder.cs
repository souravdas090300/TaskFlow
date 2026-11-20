using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public static class CatalogSeeder
{
    private static string Img(string text) =>
        $"https://placehold.co/800x800/e2e8f0/6d28d9?text={text}";

    public static async Task SeedIfEmptyAsync(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.Categories.AnyAsync() || await context.Products.AnyAsync())
                return;

            var categories = new[]
            {
                new Category { Name = "Desk setups", Slug = "desk-setups", Description = "Keyboards, mice, monitors and desk accessories.", ImageUrl = Img("Desk+Setups"), DisplayOrder = 1 },
                new Category { Name = "Kitchen", Slug = "kitchen", Description = "Cookware, appliances and everyday kitchen tools.", ImageUrl = Img("Kitchen"), DisplayOrder = 2 },
                new Category { Name = "Sneakers", Slug = "sneakers", Description = "Everyday and performance sneakers.", ImageUrl = Img("Sneakers"), DisplayOrder = 3 },
                new Category { Name = "Everyday carry", Slug = "everyday-carry", Description = "Bags, bottles and travel essentials.", ImageUrl = Img("Everyday+Carry"), DisplayOrder = 4 },
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var bySlug = categories.ToDictionary(c => c.Slug, c => c.Id);
            var now = DateTime.UtcNow;
            var products = new List<Product>();

            void Add(string categorySlug, string name, string slug, string description, decimal price, int stock, string? brand, string sku, bool featured, bool isNew, string imageText, int dayOffset)
            {
                var id = Guid.NewGuid();
                products.Add(new Product
                {
                    Id = id,
                    Name = name,
                    Slug = slug,
                    Description = description,
                    Price = price,
                    Stock = stock,
                    Brand = brand,
                    SKU = sku,
                    IsFeatured = featured,
                    IsNewArrival = isNew,
                    IsActive = true,
                    CategoryId = bySlug[categorySlug],
                    CreatedAt = now.AddDays(-dayOffset),
                    Images =
                    {
                        new ProductImage { Id = Guid.NewGuid(), ProductId = id, ImageUrl = Img(imageText), AltText = name, DisplayOrder = 0, IsPrimary = true, CreatedAt = now.AddDays(-dayOffset) }
                    }
                });
            }

            Add("desk-setups", "Slim Mechanical Keyboard", "slim-mechanical-keyboard", "Low-profile mechanical keyboard with hot-swappable switches and RGB underglow.", 89.00m, 1, "KeyLab", "KL-75", true, false, "Mechanical+Keyboard", 1);
            Add("desk-setups", "Ergonomic Wireless Mouse", "ergonomic-wireless-mouse", "Contoured wireless mouse with silent clicks and 18-month battery life.", 34.50m, 24, "KeyLab", "EM-220", true, true, "Wireless+Mouse", 0);
            Add("desk-setups", "27-inch 4K Monitor", "27-inch-4k-monitor", "Color-accurate 27\" 4K monitor with USB-C hub and height-adjust stand.", 329.00m, 8, "ViewPro", "VP-27U", false, true, "4K+Monitor", 0);
            Add("desk-setups", "Folding Laptop Stand", "folding-laptop-stand", "Aluminum laptop stand with adjustable height and cable pass-through.", 42.00m, 30, "DeskCo", "LS-01", false, false, "Laptop+Stand", 3);

            Add("kitchen", "Enamel Cast Iron Skillet", "enamel-cast-iron-skillet", "10-inch enamel cast iron skillet, oven safe to 260°C.", 54.00m, 18, "CasaKitchen", "CK-10", true, false, "Cast+Iron+Skillet", 2);
            Add("kitchen", "Digital Kitchen Scale", "digital-kitchen-scale", "Precision 1g kitchen scale with tare and backlit display.", 19.90m, 40, "CasaKitchen", "KS-100", false, true, "Kitchen+Scale", 0);
            Add("kitchen", "Burr Coffee Grinder", "burr-coffee-grinder", "40mm conical burr grinder with 60 grind settings.", 119.00m, 12, "BrewWorks", "BG-40", true, true, "Coffee+Grinder", 1);
            Add("kitchen", "Silicone Utensil Set", "silicone-utensil-set", "Heat-resistant silicone utensil set with beechwood handles.", 28.00m, 35, "CasaKitchen", "SU-8", false, false, "Utensil+Set", 4);

            Add("sneakers", "Court Classic White", "court-classic-white", "Minimal leather court sneaker with cushioned insole.", 98.00m, 22, "Stride", "CC-01", true, false, "White+Sneakers", 1);
            Add("sneakers", "Runner Mesh Navy", "runner-mesh-navy", "Lightweight knit runner with responsive foam midsole.", 129.00m, 15, "Stride", "RN-02", true, true, "Running+Sneakers", 0);
            Add("sneakers", "High-Top Suede", "high-top-suede", "Retro high-top in water-resistant suede with padded collar.", 112.00m, 9, "Stride", "HT-03", false, true, "High+Top+Sneakers", 2);
            Add("sneakers", "Court Low Black", "court-low-black", "Everyday court low in full-grain black leather.", 89.00m, 27, "Stride", "CL-04", false, false, "Black+Sneakers", 5);

            Add("everyday-carry", "Commuter Backpack 22L", "commuter-backpack-22l", "Water-resistant 22L backpack with padded 16\" laptop sleeve.", 86.00m, 14, "CarryLab", "CB-22", true, false, "Backpack", 1);
            Add("everyday-carry", "Insulated Water Bottle", "insulated-water-bottle", "750ml double-wall bottle keeps drinks cold 24h.", 24.50m, 50, "CarryLab", "WB-750", false, true, "Water+Bottle", 0);
            Add("everyday-carry", "Tech Organizer Pouch", "tech-organizer-pouch", "Compact pouch for cables, chargers and small accessories.", 18.00m, 60, "CarryLab", "TO-01", false, false, "Organizer+Pouch", 3);
            Add("everyday-carry", "Weekender Duffel", "weekender-duffel", "35L carry-on duffel with separate shoe compartment.", 139.00m, 7, "CarryLab", "WD-35", true, true, "Duffel+Bag", 0);

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Catalog seeding skipped: {ex.Message}");
        }
    }
}
