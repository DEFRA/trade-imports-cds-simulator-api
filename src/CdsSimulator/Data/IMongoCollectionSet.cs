using System.Linq.Expressions;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IMongoCollectionSet<T> : IQueryable<T>
{
    IMongoCollection<T> Collection { get; }

    void Insert(T item);

    // Replaces the item the filter matches, or inserts it if there's none. Writes straight away, not on Save.
    Task Upsert(Expression<Func<T, bool>> filter, T item, CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
