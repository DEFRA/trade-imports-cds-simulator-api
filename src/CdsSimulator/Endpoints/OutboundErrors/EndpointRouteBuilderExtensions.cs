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

    [HttpGet]
    private static async Task<IResult> GetErrors(
        [AsParameters] GetErrorsQuery request,
        [FromServices] IValidator<GetErrorsQuery> validator,
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

        return Results.Ok(await query.ToListWithFallbackAsync(cancellationToken: cancellationToken));
    }
}
