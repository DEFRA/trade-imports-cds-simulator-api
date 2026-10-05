using CdsSimulator.BtmsClient.Models;

namespace Defra.TradeImportsCdsSimulator.Control.Models
{
    public record ClearanceRequestControlModel
    {
        public required AlvsClearanceRequest ALVSClearanceRequest { get; set; }
    }
}
