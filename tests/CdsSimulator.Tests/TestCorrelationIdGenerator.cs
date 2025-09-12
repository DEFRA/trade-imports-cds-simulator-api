using Defra.TradeImportsCdsSimulator.Utils.CorrelationId;

namespace Defra.TradeImportsCdsSimulator.Tests
{
    internal class TestCorrelationIdGenerator(string value) : ICorrelationIdGenerator
    {
        public string Generate()
        {
            return value;
        }
    }
}
