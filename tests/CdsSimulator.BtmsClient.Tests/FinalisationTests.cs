using System.Net;
using System.Xml.Linq;
using AwesomeAssertions;
using BtmsGateway.Contract.CustomsDeclarations;
using Xunit;

namespace CdsSimulator.BtmsClient.Tests;

public class FinalisationTests
{
    [Fact]
    public async Task PostFinalisationAsync_IncludesSoapEnvelopeAndEscapedHeader()
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
                ["FinalisationNotificationRequest"] = new() { Path = BtmsGatewayClient.DefaultRoutePath },
            },
        };

        var client = new BtmsGatewayClient(httpClient, btmsOptions);

        var resp = await client.PostFinalisationNotificationAsync(CreateRichRequestModel(), CancellationToken.None);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("<Finalisation");
        capturedBody.Should().Contain("xmlns=\"http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com\"");
        capturedBody.Should().Contain("<oas:Username>test-username</oas:Username>");
        capturedBody.Should().Contain("<oas:Password");
        capturedBody.Should().Contain(password);
    }

    [Fact]
    public async Task PostFinalisationNotificationAsync_PreservesPayload_WhenSerializedToSoapBody()
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
                    ["FinalisationNotificationRequest"] = new() { Path = BtmsGatewayClient.DefaultRoutePath },
                },
            }
        );

        var response = await client.PostFinalisationNotificationAsync(CreateRichRequestModel(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedBody.Should().NotBeNullOrWhiteSpace();

        var soap = XDocument.Parse(capturedBody!);
        XNamespace soapNs = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace reqNs = "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com";

        var payload = soap.Root?.Element(soapNs + "Body")?.Element(reqNs + "FinalisationNotificationRequest");
        payload.Should().NotBeNull();

        payload.Element(reqNs + "ServiceHeader")!.Element(reqNs + "SourceSystem")!.Value.Should().Be("CHIEF");
        payload.Element(reqNs + "ServiceHeader")!.Element(reqNs + "DestinationSystem")!.Value.Should().Be("ALVS");
        payload
            .Element(reqNs + "ServiceHeader")!
            .Element(reqNs + "CorrelationId")!
            .Value.Should()
            .Be("193264645102865");
        payload.Element(reqNs + "ServiceHeader")!.Element(reqNs + "SourceSystem")!.Value.Should().Be("CHIEF");

        payload
            .Element(reqNs + "Header")!
            .Element(reqNs + "EntryReference")!
            .Value.Should()
            .Be("290-000151G-02/01/2022");
        payload.Element(reqNs + "Header")!.Element(reqNs + "EntryVersionNumber")!.Value.Should().Be("3");
        payload.Element(reqNs + "Header")!.Element(reqNs + "FinalState")!.Value.Should().Be("1");
        payload.Element(reqNs + "Header")!.Element(reqNs + "DecisionNumber")!.Value.Should().Be("2");
        payload.Element(reqNs + "Header")!.Element(reqNs + "ManualAction")!.Value.Should().Be("N");
    }

    [Fact]
    public async Task PostFinalisationAsync_WhenRouteIsMissing_ThrowsKeyNotFoundException()
    {
        var handler = new CaptureHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

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

        var act = () => client.PostFinalisationNotificationAsync(CreateRichRequestModel(), CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Route 'FinalisationNotificationRequest' was not configured under BtmsClient:Routes.");
    }

    private static Finalisation CreateRichRequestModel()
    {
        return new Finalisation
        {
            ServiceHeader = new ServiceHeader
            {
                SourceSystem = "CHIEF",
                DestinationSystem = "ALVS",
                CorrelationId = "193264645102865",
                ServiceCallTimestamp = new DateTime(2022, 1, 12, 14, 21, 50, 286, DateTimeKind.Utc),
            },
            Header = new FinalisationHeader
            {
                EntryReference = "290-000151G-02/01/2022",
                EntryVersionNumber = 3,
                FinalState = "1",
                DecisionNumber = 2,
                ManualAction = "N",
            },
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
