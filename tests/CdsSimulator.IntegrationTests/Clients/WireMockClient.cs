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

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await _httpClient.DeleteAsync($"/__admin/mappings/{BtmsClearanceRequestMappingId}", cancellationToken);
        await _httpClient.PostAsync("/__admin/requests/reset", content: null, cancellationToken);
    }

    public async Task StubBtmsClearanceRequestAsync(CancellationToken cancellationToken)
    {
        var mapping = new
        {
            priority = 1,
            request = new { method = "POST", url = "/ITSW/CDS/SubmitImportDocumentCDSFacadeService" },
            response = new { status = 200, body = string.Empty },
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/__admin/mappings?id={BtmsClearanceRequestMappingId}",
            mapping,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }
}
