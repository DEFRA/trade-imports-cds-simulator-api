using System.Net;
using AwesomeAssertions;
using CdsSimulator.BtmsClient.Models;

namespace CdsSimulator.BtmsClient.Tests;

public class BtmsGatewayClientTests
{
    [Fact]
    public async Task PostClearanceRequestAsync_IncludesSoapEnvelopeAndEscapedHeader()
    {
        // Arrange
        string? capturedBody = null;

        var handler = new CaptureHandler((req, ct) =>
        {
            // capture content
            capturedBody = req.Content?.ReadAsStringAsync(ct).Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") });
        });

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
                ["AlvsClearanceRequest"] = new() { Path = BtmsGatewayClient.DefaultRoutePath }
            }
        };

        var client = new BtmsGatewayClient(httpClient, btmsOptions);

        var requestModel = new AlvsClearanceRequest
        {
            ServiceHeader = new AlvsClearanceRequestServiceHeader
            {
                SourceSystem = "CDS",
                DestinationSystem = "ALVS",
                CorrelationId = 123ul,
                ServiceCallTimestamp = DateTime.UtcNow,
            },
            Header = new AlvsClearanceRequestHeader
            {
                EntryReference = "290-000151G-02/01/2022",
            },
            Items = [new AlvsClearanceRequestItem { ItemNumber = 1 }]
        };

        // Act
        var resp = await client.PostClearanceRequestAsync(requestModel, "http://localhost:1234");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("<ALVSClearanceRequest");
        // header username should be XML-escaped inside username element
        capturedBody.Should().Contain("<oas:Username>test-username</oas:Username>");
        capturedBody.Should().Contain("<oas:Password");
        capturedBody.Should().Contain(password);
    }

    private class CaptureHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> onSend)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return onSend(request, cancellationToken);
        }
    }
}
