namespace TaskFlow.Application.Repositories;

public interface IStockRepository
{
    /// <summary>One atomic statement: Stock = Stock - qty WHERE Id = id AND Stock >= qty. False = not enough stock.</summary>
    Task<bool> TryDecrementAsync(Guid productId, int quantity);
}
