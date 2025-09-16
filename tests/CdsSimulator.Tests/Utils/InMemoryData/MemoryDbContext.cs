using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;

public class MemoryDbContext : IDbContext
{
    public IMongoCollectionSet<DecisionNotification> DecisionNotifications { get; } =
        new MemoryCollectionSet<DecisionNotification>();

    public IMongoCollectionSet<ErrorNotification> ErrorNotifications { get; } =
        new MemoryCollectionSet<ErrorNotification>();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
