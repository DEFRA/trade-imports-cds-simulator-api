using BtmsGateway.Contract.CustomsDeclarations;

namespace CdsSimulator.BtmsClient;

public interface IBtmsGatewayClient
{
    Task<HttpResponseMessage> PostClearanceRequestAsync(
        ClearanceRequest request,
        CancellationToken cancellationToken = default
    );
}
