using System.Net;
using System.Text;
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
    public async Task Post_WhenCorrelationIdAlreadySet_ShouldReturnBadRequest()
    {
        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS", "correlationId": "123" },
              "header": { "entryReference": "TESTMRN123",  "entryVersionNumber": 1, "manualAction": "N", "finalState": "1" }
            }
            """;

        var client = CreateClient();
        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenServiceCallTimestampAlreadySet_ShouldReturnBadRequest()
    {
        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS", "serviceCallTimestamp":"2024-01-01T00:00:00Z" },
              "header": { "entryReference":"TESTMRN123",  "entryVersionNumber": 1, "manualAction":"N", "finalState":"1" }
            }
            """;

        var client = CreateClient();
        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenEntryVersionNumberNotSet_ShouldReturnBadRequest()
    {
        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS" },
              "header": { "entryReference":"TESTMRN123", "manualAction":"N", "finalState":"1" }
            }
            """;

        var client = CreateClient();
        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenEntryReferenceNotSet_ShouldReturnBadRequest()
    {
        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS" },
              "header": { "entryVersionNumber":1, "manualAction": "N", "finalState": "1" }
            }
            """;

        var client = CreateClient();
        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenValidJson_ShouldReturnCreated()
    {
        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS" },
              "header": { "entryReference":"TESTMRN123",  "entryVersionNumber": 1, "manualAction":"N", "finalState":"1" }
            }
            """;

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
        ((MemoryCollectionSet<FinalisationDataSet>)_memDb.FinalisationNotifications).Insert(
            new Data.Entities.Finalisation
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Timestamp = DateTime.UtcNow,
                Mrn = "DuplicateMrn",
                EntryVersionNumber = 1,
                Xml = "<xml/>",
            }
        );

        var json = """
            {
              "serviceHeader": { "sourceSystem": "ALVS", "destinationSystem": "CDS" },
              "header": { "entryReference":"DuplicateMrn",  "entryVersionNumber": 1, "manualAction":"N", "finalState":"1" }
            }
            """;

        var client = CreateClient();

        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_WhenInvalidXml_ShouldReturnBadRequest()
    {
        var client = CreateClient();

        var soap = $"""
            <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope" xmlns:oas="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                 <soap:Header>
                     <oas:Security soap:role="system" soap:mustUnderstand="true">
                         <oas:UsernameToken></oas:UsernameToken>
                     </oas:Security>
                 </soap:Header>
                 <soap:Body>junk</soap:Body>
            </soap:Envelope>
            """;

        var content = new StringContent(soap, Encoding.UTF8, "application/xml");
        var response = await client.PostAsync(Testing.Endpoints.FinalisationNotifications.Post, content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
