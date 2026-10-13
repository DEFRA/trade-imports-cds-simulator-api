using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using BtmsGateway.Contract.CustomsDeclarations;
using CdsSimulator.BtmsClient;
using Defra.TradeImportsCdsSimulator.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Finalisations;

public class PutTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private readonly Utils.InMemoryData.MemoryDbContext _memDb = new();
    private readonly IBtmsGatewayClient _mockBtms = Substitute.For<IBtmsGatewayClient>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        _mockBtms
            .PostFinalisationNotificationAsync(Arg.Any<Finalisation>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        services.RemoveAll<IDbContext>();
        services.RemoveAll<IBtmsGatewayClient>();
        services.AddSingleton<IDbContext>(_ => _memDb);
        services.AddSingleton<IBtmsGatewayClient>(_ => _mockBtms);
    }

    [Fact]
    public async Task Put_WhenValidJson_ShouldReturnNoContent()
    {
        var finalisation = new Finalisation
        {
            Header = new FinalisationHeader
            {
                EntryReference = "TESTMRN123",
                EntryVersionNumber = 1,
                ManualAction = "N",
                FinalState = "1",
            },
            ServiceHeader = new ServiceHeader { DestinationSystem = "BTMS", SourceSystem = "CDS" },
        };

        var json = JsonSerializer.Serialize(finalisation);

        var client = CreateClient();

        var response = await client.PutAsync(
            Testing.Endpoints.FinalisationNotifications.Put,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _mockBtms
            .Received(1)
            .PostFinalisationNotificationAsync(Arg.Any<Finalisation>(), Arg.Any<CancellationToken>());
    }
}
