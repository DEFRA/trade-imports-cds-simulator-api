using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CdsSimulator.BtmsClient.Tests
{
    public class ServiceRegistrationTests
    {
        [Fact]
        public void AddBtmsClient_RegistersOptionsAndClient()
        {
            // Arrange: in-memory configuration
            var inMemory = new Dictionary<string, string?>
            {
                ["BtmsClient:GatewayBaseUrl"] = "http://localhost:1234",
                ["BtmsClient:UsernameToken"] = "user",
                ["BtmsClient:Password"] = "pass",
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBtmsClient(configuration);
            var sp = services.BuildServiceProvider();

            // Assert
            var opts = sp.GetService<IOptions<BtmsClientOptions>>();
            Assert.NotNull(opts);
            Assert.Equal("http://localhost:1234", opts!.Value.GatewayBaseUrl);

            var client = sp.GetService<IBtmsGatewayClient>();
            Assert.NotNull(client);
        }
    }
}
