using Defra.TradeImportsCdsSimulator.Configuration;
using Defra.TradeImportsCdsSimulator.Metrics;
using Defra.TradeImportsCdsSimulator.Utils.CorrelationId;

namespace Defra.TradeImportsCdsSimulator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddOptions<CdpOptions>().Bind(configuration).ValidateDataAnnotations();

        return services;
    }

    public static IServiceCollection AddCustomMetrics(this IServiceCollection services)
    {
        services.AddTransient<MetricsMiddleware>();

        services.AddSingleton<RequestMetrics>();

        return services;
    }
}
