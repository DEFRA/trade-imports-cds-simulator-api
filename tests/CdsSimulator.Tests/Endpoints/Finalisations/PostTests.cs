using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using BtmsGateway.Contract.CustomsDeclarations;
using CdsSimulator.BtmsClient;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit.Abstractions;
using FinalisationDataSet = Defra.TradeImportsCdsSimulator.Data.Entities.Finalisation;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Finalisations;

public class PostTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private readonly Utils.InMemoryData.MemoryDbContext _memDb = new();
    private readonly IBtmsGatewayClient _mockBtms = Substitute.For<IBtmsGatewayClient>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        _mockBtms
            .PostFinalisationNotificationAsync(Arg.Any<Finalisation>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)));

        services.RemoveAll<IDbContext>();
        services.RemoveAll<IBtmsGatewayClient>();
        services.AddSingleton<IDbContext>(_ => _memDb);
        services.AddSingleton<IBtmsGatewayClient>(_ => _mockBtms);
    }

    [Fact]
    public async Task Post_WhenValidJson_ShouldReturnCreated()
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

        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        _memDb.FinalisationNotifications.Should().NotBeEmpty();
        await _mockBtms
            .Received(1)
            .PostFinalisationNotificationAsync(Arg.Any<Finalisation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_WhenDuplicateMrnAndVersion_ShouldReturnConflict()
    {
        const string duplicateMrn = "DuplicateMrn";
        const int duplicateVersion = 1;

        ((MemoryCollectionSet<FinalisationDataSet>)_memDb.FinalisationNotifications).Insert(
            new Data.Entities.Finalisation
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Timestamp = DateTime.UtcNow,
                Mrn = duplicateMrn,
                EntryVersionNumber = duplicateVersion,
                Xml = "<xml/>",
            }
        );

        var finalisation = new Finalisation
        {
            Header = new FinalisationHeader
            {
                EntryReference = duplicateMrn,
                EntryVersionNumber = duplicateVersion,
                ManualAction = "N",
                FinalState = "1",
            },
            ServiceHeader = new ServiceHeader { DestinationSystem = "BTMS", SourceSystem = "CDS" },
        };

        var json = JsonSerializer.Serialize(finalisation);

        var client = CreateClient();

        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
