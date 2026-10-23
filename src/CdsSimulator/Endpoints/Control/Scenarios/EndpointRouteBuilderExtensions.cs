using System.Text.Json;
using System.Text.RegularExpressions;
using Defra.TradeImportsCdsSimulator.Authentication;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Control.Scenarios;

// Saved starting points for clearance requests, for the simulator's UI: it lists them to start a
// declaration from, and saves its form as one.
public static partial class EndpointRouteBuilderExtensions
{
    public record ScenarioBody(string? Name, JsonElement Model);

    public record ScenarioResponse(string Id, string Name, JsonElement Model);

    // An ID is a slug of the scenario's name, e.g. "bison-with-n853".
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Slug();

    public static void MapScenarioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("scenarios", GetScenarios).RequireAuthorization(PolicyNames.Read).Produces<List<ScenarioResponse>>();

        app.MapPut("scenarios/{id}", PutScenario)
            .RequireAuthorization(PolicyNames.Write)
            .Produces<ScenarioResponse>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    public static async Task<IResult> GetScenarios(
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var scenarios = await (
            from scenario in dbContext.Scenarios
            orderby scenario.Name
            select scenario
        ).ToListWithFallbackAsync(cancellationToken);

        return Results.Ok(scenarios.Select(Response).ToList());
    }

    // Saving under an ID that's taken replaces that scenario.
    public static async Task<IResult> PutScenario(
        string id,
        [FromBody] ScenarioBody body,
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (!Slug().IsMatch(id))
        {
            return Results.BadRequest("The id must be lower-case letters and digits, joined by single dashes");
        }

        if (string.IsNullOrWhiteSpace(body.Name) || body.Model.ValueKind != JsonValueKind.Object)
        {
            return Results.BadRequest("A scenario needs a name and a model");
        }

        var scenario = new Scenario
        {
            Id = id,
            Name = body.Name.Trim(),
            Model = body.Model.GetRawText(),
        };
        await dbContext.Scenarios.Upsert(s => s.Id == id, scenario, cancellationToken);

        return Results.Ok(Response(scenario));
    }

    private static ScenarioResponse Response(Scenario scenario) =>
        new(scenario.Id, scenario.Name, JsonSerializer.Deserialize<JsonElement>(scenario.Model));
}
