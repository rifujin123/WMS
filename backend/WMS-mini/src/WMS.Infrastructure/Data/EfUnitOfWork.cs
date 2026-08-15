using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly WmsDbContext _db;

    public EfUnitOfWork(WmsDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await action();
        await transaction.CommitAsync(cancellationToken);
    }
}
