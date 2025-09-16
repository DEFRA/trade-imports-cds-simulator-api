using Azure.Core;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Endpoints;
using MongoDB.Driver;

namespace Defra.TradeImportsCdsSimulator.Data;

public static class QueryableExtensions
{
    public static async Task<List<TSource>> ToListWithFallbackAsync<TSource>(
        this IQueryable<TSource> source,
        CancellationToken cancellationToken = default
    )
    {
        if (source is IAsyncCursorSource<TSource> cursorSource)
        {
            return await cursorSource.ToListAsync(cancellationToken);
        }

        return source.AsEnumerable().ToList();
    }

    public static IQueryable<Notification> ApplyNotificationQuery(this IQueryable<Notification> query, GetQuery request)
    {
        if (!string.IsNullOrEmpty(request.Mrn))
        {
            query = from decision in query where decision.Mrn == request.Mrn select decision;
        }

        if (request.From.HasValue)
        {
            query = from decision in query where decision.Timestamp >= request.From select decision;
        }

        if (request.To.HasValue)
        {
            query = from decision in query where decision.Timestamp < request.To select decision;
        }

        return query;
    }
}
