using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data.Mongo;

[ExcludeFromCodeCoverage]
public class MongoIndexService(IMongoDatabase database, ILogger<MongoIndexService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateIndex(
            "DecisionNotificationIdx",
            Builders<Notification>.IndexKeys.Ascending(x => x.Mrn).Ascending(x => x.Timestamp),
            collectionName: nameof(IDbContext.DecisionNotifications),
            cancellationToken: cancellationToken
        );

        await CreateIndex(
            "ErrorNotificationIdx",
            Builders<Notification>.IndexKeys.Ascending(x => x.Mrn).Ascending(x => x.Timestamp),
            collectionName: nameof(IDbContext.ErrorNotifications),
            cancellationToken: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateIndex<T>(
        string name,
        IndexKeysDefinition<T> keys,
        bool unique = false,
        string? collectionName = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var indexModel = new CreateIndexModel<T>(
                keys,
                new CreateIndexOptions
                {
                    Name = name,
                    Background = true,
                    Unique = unique,
                }
            );

            var collection = string.IsNullOrEmpty(collectionName)
                ? database.GetCollection<T>(typeof(T).Name)
                : database.GetCollection<T>(collectionName);

            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Create index {Name} on {Collection}", name, typeof(T).Name);
        }
    }
}
