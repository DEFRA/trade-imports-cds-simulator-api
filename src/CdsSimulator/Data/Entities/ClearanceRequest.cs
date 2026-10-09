namespace Defra.TradeImportsCdsSimulator.Data.Entities;

public class ClearanceRequest
{
    public required string Id { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string Mrn { get; init; }

    public required byte EntryVersionNumber { get; init; }

    public required string Xml { get; init; }
}
