using System.Collections;
using System.Linq.Expressions;
using Defra.TradeImportsCdsSimulator.Data;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;

public class MemoryCollectionSet<T> : IMongoCollectionSet<T>
{
    private readonly List<T> _data = [];

    private IQueryable<T> EntityQueryable => _data.AsQueryable();

    public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Type ElementType => EntityQueryable.ElementType;
    public Expression Expression => EntityQueryable.Expression;
    public IQueryProvider Provider => EntityQueryable.Provider;

    public IMongoCollection<T> Collection => throw new NotImplementedException();

    internal void AddTestData(T item) => _data.Add(item);

    public void Insert(T item) => throw new NotImplementedException();

    public Task Save(CancellationToken cancellationToken) => throw new NotImplementedException();
}
