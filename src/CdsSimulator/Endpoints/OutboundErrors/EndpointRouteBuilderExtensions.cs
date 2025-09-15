using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Defra.TradeImportsCdsSimulator.Endpoints.OutboundErrors;

public static class EndpointRouteBuilderExtensions
{
    public static void MapErrorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("alvsclearance/errornotification/v1", PutError).RequireAuthorization(PolicyNames.Write);
    }

    [HttpPost]
    private static async Task<IResult> PutError(
        HttpContext context,
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var incoming = await reader.ReadToEndAsync(cancellationToken);

        dbContext.ErrorNotifications.Insert(
            new ErrorNotification
            {
                Mrn = incoming.GetMrn(),
                Timestamp = DateTime.UtcNow,
                Id = ObjectId.GenerateNewId().ToString(),
                Xml = incoming.ToHtmlDecodedXml(),
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }
}
