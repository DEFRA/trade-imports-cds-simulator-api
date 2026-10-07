using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CdsSimulator.BtmsClient;
using CdsSimulator.BtmsClient.Models;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Utils.Mrn;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using ClearanceRequest = Defra.TradeImportsCdsSimulator.Data.Entities.ClearanceRequest;

namespace Defra.TradeImportsCdsSimulator.Endpoints.ClearanceRequests;

public static class EndpointRouteBuilderExtensions
{
    public static void MapClearanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("ws/CDS/defra/alvsclearancerequestinbound/v1", PostClearanceRequest)
            .Produces(StatusCodes.Status201Created);

        app.MapPut("clearanceRequest", PutClearanceRequest).Produces(StatusCodes.Status204NoContent);
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

        request.ServiceHeader.CorrelationId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // ServiceCallTimestamp must NOT be set by caller
        if (request.ServiceHeader.ServiceCallTimestamp != null)
        {
            return Results.BadRequest("ServiceHeader.ServiceCallTimestamp must not be set");
        }

        request.ServiceHeader.ServiceCallTimestamp = DateTime.UtcNow;

        request.Header ??= new AlvsClearanceRequestHeader();

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
        var request = await NegotiateRequest<AlvsClearanceRequest>(httpRequest, cancellationToken);

        // PUT (test) has different validation requirements
        if (request.ServiceHeader == null)
        {
            return Results.BadRequest("ServiceHeader must not be null");
        }

        request.ServiceHeader.CorrelationId ??= (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

        if (contentType.Contains("xml"))
        {
            // Read the request body as a string with BOM detection
            using var sr = new StreamReader(httpRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var body = await sr.ReadToEndAsync(cancellationToken);

            // Parse XML and extract the expected element (e.g., ALVSClearanceRequest inside a SOAP body)
            XDocument doc;
            try
            {
                doc = XDocument.Parse(body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Request body is not valid XML.", ex);
            }

            var rootAttr = typeof(T).GetCustomAttribute<XmlRootAttribute>();
            var expectedLocal = rootAttr?.ElementName ?? typeof(T).Name;
            var expectedNs = rootAttr?.Namespace ?? string.Empty;

            var payload = doc.Descendants()
                .FirstOrDefault(e =>
                    e.Name.LocalName == expectedLocal
                    && (string.IsNullOrEmpty(expectedNs) || e.Name.NamespaceName == expectedNs)
                );

            if (payload is null)
            {
                throw new InvalidOperationException($"SOAP body does not contain expected '{expectedLocal}' element.");
            }

            // Mark empty leaf elements as xsi:nil="true" so XmlSerializer treats them as null
            var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            // ensure xsi prefix is declared on the payload
            if (payload.GetNamespaceOfPrefix("xsi") == null)
            {
                payload.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi.NamespaceName);
            }

            var emptyLeaves = payload
                .DescendantsAndSelf()
                .Where(e => !e.HasElements && string.IsNullOrWhiteSpace(e.Value))
                .ToList();
            foreach (var el in emptyLeaves)
            {
                el.SetAttributeValue(xsi + "nil", "true");
                el.RemoveNodes(); // remove empty text content
            }

            var xml = payload.ToString(SaveOptions.DisableFormatting);

            var serializer = new XmlSerializer(typeof(T));
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            if (serializer.Deserialize(reader) is not T request)
                throw new InvalidOperationException($"Unable to deserialize XML to {typeof(T).FullName}");

            return request;
        }

        // support JSON of the raw object
        var jsonOptions = httpRequest
            .HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>()
            .Value.SerializerOptions;

        var result = await httpRequest.ReadFromJsonAsync<T>(jsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException($"Unable to deserialize JSON to {typeof(T).FullName}");
    }

    public static async Task<ClearanceRequest> SaveClearanceRequest(
        AlvsClearanceRequest request,
        IDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
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
            Mrn = request.Header!.EntryReference!,
            EntryVersionNumber = (byte)request.Header!.EntryVersionNumber!,
            Xml = xml,
        };

        dbContext.ClearanceRequests.Insert(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
