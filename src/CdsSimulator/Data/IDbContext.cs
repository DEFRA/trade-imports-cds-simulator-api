using Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IDbContext
{
    IMongoCollectionSet<Notification> DecisionNotifications { get; }

    IMongoCollectionSet<Notification> ErrorNotifications { get; }

    IMongoCollectionSet<ClearanceRequest> ClearanceRequests { get; }

    IMongoCollectionSet<Finalisation> FinalisationNotifications { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
