using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddMongoDb(
                provider => provider.GetRequiredService<IMongoDatabase>(),
                timeout: TimeSpan.FromSeconds(10),
                tags: [WebApplicationExtensions.Extended]
            );
        return services;
    }
}
