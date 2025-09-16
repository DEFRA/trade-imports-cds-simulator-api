using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Decisions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("ws/CDS/defra/alvsclearanceinbound/v1", PutDecision).RequireAuthorization(PolicyNames.Write);
        app.MapGet("decision-notifications", GetDecisions).RequireAuthorization(PolicyNames.Read);
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
        var incoming = await reader.ReadToEndAsync(cancellationToken);

        string mrn;
        if (incoming.IsErrorNotification())
        {
            mrn = incoming.GetErrorMrn();
        }
        else if (incoming.IsDecisionNotification())
        {
            mrn = incoming.GetDecisionMrn();
        }
        else
        {
            return Results.Problem("Unexpected XML", statusCode: 400);
        }

        dbContext.DecisionNotifications.Insert(
            new Notification()
            {
                Mrn = mrn,
                Timestamp = DateTime.UtcNow,
                Id = ObjectId.GenerateNewId().ToString(),
                Xml = incoming.ToHtmlDecodedXml(),
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }

    [HttpGet]
    private static async Task<IResult> GetDecisions(
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

        var query = from decision in dbContext.DecisionNotifications select decision;
        query = query.ApplyNotificationQuery(request);

        return Results.Ok(await query.ToListWithFallbackAsync(cancellationToken: cancellationToken));
    }
}
