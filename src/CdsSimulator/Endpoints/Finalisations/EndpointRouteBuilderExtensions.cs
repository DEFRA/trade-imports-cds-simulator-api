using BtmsGateway.Contract.CustomsDeclarations;

using CdsSimulator.BtmsClient;

using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Exceptions;
using Defra.TradeImportsCdsSimulator.Utils;
using Defra.TradeImportsCdsSimulator.Utils.CorrelationId;

using MongoDB.Bson;

using System.Text;
using System.Xml.Serialization;

using DbEntities = Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Finalisations;

public static class EndpointRouteBuilderExtensions
{
    public static void MapFinalisationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("ws/CDS/defra/alvsfinalisationrequestinbound/v1", PostFinalisationRequest)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPut("ws/CDS/defra/alvsfinalisationrequestinbound/v1", PutFinalisationRequest)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);
    }

    public static async Task<IResult> PostFinalisationRequest(
        HttpRequest httpRequest,
        IDbContext dbContext,
        IBtmsGatewayClient btmsClient,
        CancellationToken cancellationToken
    )
    {
        Finalisation request;
        try
        {
            request = await Negotiator.NegotiateRequest<Finalisation>(httpRequest, cancellationToken);
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
            return Results.BadRequest("Header.EntryReference must be a valid MRN");
        }

        if (request.Header.EntryVersionNumber == null)
        {
            return Results.BadRequest("Header.EntryVersionNumber must be set");
        }

        var exists = dbContext.FinalisationNotifications.Any(fn =>
            fn.Mrn == request.Header.EntryReference && fn.EntryVersionNumber == request.Header.EntryVersionNumber
        );
        if (exists)
        {
            return Results.Conflict(
                $"Entry {request.Header.EntryReference} with version {request.Header.EntryVersionNumber} already exists"
            );
        }

        var clearanceRequest = await SaveFinalisation(request, dbContext, cancellationToken);

        await btmsClient.PostFinalisationNotificationAsync(request, cancellationToken);

        return Results.Created($"/clearanceRequest/{clearanceRequest.Id}", clearanceRequest);
    }

    public static async Task<IResult> PutFinalisationRequest(
        HttpRequest httpRequest,
        IDbContext dbContext,
        IBtmsGatewayClient btmsClient,
        CancellationToken cancellationToken
    )
    {
        Finalisation request;
        try
        {
            request = await Negotiator.NegotiateRequest<Finalisation>(httpRequest, cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        // PUT (test) has different validation requirements
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

        await SaveFinalisation(request, dbContext, cancellationToken);

        await btmsClient.PostFinalisationNotificationAsync(request, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<DbEntities.Finalisation> SaveFinalisation(
        Finalisation request,
        IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var serializer = new XmlSerializer(typeof(Finalisation));
        var sb = new StringBuilder();
        await using (var sw = new StringWriter(sb))
        {
            serializer.Serialize(sw, request);
        }

        var xml = sb.ToString();

        var entity = new DbEntities.Finalisation
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = DateTime.UtcNow,
            Mrn = request.Header.EntryReference!,
            EntryVersionNumber = (byte)request.Header.EntryVersionNumber!,
            Xml = xml,
        };

        dbContext.FinalisationNotifications.Insert(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
