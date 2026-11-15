using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Persistence;

/// <summary>
/// Persistence abstraction implemented by Infrastructure's ApplicationDbContext.
/// Lets Application services use EF Core queries without referencing Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
