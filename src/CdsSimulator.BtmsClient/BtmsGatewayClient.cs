using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CdsSimulator.BtmsClient.Models;

namespace CdsSimulator.BtmsClient;

public class BtmsGatewayClient(HttpClient httpClient, BtmsClientOptions btmsClientOptions) : IBtmsGatewayClient
{
    public const string DefaultRoutePath = "/ITSW/CDS/SubmitImportDocumentCDSFacadeService";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<HttpResponseMessage> PostClearanceRequestAsync(
        ClearanceRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var routePath = GetRoutePath(btmsClientOptions.Routes, "AlvsClearanceRequest");
        var target = CombineUrl(btmsClientOptions.GatewayBaseUrl, routePath);

        var soap = BuildSoapEnvelope(request, btmsClientOptions.UsernameToken, btmsClientOptions.Password);

        using var content = new StringContent(soap, Encoding.UTF8, "application/soap+xml");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml") { CharSet = "utf-8" };

        var response = await _httpClient.PostAsync(target, content, cancellationToken).ConfigureAwait(false);

        return response;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(path))
            return baseUrl;
        var baseUri = new Uri(baseUrl, UriKind.Absolute);
        var target = new Uri(baseUri, path);
        return target.ToString();
    }

    private static string GetRoutePath(Dictionary<string, BtmsClientRoute> routes, string routeName)
    {
        if (routes.TryGetValue(routeName, out var route))
        {
            return route.Path;
        }

        var matchedRoute = routes.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, routeName, StringComparison.OrdinalIgnoreCase)
        );
        if (!string.IsNullOrWhiteSpace(matchedRoute.Key))
        {
            return matchedRoute.Value.Path;
        }

        throw new KeyNotFoundException($"Route '{routeName}' was not configured under BtmsClient:Routes.");
    }

    private static string BuildSoapEnvelope(ClearanceRequest request, string? usernameToken, string? password)
    {
        var bodyXml = SerializeObjectToXml(request);

        var bodyElement = XElement.Parse(bodyXml);

        var soapNs = "http://www.w3.org/2003/05/soap-envelope";
        var oasNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        var envelope = new XElement(
            XName.Get("Envelope", soapNs),
            new XAttribute(XNamespace.Xmlns + "soap", soapNs),
            // Header
            new XElement(
                XName.Get("Header", soapNs),
                string.IsNullOrWhiteSpace(usernameToken) && string.IsNullOrWhiteSpace(password)
                    ? null
                    : new XElement(
                        XName.Get("Security", oasNs),
                        new XAttribute(XNamespace.Xmlns + "oas", oasNs),
                        new XAttribute(XName.Get("role", soapNs), "system"),
                        new XAttribute(XName.Get("mustUnderstand", soapNs), "true"),
                        new XElement(
                            XName.Get("UsernameToken", oasNs),
                            new XElement(XName.Get("Username", oasNs), usernameToken ?? string.Empty),
                            new XElement(
                                XName.Get("Password", oasNs),
                                password ?? string.Empty,
                                new XAttribute(
                                    "Type",
                                    "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"
                                )
                            )
                        )
                    )
            ),
            // Body
            new XElement(XName.Get("Body", soapNs), bodyElement)
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), envelope);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static string SerializeObjectToXml<T>(T obj)
    {
        var ns = new XmlSerializerNamespaces();
        // Ensure default namespace matches model's XmlRoot Namespace
        ns.Add(string.Empty, "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com");

        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Encoding = Encoding.UTF8,
            Indent = false,
        };

        using var sw = new StringWriter();
        using var xw = XmlWriter.Create(sw, settings);
        serializer.Serialize(xw, obj, ns);
        return sw.ToString();
    }
}
