using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;

public class MemoryDbContext : IDbContext
{
    public IMongoCollectionSet<Notification> DecisionNotifications { get; } = new MemoryCollectionSet<Notification>();

    public IMongoCollectionSet<Notification> ErrorNotifications { get; } = new MemoryCollectionSet<Notification>();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
