using System.Xml;
using System.Xml.Linq;

namespace Defra.TradeImportsCdsSimulator.Extensions;

public static class ElementExtensions
{
    public static string GetMrn(this string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml.ToHtmlDecodedXml()));
        reader.ReadToFollowing(
            ElementNames.DecisionNotification.LocalName,
            ElementNames.DecisionNotification.NamespaceName
        );

        if (reader.NodeType != XmlNodeType.Element)
            return string.Empty;

        var element = XElement.Load(reader.ReadSubtree());
        var decisionNumberElement = element
            .Descendants(ElementNames.Header)
            .Descendants(ElementNames.EntryReference)
            .FirstOrDefault();

        return decisionNumberElement?.Value!;
    }
}
