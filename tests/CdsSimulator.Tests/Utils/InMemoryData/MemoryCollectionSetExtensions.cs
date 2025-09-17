using Defra.TradeImportsCdsSimulator.Data;

namespace Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;

public static class MemoryCollectionSetExtensions
{
    public static void AddTestData<T>(this IMongoCollectionSet<T> set, T item)
    {
        if (set is MemoryCollectionSet<T> memoryCollectionSet)
        {
            memoryCollectionSet.AddTestData(item);
        }
    }
}
