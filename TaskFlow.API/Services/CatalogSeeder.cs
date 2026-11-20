using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public static class CatalogSeeder
{
    private static string Img(string text) =>
        $"https://placehold.co/800x800/e2e8f0/6d28d9?text={text}";

    private sealed record CategorySeed(string Name, string Slug, string Description, int DisplayOrder, string ImageText);

    private sealed record ProductSeed(
        string CategorySlug,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        int Stock,
        string? Brand,
        string Sku,
        bool Featured,
        bool IsNew,
        string ImageText,
        int DayOffset);

    public static async Task SeedCatalogAsync(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var categorySeeds = new[]
            {
                new CategorySeed("Desk setups", "desk-setups", "Keyboards, mice, monitors and desk accessories.", 1, "Desk+Setups"),
                new CategorySeed("Kitchen", "kitchen", "Cookware, appliances and everyday kitchen tools.", 2, "Kitchen"),
                new CategorySeed("Sneakers", "sneakers", "Everyday and performance sneakers.", 3, "Sneakers"),
                new CategorySeed("Everyday carry", "everyday-carry", "Bags, bottles and travel essentials.", 4, "Everyday+Carry"),
            };

            var categorySlugs = categorySeeds.Select(c => c.Slug).ToArray();
            var existingCategories = await context.Categories
                .Where(c => categorySlugs.Contains(c.Slug))
                .ToDictionaryAsync(c => c.Slug, StringComparer.OrdinalIgnoreCase);

            foreach (var seed in categorySeeds)
            {
                if (existingCategories.ContainsKey(seed.Slug))
                    continue;

                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = seed.Name,
                    Slug = seed.Slug,
                    Description = seed.Description,
                    ImageUrl = Img(seed.ImageText),
                    DisplayOrder = seed.DisplayOrder,
                    IsActive = true,
                    CreatedAt = now,
                };
                context.Categories.Add(category);
                existingCategories[seed.Slug] = category;
            }

            await context.SaveChangesAsync();

            var productSeeds = new[]
            {
                new ProductSeed("desk-setups", "Slim Mechanical Keyboard", "slim-mechanical-keyboard", "Low-profile mechanical keyboard with hot-swappable switches and RGB underglow.", 89.00m, 1, "KeyLab", "KL-75", true, false, "Mechanical+Keyboard", 1),
                new ProductSeed("desk-setups", "Ergonomic Wireless Mouse", "ergonomic-wireless-mouse", "Contoured wireless mouse with silent clicks and 18-month battery life.", 34.50m, 24, "KeyLab", "EM-220", true, true, "Wireless+Mouse", 0),
                new ProductSeed("desk-setups", "27-inch 4K Monitor", "27-inch-4k-monitor", "Color-accurate 27\" 4K monitor with USB-C hub and height-adjust stand.", 329.00m, 8, "ViewPro", "VP-27U", false, true, "4K+Monitor", 0),
                new ProductSeed("desk-setups", "Folding Laptop Stand", "folding-laptop-stand", "Aluminum laptop stand with adjustable height and cable pass-through.", 42.00m, 30, "DeskCo", "LS-01", false, false, "Laptop+Stand", 3),

                new ProductSeed("kitchen", "Enamel Cast Iron Skillet", "enamel-cast-iron-skillet", "10-inch enamel cast iron skillet, oven safe to 260°C.", 54.00m, 18, "CasaKitchen", "CK-10", true, false, "Cast+Iron+Skillet", 2),
                new ProductSeed("kitchen", "Digital Kitchen Scale", "digital-kitchen-scale", "Precision 1g kitchen scale with tare and backlit display.", 19.90m, 40, "CasaKitchen", "KS-100", false, true, "Kitchen+Scale", 0),
                new ProductSeed("kitchen", "Burr Coffee Grinder", "burr-coffee-grinder", "40mm conical burr grinder with 60 grind settings.", 119.00m, 12, "BrewWorks", "BG-40", true, true, "Coffee+Grinder", 1),
                new ProductSeed("kitchen", "Silicone Utensil Set", "silicone-utensil-set", "Heat-resistant silicone utensil set with beechwood handles.", 28.00m, 35, "CasaKitchen", "SU-8", false, false, "Utensil+Set", 4),

                new ProductSeed("sneakers", "Court Classic White", "court-classic-white", "Minimal leather court sneaker with cushioned insole.", 98.00m, 22, "Stride", "CC-01", true, false, "White+Sneakers", 1),
                new ProductSeed("sneakers", "Runner Mesh Navy", "runner-mesh-navy", "Lightweight knit runner with responsive foam midsole.", 129.00m, 15, "Stride", "RN-02", true, true, "Running+Sneakers", 0),
                new ProductSeed("sneakers", "High-Top Suede", "high-top-suede", "Retro high-top in water-resistant suede with padded collar.", 112.00m, 9, "Stride", "HT-03", false, true, "High+Top+Sneakers", 2),
                new ProductSeed("sneakers", "Court Low Black", "court-low-black", "Everyday court low in full-grain black leather.", 89.00m, 27, "Stride", "CL-04", false, false, "Black+Sneakers", 5),

                new ProductSeed("everyday-carry", "Commuter Backpack 22L", "commuter-backpack-22l", "Water-resistant 22L backpack with padded 16\" laptop sleeve.", 86.00m, 14, "CarryLab", "CB-22", true, false, "Backpack", 1),
                new ProductSeed("everyday-carry", "Insulated Water Bottle", "insulated-water-bottle", "750ml double-wall bottle keeps drinks cold 24h.", 24.50m, 50, "CarryLab", "WB-750", false, true, "Water+Bottle", 0),
                new ProductSeed("everyday-carry", "Tech Organizer Pouch", "tech-organizer-pouch", "Compact pouch for cables, chargers and small accessories.", 18.00m, 60, "CarryLab", "TO-01", false, false, "Organizer+Pouch", 3),
                new ProductSeed("everyday-carry", "Weekender Duffel", "weekender-duffel", "35L carry-on duffel with separate shoe compartment.", 139.00m, 7, "CarryLab", "WD-35", true, true, "Duffel+Bag", 0),
            };

            var productSlugs = productSeeds.Select(p => p.Slug).ToArray();
            var existingProductSlugs = await context.Products
                .Where(p => productSlugs.Contains(p.Slug))
                .Select(p => p.Slug)
                .ToListAsync();
            var existingProductSlugSet = new HashSet<string>(existingProductSlugs, StringComparer.OrdinalIgnoreCase);
            var missingProducts = productSeeds.Where(p => !existingProductSlugSet.Contains(p.Slug)).ToList();
            if (missingProducts.Count == 0)
                return;

            var products = new List<Product>();
            foreach (var seed in missingProducts)
            {
                var id = Guid.NewGuid();
                products.Add(new Product
                {
                    Id = id,
                    Name = seed.Name,
                    Slug = seed.Slug,
                    Description = seed.Description,
                    Price = seed.Price,
                    Stock = seed.Stock,
                    Brand = seed.Brand,
                    SKU = seed.Sku,
                    IsFeatured = seed.Featured,
                    IsNewArrival = seed.IsNew,
                    IsActive = true,
                    CategoryId = existingCategories[seed.CategorySlug].Id,
                    CreatedAt = now.AddDays(-seed.DayOffset),
                    Images =
                    {
                        new ProductImage
                        {
                            Id = Guid.NewGuid(),
                            ProductId = id,
                            ImageUrl = Img(seed.ImageText),
                            AltText = seed.Name,
                            DisplayOrder = 0,
                            IsPrimary = true,
                            CreatedAt = now.AddDays(-seed.DayOffset),
                        }
                    },
                });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Catalog seeding skipped: {ex.Message}");
        }
    }
}
