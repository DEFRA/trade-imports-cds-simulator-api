using System.Xml.Serialization;
using BtmsGateway.Contract.CustomsDeclarations;
using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Control.Declarations;

// What the simulator holds per declaration, for its UI: what was sent and what BTMS sent back.
public static class EndpointRouteBuilderExtensions
{
    public static void MapDeclarationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("clearance-requests", ListClearanceRequests)
            .RequireAuthorization(PolicyNames.Read)
            .Produces<DeclarationList>()
            .Produces(StatusCodes.Status400BadRequest);

        app.MapGet("clearance-requests/{mrn}", GetClearanceRequest)
            .RequireAuthorization(PolicyNames.Read)
            .Produces<Declaration>()
            .Produces(StatusCodes.Status404NotFound);
    }

    public record DeclarationSummary(string Mrn, byte EntryVersionNumber, DateTime Timestamp);

    public record DeclarationList(int Page, int PageSize, bool HasMore, List<DeclarationSummary> Items);

    public record RequestVersion(byte EntryVersionNumber, DateTime Timestamp, ClearanceRequest Request);

    public record FinalisationVersion(byte EntryVersionNumber, DateTime Timestamp, Finalisation Finalisation);

    public record Declaration(
        string Mrn,
        List<RequestVersion> Versions,
        List<FinalisationVersion> Finalisations,
        List<Decision> Decisions,
        List<ErrorNotification> Errors
    );

    // Declarations, one per MRN, the most recently sent first. One more than a page is read, to tell
    // whether another page follows.
    public static async Task<IResult> ListClearanceRequests(
        IDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["page"] = ["page must be 1 or more and pageSize from 1 to 100"] }
            );
        }

        var rows = await dbContext
            .ClearanceRequests.GroupBy(request => request.Mrn)
            .Select(versions => new
            {
                Mrn = versions.Key,
                EntryVersionNumber = versions.Max(request => request.EntryVersionNumber),
                Timestamp = versions.Max(request => request.Timestamp),
            })
            .OrderByDescending(row => row.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListWithFallbackAsync(cancellationToken);

        return Results.Ok(
            new DeclarationList(
                page,
                pageSize,
                rows.Count > pageSize,
                rows.Take(pageSize)
                    .Select(row => new DeclarationSummary(row.Mrn, row.EntryVersionNumber, row.Timestamp))
                    .ToList()
            )
        );
    }

    // Everything the simulator holds for one MRN: what was sent, as the contract's JSON, and what
    // BTMS sent back, read out of its SOAP.
    public static async Task<IResult> GetClearanceRequest(
        string mrn,
        IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var versions = await (
            from request in dbContext.ClearanceRequests
            where request.Mrn == mrn
            orderby request.Timestamp
            select request
        ).ToListWithFallbackAsync(cancellationToken);
        if (versions.Count == 0)
        {
            return Results.NotFound($"No clearance request for {mrn} exists in the simulator");
        }

        var finalisations = await (
            from finalisation in dbContext.FinalisationNotifications
            where finalisation.Mrn == mrn
            orderby finalisation.Timestamp
            select finalisation
        ).ToListWithFallbackAsync(cancellationToken);
        var decisions = await (
            from decision in dbContext.DecisionNotifications
            where decision.Mrn == mrn
            orderby decision.Timestamp
            select decision
        ).ToListWithFallbackAsync(cancellationToken);
        var errors = await (
            from error in dbContext.ErrorNotifications
            where error.Mrn == mrn
            orderby error.Timestamp
            select error
        ).ToListWithFallbackAsync(cancellationToken);

        return Results.Ok(
            new Declaration(
                mrn,
                versions
                    .Select(v => new RequestVersion(
                        v.EntryVersionNumber,
                        v.Timestamp,
                        FromXml<ClearanceRequest>(v.Xml)
                    ))
                    .ToList(),
                finalisations
                    .Select(f => new FinalisationVersion(
                        f.EntryVersionNumber,
                        f.Timestamp,
                        FromXml<Finalisation>(f.Xml)
                    ))
                    .ToList(),
                decisions.Select(d => Notifications.ParseDecision(d.Timestamp, d.Xml)).ToList(),
                errors.Select(e => Notifications.ParseError(e.Timestamp, e.Xml)).ToList()
            )
        );
    }

    // What was stored is what was sent, serialised from the contract, so it reads straight back.
    private static T FromXml<T>(string xml)
    {
        using var reader = new StringReader(xml);
        return (T)new XmlSerializer(typeof(T)).Deserialize(reader)!;
    }
}
