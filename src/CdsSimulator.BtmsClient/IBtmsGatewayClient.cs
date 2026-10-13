using BtmsGateway.Contract.CustomsDeclarations;

namespace CdsSimulator.BtmsClient;

public interface IBtmsGatewayClient
{
    Task<HttpResponseMessage> PostClearanceRequestAsync(
        ClearanceRequest request,
        CancellationToken cancellationToken = default
    );

    Task<HttpResponseMessage> PostFinalisationNotificationAsync(
        Finalisation finalisation,
        CancellationToken cancellationToken = default
    );
}
