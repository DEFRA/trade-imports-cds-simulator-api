using CdsSimulator.BtmsClient.Models;

namespace CdsSimulator.BtmsClient;

public interface IBtmsGatewayClient
{
    Task<HttpResponseMessage> PostClearanceRequestAsync(
        AlvsClearanceRequest request,
        CancellationToken cancellationToken = default
    );
}
