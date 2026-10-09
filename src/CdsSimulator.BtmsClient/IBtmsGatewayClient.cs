using CdsSimulator.BtmsClient.Models;

namespace CdsSimulator.BtmsClient;

public interface IBtmsGatewayClient
{
    Task<HttpResponseMessage> PostClearanceRequestAsync(
        ClearanceRequest request,
        CancellationToken cancellationToken = default
    );
}
