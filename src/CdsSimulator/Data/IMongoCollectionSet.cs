using System.Linq.Expressions;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data;

public interface IMongoCollectionSet<T> : IQueryable<T>
{
    IMongoCollection<T> Collection { get; }

    void Insert(T item);

    // Replaces the item the filter matches, or inserts it if there's none.
    void Replace(Expression<Func<T, bool>> filter, T item);

    Task Save(CancellationToken cancellationToken);
}
