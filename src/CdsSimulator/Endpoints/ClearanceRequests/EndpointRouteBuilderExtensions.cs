using System.Text;
using System.Xml.Serialization;
using Azure.Core;
using CdsSimulator.BtmsClient;
using CdsSimulator.BtmsClient.Models;
using Defra.TradeImportsCdsSimulator.Data;
using MongoDB.Bson;
using ClearanceRequest = Defra.TradeImportsCdsSimulator.Data.Entities.ClearanceRequest;

namespace Defra.TradeImportsCdsSimulator.Endpoints.ClearanceRequests;

public static class EndpointRouteBuilderExtensions
{
    public static void MapClearanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("clearanceRequest", PostClearanceRequest)
            .Accepts<AlvsClearanceRequest>("application/json", "application/xml")
            .Produces(StatusCodes.Status201Created);

        app.MapPut("clearanceRequest", PutClearanceRequest)
            .Accepts<AlvsClearanceRequest>("application/json", "application/xml")
            .Produces(StatusCodes.Status204NoContent);
    }

    public static async Task<IResult> PostClearanceRequest(
        HttpRequest httpRequest,
        IDbContext dbContext,
        IBtmsGatewayClient btmsClient,
        CancellationToken cancellationToken
    )
    {
        var request = await NegotiateRequest<AlvsClearanceRequest>(httpRequest, cancellationToken);

        // POST-specific validation / defaults
        request.ServiceHeader ??= new AlvsClearanceRequestServiceHeader();

        // CorrelationId must NOT be set by caller
        if (request.ServiceHeader.CorrelationId != null)
        {
            return Results.BadRequest("ServiceHeader.CorrelationId must not be set");
        }

        request.ServiceHeader.CorrelationId = (ulong)DateTime.UtcNow.Ticks;

        // ServiceCallTimestamp must NOT be set by caller
        if (request.ServiceHeader.ServiceCallTimestamp != null)
        {
            return Results.BadRequest("ServiceHeader.ServiceCallTimestamp must not be set");
        }

        request.ServiceHeader.ServiceCallTimestamp = DateTime.UtcNow;

        request.Header ??= new AlvsClearanceRequestHeader();

        if (string.IsNullOrWhiteSpace(request.Header.EntryReference))
        {
            request.Header.EntryReference = GenerateMrn();
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
        var request = await NegotiateRequest<AlvsClearanceRequest>(httpRequest, cancellationToken);

        // PUT (test) has different validation requirements
        if (request.ServiceHeader == null)
        {
            return Results.BadRequest("ServiceHeader must not be null");
        }

        request.ServiceHeader.CorrelationId ??= (ulong)DateTime.UtcNow.Ticks;
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

    private static async Task<T> NegotiateRequest<T>(HttpRequest httpRequest, CancellationToken cancellationToken)
        where T : class
    {
        var contentType = httpRequest.ContentType?.Split(';')[0].Trim().ToLowerInvariant() ?? string.Empty;

        // If XML, use XmlSerializer
        if (contentType.Contains("xml"))
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));

                // Read the request body as a string first to avoid encoding mismatches
                // (e.g. xml declaration may state utf-16 while the HTTP body is encoded as UTF-8).
                using var sr = new StreamReader(
                    httpRequest.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true
                );
                var body = await sr.ReadToEndAsync();

                using var stringReader = new StringReader(body);
                if (serializer.Deserialize(stringReader) is not T request)
                    throw new InvalidOperationException($"Unable to deserialise XML to type {typeof(T).FullName}");
                return request;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"XML deserialization failed: {ex.Message}", ex);
            }
        }

        // Default to JSON
        try
        {
            var jsonOptions = httpRequest
                .HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
                .Value.SerializerOptions;

            var result = await httpRequest.ReadFromJsonAsync<T>(jsonOptions, cancellationToken);

            return result ?? throw new InvalidOperationException($"Unable to deserialize JSON to {typeof(T).FullName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}", ex);
        }
    }

    public static async Task<ClearanceRequest> SaveClearanceRequest(
        AlvsClearanceRequest request,
        IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (request.Header?.EntryReference == null || request.Header?.EntryVersionNumber == null)
        {
            throw new InvalidOperationException();
        }

        var serializer = new XmlSerializer(typeof(AlvsClearanceRequest));
        var sb = new StringBuilder();
        await using (var sw = new StringWriter(sb))
        {
            serializer.Serialize(sw, request);
        }

        var xml = sb.ToString();

        var entity = new ClearanceRequest
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = DateTime.UtcNow,
            Mrn = request.Header.EntryReference,
            EntryVersionNumber = (byte)request.Header.EntryVersionNumber,
            Xml = xml,
        };

        dbContext.ClearanceRequests.Insert(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    private static readonly char[] s_mrnChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private static string GenerateMrn()
    {
        var yy = (DateTime.UtcNow.Year % 100).ToString("D2");
        var rnd = new Random();
        var sb = new StringBuilder();
        for (var i = 0; i < 14; i++)
            sb.Append(s_mrnChars[rnd.Next(s_mrnChars.Length)]);
        return $"{yy}GB{sb}";
    }
}
