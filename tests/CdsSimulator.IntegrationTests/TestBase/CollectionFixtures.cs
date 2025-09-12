using Defra.TradeImportsCdsSimulator.IntegrationTests.Clients;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.TestBase;

[CollectionDefinition("UsesWireMockClient")]
public class WireMockClientCollection : ICollectionFixture<WireMockClient>;
