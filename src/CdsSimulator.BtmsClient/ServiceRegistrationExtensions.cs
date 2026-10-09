using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CdsSimulator.BtmsClient
{
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Configure BTMS client options from configuration and register the typed HTTP client.
        /// Expects configuration section "BtmsClient" to be present (optional - defaults allowed).
        /// </summary>
        public static IServiceCollection AddBtmsClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind configuration section and register an IConfiguration-backed IOptions implementation
            var section = configuration.GetSection("BtmsClient");
            services.AddOptions<BtmsClientOptions>().Bind(section).ValidateOnStart();

            // Expose concrete BtmsClientOptions as a singleton resolved from IOptions so constructors that require the POCO can be activated by DI
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<BtmsClientOptions>>().Value);

            // Register the typed HTTP client for BtmsGatewayClient. The client will receive a configured HttpClient.
            services.AddHttpClient<IBtmsGatewayClient, BtmsGatewayClient>(
                (sp, http) =>
                {
                    var opts = sp.GetRequiredService<IOptions<BtmsClientOptions>>().Value;
                    if (!string.IsNullOrWhiteSpace(opts?.GatewayBaseUrl))
                    {
                        http.BaseAddress = new Uri(opts.GatewayBaseUrl, UriKind.Absolute);
                    }
                }
            );

            return services;
        }
    }
}
