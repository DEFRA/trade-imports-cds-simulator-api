using System.Globalization;
using System.Net;
using System.Xml.Linq;
using AwesomeAssertions;
using CdsSimulator.BtmsClient.Models;
using Xunit;

namespace CdsSimulator.BtmsClient.Tests;

public class BtmsGatewayClientTests
{
    [Fact]
    public async Task PostClearanceRequestAsync_IncludesSoapEnvelopeAndEscapedHeader()
    {
        string? capturedBody = null;

        var handler = new CaptureHandler(
            (req, ct) =>
            {
                capturedBody = req.Content?.ReadAsStringAsync(ct).Result;
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") }
                );
            }
        );

        var httpClient = new HttpClient(handler);

        var username = "test-username";
        var password = "password";

        var btmsOptions = new BtmsClientOptions
        {
            GatewayBaseUrl = "http://localhost:1234",
            UsernameToken = username,
            Password = password,
            Routes = new Dictionary<string, BtmsClientRoute>
            {
                ["AlvsClearanceRequest"] = new() { Path = BtmsGatewayClient.DefaultRoutePath },
            },
        };

        var client = new BtmsGatewayClient(httpClient, btmsOptions);

        var resp = await client.PostClearanceRequestAsync(CreateRequestModel(), CancellationToken.None);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("<ALVSClearanceRequest");
        capturedBody.Should().Contain("<oas:Username>test-username</oas:Username>");
        capturedBody.Should().Contain("<oas:Password");
        capturedBody.Should().Contain(password);
    }

    [Fact]
    public async Task PostClearanceRequestAsync_PreservesMultiItemPayload_WhenSerializedToSoapBody()
    {
        string? capturedBody = null;

        var handler = new CaptureHandler(
            (req, ct) =>
            {
                capturedBody = req.Content?.ReadAsStringAsync(ct).Result;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        );

        var client = new BtmsGatewayClient(
            new HttpClient(handler),
            new BtmsClientOptions
            {
                GatewayBaseUrl = "http://localhost:1234",
                UsernameToken = "user",
                Password = "pass",
                Routes = new Dictionary<string, BtmsClientRoute>
                {
                    ["AlvsClearanceRequest"] = new() { Path = BtmsGatewayClient.DefaultRoutePath },
                },
            }
        );

        var response = await client.PostClearanceRequestAsync(CreateRichRequestModel(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedBody.Should().NotBeNullOrWhiteSpace();

        var soap = XDocument.Parse(capturedBody!);
        XNamespace soapNs = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace reqNs = "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com";

        var payload = soap.Root?.Element(soapNs + "Body")?.Element(reqNs + "ALVSClearanceRequest");
        payload.Should().NotBeNull();

        payload!
            .Element(reqNs + "Header")!
            .Element(reqNs + "EntryReference")!
            .Value.Should()
            .Be("290-000151G-02/01/2022");
        payload.Element(reqNs + "Header")!.Element(reqNs + "EntryVersionNumber")!.Value.Should().Be("3");
        payload.Element(reqNs + "Header")!.Element(reqNs + "PreviousVersionNumber")!.Value.Should().Be("2");

        var items = payload.Elements(reqNs + "Item").ToArray();
        items.Length.Should().Be(2);

        items[0].Element(reqNs + "ItemNumber")!.Value.Should().Be("1");
        items[0].Element(reqNs + "TaricCommodityCode")!.Value.Should().Be("2309105100");
        decimal.Parse(items[0].Element(reqNs + "ItemNetMass")!.Value, CultureInfo.InvariantCulture)
            .Should()
            .Be(5011.200m);
        items[0]
            .Element(reqNs + "Document")!
            .Element(reqNs + "DocumentReference")!
            .Value.Should()
            .Be("GBCVD2021.1396392");
        items[0].Element(reqNs + "Check")!.Element(reqNs + "DepartmentCode")!.Value.Should().Be("PHA");

        items[1].Element(reqNs + "ItemNumber")!.Value.Should().Be("2");
        items[1].Element(reqNs + "TaricCommodityCode")!.Value.Should().Be("2309103300");
        decimal.Parse(items[1].Element(reqNs + "ItemNetMass")!.Value, CultureInfo.InvariantCulture)
            .Should()
            .Be(3198.720m);
        items[1]
            .Element(reqNs + "Document")!
            .Element(reqNs + "DocumentReference")!
            .Value.Should()
            .Be("GBCVD2021.1396392");
        items[1].Element(reqNs + "Check")!.Element(reqNs + "DepartmentCode")!.Value.Should().Be("PHA");
    }

    [Fact]
    public async Task PostClearanceRequestAsync_WhenRouteIsMissing_ThrowsKeyNotFoundException()
    {
        var handler = new CaptureHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var client = new BtmsGatewayClient(
            new HttpClient(handler),
            new BtmsClientOptions
            {
                GatewayBaseUrl = "http://localhost:1234",
                UsernameToken = "user",
                Password = "pass",
                Routes = new Dictionary<string, BtmsClientRoute>
                {
                    ["SomeOtherRoute"] = new BtmsClientRoute { Path = "/x" },
                },
            }
        );

        var act = () => client.PostClearanceRequestAsync(CreateRequestModel(), CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Route 'AlvsClearanceRequest' was not configured under BtmsClient:Routes.");
    }

    private static ClearanceRequest CreateRequestModel()
    {
        return new ClearanceRequest
        {
            ServiceHeader = new ServiceHeader
            {
                SourceSystem = "CDS",
                DestinationSystem = "ALVS",
                CorrelationId = "193264645102865",
                ServiceCallTimestamp = DateTime.UtcNow,
            },
            Header = new Header { EntryReference = "290-000151G-02/01/2022", EntryVersionNumber = 1 },
            Items = [new Item { ItemNumber = 1 }],
        };
    }

    private static ClearanceRequest CreateRichRequestModel()
    {
        return new ClearanceRequest
        {
            ServiceHeader = new ServiceHeader
            {
                SourceSystem = "CHIEF",
                DestinationSystem = "ALVS",
                CorrelationId = "193264645102865",
                ServiceCallTimestamp = new DateTime(2022, 1, 12, 14, 21, 50, 286, DateTimeKind.Utc),
            },
            Header = new Header
            {
                EntryReference = "290-000151G-02/01/2022",
                EntryVersionNumber = 3,
                PreviousVersionNumber = 2,
                DeclarationUCR = "1GB116445386000-KDA35932Y1BHX",
                DeclarationType = "S",
                ArrivalDateTime = 202201031224,
                SubmitterTURN = 123456789012,
                DeclarantId = "GB123456789013",
                DeclarantName = "REDACTED",
                DispatchCountryCode = "CN",
                GoodsLocationCode = "GBSTN",
                MasterUCR = "GB/CNS1-SCT16S6LF00000",
            },
            Items =
            [
                new Item
                {
                    ItemNumber = 1,
                    CustomsProcedureCode = "4000000",
                    TaricCommodityCode = "2309105100",
                    GoodsDescription = "DOG CHEW",
                    ConsigneeId = "GB123456789012",
                    ConsigneeName = "REDACTED",
                    ItemNetMass = 5011.200m,
                    ItemOriginCountryCode = "CN",
                    Documents =
                    [
                        new Document
                        {
                            DocumentCode = "N853",
                            DocumentReference = "GBCVD2021.1396392",
                            DocumentStatus = "AE",
                            DocumentControl = "P",
                        },
                    ],
                    Checks = [new Check { CheckCode = "H222", DepartmentCode = "PHA" }],
                },
                new Item
                {
                    ItemNumber = 2,
                    CustomsProcedureCode = "4000000",
                    TaricCommodityCode = "2309103300",
                    GoodsDescription = "DOG CHEW",
                    ConsigneeId = "GB123456789012",
                    ConsigneeName = "REDACTED",
                    ItemNetMass = 3198.720m,
                    ItemOriginCountryCode = "CN",
                    Documents =
                    [
                        new Document
                        {
                            DocumentCode = "N853",
                            DocumentReference = "GBCVD2021.1396392",
                            DocumentStatus = "AE",
                            DocumentControl = "P",
                        },
                    ],
                    Checks = [new Check { CheckCode = "H222", DepartmentCode = "PHA" }],
                },
            ],
        };
    }

    private class CaptureHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> onSend)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return onSend(request, cancellationToken);
        }
    }
}
