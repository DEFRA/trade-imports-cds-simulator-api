using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Decisions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("alvsclearance/decisionnotification/v1", PutDecision).RequireAuthorization(PolicyNames.Write);
    }

    [HttpPost]
    private static async Task<IResult> PutDecision(
        HttpContext context,
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var incomingDecision = await reader.ReadToEndAsync(cancellationToken);

        dbContext.DecisionNotifications.Insert(
            new DecisionNotification()
            {
                Mrn = incomingDecision.GetMrn(),
                Timestamp = DateTime.UtcNow,
                Id = ObjectId.GenerateNewId().ToString(),
                Xml = incomingDecision.ToHtmlDecodedXml(),
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }
}
