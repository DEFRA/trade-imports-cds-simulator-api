using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IMongoCollectionSet<T> : IQueryable<T>
{
    IMongoCollection<T> Collection { get; }

    void Insert(T item);

    Task Save(CancellationToken cancellationToken);
}
