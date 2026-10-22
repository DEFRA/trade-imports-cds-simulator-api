namespace Defra.TradeImportsCdsSimulator.Data.Entities;

// A starting point for a clearance request, saved from the simulator's UI. The model is kept as the
// JSON the UI sent: it's the contract's shape, but only the UI reads it back.
public class Scenario
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Model { get; init; }
}
