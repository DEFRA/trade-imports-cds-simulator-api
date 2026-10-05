using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsCdsSimulator.Endpoints.ClearanceRequests;

public static class EndpointRouteBuilderExtensions
{
    public static void MapClearanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("clearanceRequest", PutClearanceRequest);
    }

    [HttpPost]
    private static async Task<IResult> PutClearanceRequest(
        HttpContext context,
        [FromServices] IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        // accept in a clearance request - this will be an AlvsCleareanceRequest item - with rules defined as follows:
        // ServiceHeader.CorrelationId - is set to a new Guid
        // ServiceHeader.ServiceCallTimestamp - is set to the current UTC date
        // Header.EntryReference – generated MRN
        // if Header.EntryReference is not defined:
        //    Header.EntryReference is generated as a syntatically correct Movement reference Number in the format : {YY}GB{XXXXXXXXXXXXXX} Where : {YY} is the last two digits of the current year (currently 26), {XXXXXXXXXXXXXX} is a random alphanumeric code 
        //    Header.EntryVersionNumber is set to 1
        // if Header.EntityReference is defined:
        //    If the Header.EntryVersionNumber for that version already exists in the db collection an HTTP 409 conflict is returned
        // 
        // if Header.EntryVersionNumber – is defined it must always be greater than Header.PreviousVersionNumber

        // if the request is valid, add the record to the mongo db collection
        // call the BTMS gateway client  

        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var incoming = await reader.ReadToEndAsync(cancellationToken);

        if (incoming.IsErrorNotification())
        {
            // save the request to the db
            // and post it to the configured topic
        }
        else
        {
            return Results.Problem("Unexpected XML", statusCode: 400);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // return the clearance request
    }
}
