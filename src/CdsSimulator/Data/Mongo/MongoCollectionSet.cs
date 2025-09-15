using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data.Mongo;

[ExcludeFromCodeCoverage]
public class MongoCollectionSet<T>(MongoDbContext dbContext, string collectionName = null!) : IMongoCollectionSet<T>
{
    private readonly List<T> _entitiesToInsert = [];

    private IQueryable<T> EntityQueryable => Collection.AsQueryable();

    public IEnumerator<T> GetEnumerator() => EntityQueryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => EntityQueryable.GetEnumerator();

    public Type ElementType => EntityQueryable.ElementType;
    public Expression Expression => EntityQueryable.Expression;
    public IQueryProvider Provider => EntityQueryable.Provider;

    public IMongoCollection<T> Collection { get; } =
        string.IsNullOrEmpty(collectionName)
            ? dbContext.Database.GetCollection<T>(typeof(T).Name)
            : dbContext.Database.GetCollection<T>(collectionName);

    public async Task Save(CancellationToken cancellationToken)
    {
        await Insert(cancellationToken);
    }

    private async Task Insert(CancellationToken cancellationToken)
    {
        if (_entitiesToInsert.Count != 0)
        {
            foreach (var item in _entitiesToInsert)
            {
                await Collection.InsertOneAsync(item, cancellationToken: cancellationToken);
            }

            _entitiesToInsert.Clear();
        }
    }

    public void Insert(T item)
    {
        _entitiesToInsert.Add(item);
    }
}
