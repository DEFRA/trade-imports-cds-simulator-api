using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data.Mongo;

[ExcludeFromCodeCoverage]
public class MongoDbContext : IDbContext
{
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(IMongoDatabase database, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        Database = database;
        DecisionNotifications = new MongoCollectionSet<Notification>(this, nameof(DecisionNotifications));
        ErrorNotifications = new MongoCollectionSet<Notification>(this, nameof(ErrorNotifications));
    }

    internal IMongoDatabase Database { get; }

    public IMongoCollectionSet<Notification> DecisionNotifications { get; }

    public IMongoCollectionSet<Notification> ErrorNotifications { get; }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await DecisionNotifications.Save(cancellationToken);
            await ErrorNotifications.Save(cancellationToken);
        }
        catch (MongoCommandException mongoCommandException) when (mongoCommandException.Code == 112)
        {
            const string message = "Mongo write conflict - consumer will retry";
            _logger.LogWarning(mongoCommandException, message);

            // WriteConflict error: this operation conflicted with another operation. Please retry your operation or multi-document transaction
            // - retries are built into consumers of the data API
            throw new ConcurrencyException(message, mongoCommandException);
        }
        catch (MongoWriteException mongoWriteException) when (mongoWriteException.WriteError.Code == 11000)
        {
            const string message = "Mongo write error - consumer will retry";
            _logger.LogWarning(mongoWriteException, message);

            // A write operation resulted in an error. WriteError: { Category : "DuplicateKey", Code : 11000 }
            // - retries are built into consumers of the data API
            throw new ConcurrencyException(message, mongoWriteException);
        }
    }
}
