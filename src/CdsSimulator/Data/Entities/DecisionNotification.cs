namespace Defra.TradeImportsCdsSimulator.Data.Entities;

public class DecisionNotification
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }
    public required string Xml { get; init; }
}
