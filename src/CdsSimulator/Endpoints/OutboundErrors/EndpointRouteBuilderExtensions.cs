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
        app.MapGet("error-notifications", GetErrors).RequireAuthorization(PolicyNames.Read);
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
