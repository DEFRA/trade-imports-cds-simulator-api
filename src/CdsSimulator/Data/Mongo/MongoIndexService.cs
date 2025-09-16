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
            Builders<DecisionNotification>.IndexKeys.Ascending(x => x.Mrn).Ascending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );

        await CreateIndex(
            "ErrorNotificationIdx",
            Builders<ErrorNotification>.IndexKeys.Ascending(x => x.Mrn).Ascending(x => x.Timestamp),
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
            await database
                .GetCollection<T>(typeof(T).Name)
                .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Create index {Name} on {Collection}", name, typeof(T).Name);
        }
    }
}
