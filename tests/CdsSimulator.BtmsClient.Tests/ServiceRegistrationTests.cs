using System;
using AwesomeAssertions;
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

        [Fact]
        public void AddBtmsClient_WhenGatewayBaseUrlIsConfigured_SetsHttpClientBaseAddress()
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["BtmsClient:GatewayBaseUrl"] = "http://gateway.example.com",
                ["BtmsClient:UsernameToken"] = "user",
                ["BtmsClient:Password"] = "pass",
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddBtmsClient(configuration);

            var sp = services.BuildServiceProvider();

            // Resolve via IHttpClientFactory to inspect the configured HttpClient
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(nameof(IBtmsGatewayClient));

            httpClient.BaseAddress.Should().Be(new Uri("http://gateway.example.com"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddBtmsClient_WhenGatewayBaseUrlIsNullOrWhiteSpace_DoesNotSetHttpClientBaseAddress(string? gatewayBaseUrl)
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["BtmsClient:GatewayBaseUrl"] = gatewayBaseUrl,
                ["BtmsClient:UsernameToken"] = "user",
                ["BtmsClient:Password"] = "pass",
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddBtmsClient(configuration);

            var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(nameof(IBtmsGatewayClient));

            httpClient.BaseAddress.Should().BeNull();
        }
    }
}
