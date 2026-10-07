using System.Net;
using System.Text;
using System.Xml.Serialization;
using CdsSimulator.BtmsClient;
using CdsSimulator.BtmsClient.Models;
using Defra.TradeImportsCdsSimulator.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.ClearanceRequests;

public class ClearanceRequestTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private readonly Utils.InMemoryData.MemoryDbContext _memDb = new();
    private readonly IBtmsGatewayClient _mockBtms = Substitute.For<IBtmsGatewayClient>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        // In-memory DB and minimal substitute for BTMS client
        _mockBtms
            .PostClearanceRequestAsync(Arg.Any<AlvsClearanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Replace app registrations with test doubles
        services.RemoveAll<IDbContext>();
        services.RemoveAll<IBtmsGatewayClient>();
        services.AddSingleton<IDbContext>(_ => _memDb);
        services.AddSingleton<IBtmsGatewayClient>(_ => _mockBtms);
    }

    [Fact]
    public async Task Post_WhenValidXml_ShouldReturnCreated()
    {
        var request = new AlvsClearanceRequest
        {
            ServiceHeader = new AlvsClearanceRequestServiceHeader { SourceSystem = "ALVS", DestinationSystem = "CDS" },
            Header = new AlvsClearanceRequestHeader { EntryVersionNumber = 1 },
            Items = [new AlvsClearanceRequestItem { ItemNumber = 1 }],
        };

        var client = CreateClient();

        var serializer = new XmlSerializer(typeof(AlvsClearanceRequest));
        var sb = new StringBuilder();
        await using (var sw = new StringWriter(sb))
        {
            serializer.Serialize(sw, request);
        }

        var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/xml");
        var response = await client.PostAsync("/clearanceRequest", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify DB and BTMS interactions occurred
        _memDb.ClearanceRequests.Should().NotBeEmpty();
        await _memDb.SaveChangesAsync(CancellationToken.None); // no-op but ensure method exists
        await _mockBtms
            .Received(1)
            .PostClearanceRequestAsync(Arg.Any<AlvsClearanceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_WhenValidJson_CaseInsensitive_ShouldReturnCreated()
    {
        var client = CreateClient();

        var json =
            @"{
          ""serviceHeader"": { ""sourceSystem"": ""ALVS"", ""destinationSystem"": ""CDS"" },
          ""header"": { ""entryVersionNumber"": 1 },
          ""items"": [ { ""itemNumber"": 1 } ]
        }";

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/clearanceRequest", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_WhenSoapEnvelope_ShouldReturnCreated()
    {
        var request = new AlvsClearanceRequest
        {
            ServiceHeader = new AlvsClearanceRequestServiceHeader { SourceSystem = "ALVS", DestinationSystem = "CDS" },
            Header = new AlvsClearanceRequestHeader { EntryVersionNumber = 1 },
            Items = [new AlvsClearanceRequestItem { ItemNumber = 1 }],
        };

        var client = CreateClient();

        // Serialize inner ALVSClearanceRequest element without XML declaration and with correct namespace
        var serializer = new XmlSerializer(typeof(AlvsClearanceRequest));
        var ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com");

        var sb = new StringBuilder();
        var settings = new System.Xml.XmlWriterSettings { OmitXmlDeclaration = true, Encoding = Encoding.UTF8 };
        await using (var sw = new StringWriter(sb))
        using (var xw = System.Xml.XmlWriter.Create(sw, settings))
        {
            serializer.Serialize(xw, request, ns);
        }

        var innerXml = sb.ToString();

        var soap = $"""
            <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope" xmlns:oas="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                 <soap:Header>
                     <oas:Security soap:role="system" soap:mustUnderstand="true">
                         <oas:UsernameToken></oas:UsernameToken>
                     </oas:Security>`
                 </soap:Header>
                 <soap:Body>{innerXml}</soap:Body>
            </soap:Envelope>
            """;

        var content = new StringContent(soap, Encoding.UTF8, "application/xml");
        var response = await client.PostAsync("/clearanceRequest", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify DB and BTMS interactions occurred
        _memDb.ClearanceRequests.Should().NotBeEmpty();
        await _memDb.SaveChangesAsync(CancellationToken.None);
        await _mockBtms
            .Received(1)
            .PostClearanceRequestAsync(Arg.Any<AlvsClearanceRequest>(), Arg.Any<CancellationToken>());
    }
}
