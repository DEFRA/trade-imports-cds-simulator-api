using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsCdsSimulator.Configuration;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsCdsSimulator.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();
        return services;
    }
}
