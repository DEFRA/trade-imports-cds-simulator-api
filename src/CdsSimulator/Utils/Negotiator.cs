using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsCdsSimulator.Utils
{
    internal static class Negotiator
    {
        public static async Task<T> NegotiateRequest<T>(HttpRequest httpRequest, CancellationToken cancellationToken)
            where T : class
        {
            var contentType = httpRequest.ContentType?.Split(';')[0].Trim().ToLowerInvariant() ?? string.Empty;

            if (contentType.Contains("xml"))
            {
                // Read the request body as a string with BOM detection
                using var sr = new StreamReader(
                    httpRequest.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true
                );
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
                    throw new InvalidOperationException(
                        $"SOAP body does not contain expected '{expectedLocal}' element."
                    );
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
    }
}
