using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.UnitTests.Integration;

public class DatabaseConstraintTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private ApplicationDbContext? _context;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("taskflow_test")
            .WithUsername("taskflow")
            .WithPassword("test_password")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessedWebhookEvent_EventId_IsUnique()
    {
        // Arrange
        var eventId = "evt_test_duplicate_12345";

        // Insert first event via raw SQL (bypass EF Core change tracker)
        await _context!.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"ProcessedWebhookEvents\" (\"EventId\", \"ProcessedAt\") VALUES ({0}, {1})",
            eventId, DateTime.UtcNow);

        // Act - Try to insert duplicate EventId via raw SQL
        var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"ProcessedWebhookEvents\" (\"EventId\", \"ProcessedAt\") VALUES ({0}, {1})",
                eventId, DateTime.UtcNow));

        // Assert - Should throw PostgresException with unique_violation
        exception.SqlState.Should().Be("23505"); // unique_violation

        // Verify only one row exists
        var count = await _context.ProcessedWebhookEvents.CountAsync(e => e.EventId == eventId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Product_Stock_AtomicUpdatePreventsOverselling()
    {
        // Arrange - Create category and product with 1 unit in stock via EF Core
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Slug = "test-category",
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context!.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test",
            Price = 100.00m,
            Stock = 1,
            Slug = "test-product",
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var productId = product.Id;

        // Act - First atomic decrement (should succeed)
        var rowsAffected1 = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"Products\" SET \"Stock\" = \"Stock\" - {0} WHERE \"Id\" = {1} AND \"Stock\" >= {0}",
            1, productId);

        // Verify stock is now 0
        _context.ChangeTracker.Clear();
        var afterFirst = await _context.Products.FindAsync(productId);
        afterFirst!.Stock.Should().Be(0);

        // Act - Second atomic decrement (should fail - insufficient stock)
        var rowsAffected2 = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"Products\" SET \"Stock\" = \"Stock\" - {0} WHERE \"Id\" = {1} AND \"Stock\" >= {0}",
            1, productId);

        // Assert
        rowsAffected1.Should().Be(1); // First update succeeded
        rowsAffected2.Should().Be(0); // Second update failed (no rows affected)

        // Verify stock stayed at 0 (not negative)
        _context.ChangeTracker.Clear();
        var finalStock = await _context.Products.FindAsync(productId);
        finalStock!.Stock.Should().Be(0);
    }
}
