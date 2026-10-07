using System.Net.Http.Json;
using System.Text.Json;
using RestEase;
using WireMock.Client;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.Clients;

public class WireMockClient
{
    private const string BtmsClearanceRequestMappingId = "btms-clearance-request";
    private static readonly Uri s_wireMockUri = new("http://localhost:9090");
    private readonly HttpClient _httpClient = new() { BaseAddress = s_wireMockUri };

    public WireMockClient()
    {
        WireMockAdminApi.ResetMappingsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ResetRequestsAsync().GetAwaiter().GetResult();
    }

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>("http://localhost:9090");

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _httpClient.DeleteAsync($"/__admin/mappings/{BtmsClearanceRequestMappingId}", cancellationToken);
        await _httpClient.PostAsync("/__admin/requests/reset", content: null, cancellationToken);
    }

    public async Task StubBtmsClearanceRequestAsync(CancellationToken cancellationToken = default)
    {
        var mapping = new
        {
            priority = 1,
            request = new
            {
                method = "POST",
                urlPath = "/ITSW/CDS/SubmitImportDocumentCDSFacadeService",
            },
            response = new
            {
                status = 200,
                body = "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Body /></soap:Envelope>",
            },
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/__admin/mappings?id={BtmsClearanceRequestMappingId}",
            mapping,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> WasBtmsClearanceRequestPostedAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/__admin/requests/count",
            new
            {
                method = "POST",
                urlPath = "/ITSW/CDS/SubmitImportDocumentCDSFacadeService",
            },
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.TryGetProperty("count", out var countElement) && countElement.GetInt32() > 0;
    }

}
