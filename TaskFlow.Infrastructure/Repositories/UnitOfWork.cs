using Microsoft.EntityFrameworkCore.Storage;
using TaskFlow.Application.Repositories;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new UnitOfWorkTransaction(transaction);
    }

    private class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _disposed;

        public UnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _transaction.DisposeAsync();
                _disposed = true;
            }
        }
    }
}
