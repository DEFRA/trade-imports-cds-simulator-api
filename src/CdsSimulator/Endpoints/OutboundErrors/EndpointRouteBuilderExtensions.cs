using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace Defra.TradeImportsCdsSimulator.Endpoints.OutboundErrors;

public static class EndpointRouteBuilderExtensions
{
    public static void MapErrorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("alvsclearance/errornotification/v1", PutError).RequireAuthorization(PolicyNames.Write);
        app.MapGet("error-notifications", GetErrors).RequireAuthorization(PolicyNames.Read);
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
            new Notification
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

    [HttpGet]
    private static async Task<IResult> GetErrors(
        [AsParameters] GetQuery request,
        [FromServices] IValidator<GetQuery> validator,
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var query = from decision in dbContext.ErrorNotifications select decision;
        query = query.ApplyNotificationQuery(request);

        return Results.Ok(await query.ToListWithFallbackAsync(cancellationToken: cancellationToken));
    }
}
