using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Repositories;

/// <summary>
/// Repository interface for Product entity operations.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Product>> GetAllAsync();
    Task<List<Product>> GetByCategoryAsync(Guid categoryId);
    Task<Product> AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<bool> ExistsBySlugAsync(string slug);
}
