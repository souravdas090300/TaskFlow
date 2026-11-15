namespace TaskFlow.Application.Repositories;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
    Task<IUnitOfWorkTransaction> BeginTransactionAsync();
}

/// <summary>Disposing without CommitAsync rolls the transaction back.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
