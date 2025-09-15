using Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IDbContext
{
    IMongoCollectionSet<DecisionNotification> DecisionNotifications { get; }

    IMongoCollectionSet<ErrorNotification> ErrorNotifications { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
