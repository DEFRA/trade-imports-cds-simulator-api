using Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IDbContext
{
    IMongoCollectionSet<RawMessageEntity> RawMessages { get; }

    Task SaveChanges(CancellationToken cancellationToken);

    Task StartTransaction(CancellationToken cancellationToken);

    Task CommitTransaction(CancellationToken cancellationToken);
}
