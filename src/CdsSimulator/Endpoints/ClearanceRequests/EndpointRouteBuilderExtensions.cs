using System.Text;
using System.Xml.Serialization;
using BtmsGateway.Contract.CustomsDeclarations;
using CdsSimulator.BtmsClient;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Exceptions;
using Defra.TradeImportsCdsSimulator.Utils;
using Defra.TradeImportsCdsSimulator.Utils.CorrelationId;
using Defra.TradeImportsCdsSimulator.Utils.Mrn;
using MongoDB.Bson;
using DbEntities = Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Endpoints.ClearanceRequests;

public static class EndpointRouteBuilderExtensions
{
    public static void MapClearanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("ws/CDS/defra/alvsclearancerequestinbound/v1", PostClearanceRequest)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPut("ws/CDS/defra/alvsclearancerequestinbound/v1", PutClearanceRequest)
            .Produces(StatusCodes.Status204NoContent);
    }

    public static async Task<IResult> PostClearanceRequest(
        HttpRequest httpRequest,
        IDbContext dbContext,
        IBtmsGatewayClient btmsClient,
        CancellationToken cancellationToken
    )
    {
        ClearanceRequest request;
        try
        {
            request = await Negotiator.NegotiateRequest<ClearanceRequest>(httpRequest, cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        // CorrelationId must NOT be set by caller
        if (request.ServiceHeader.CorrelationId != null)
        {
            return Results.BadRequest("ServiceHeader.CorrelationId must not be set");
        }

        request.ServiceHeader.CorrelationId = CorrelationIdGenerator.Generate();

        // ServiceCallTimestamp must NOT be set by caller
        if (request.ServiceHeader.ServiceCallTimestamp != null)
        {
            return Results.BadRequest("ServiceHeader.ServiceCallTimestamp must not be set");
        }

        request.ServiceHeader.ServiceCallTimestamp = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Header.EntryReference))
        {
            request.Header.EntryReference = MrnGenerator.GenerateMrn();
            request.Header.EntryVersionNumber = 1;
        }
        else if (request.Header.EntryVersionNumber != null)
        {
            if (
                request.Header.PreviousVersionNumber != null
                && request.Header.EntryVersionNumber <= request.Header.PreviousVersionNumber
            )
            {
                return Results.BadRequest(
                    "Header.EntryVersionNumber must be greater than Header.PreviousVersionNumber"
                );
            }

            var exists = dbContext.ClearanceRequests.Any(cr =>
                cr.Mrn == request.Header.EntryReference && cr.EntryVersionNumber == request.Header.EntryVersionNumber
            );
            if (exists)
            {
                return Results.Conflict(
                    $"Entry {request.Header.EntryReference} with version {request.Header.EntryVersionNumber} already exists"
                );
            }
        }

        var clearanceRequest = await SaveClearanceRequest(request, dbContext, cancellationToken);

        await btmsClient.PostClearanceRequestAsync(request, cancellationToken);

        return Results.Created($"/clearanceRequest/{clearanceRequest.Id}", clearanceRequest);
    }

    public static async Task<IResult> PutClearanceRequest(
        HttpRequest httpRequest,
        IDbContext dbContext,
        IBtmsGatewayClient btmsClient,
        CancellationToken cancellationToken
    )
    {
        ClearanceRequest request;
        try
        {
            request = await Negotiator.NegotiateRequest<ClearanceRequest>(httpRequest, cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        request.ServiceHeader.CorrelationId ??= CorrelationIdGenerator.Generate();
        request.ServiceHeader.ServiceCallTimestamp ??= DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Header?.EntryReference))
        {
            return Results.BadRequest("ServiceHeader.EntryReference must be set");
        }

        if (request.Header.EntryVersionNumber == null)
        {
            return Results.BadRequest("ServiceHeader.EntryVersionNumber must be set");
        }

        await SaveClearanceRequest(request, dbContext, cancellationToken);

        await btmsClient.PostClearanceRequestAsync(request, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<DbEntities.ClearanceRequest> SaveClearanceRequest(
        ClearanceRequest request,
        IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var serializer = new XmlSerializer(typeof(ClearanceRequest));
        var sb = new StringBuilder();
        await using (var sw = new StringWriter(sb))
        {
            serializer.Serialize(sw, request);
        }

        var xml = sb.ToString();

        var entity = new DbEntities.ClearanceRequest
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = DateTime.UtcNow,
            Mrn = request.Header.EntryReference!,
            EntryVersionNumber = (byte)request.Header.EntryVersionNumber!,
            Xml = xml,
        };

        dbContext.ClearanceRequests.Insert(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
