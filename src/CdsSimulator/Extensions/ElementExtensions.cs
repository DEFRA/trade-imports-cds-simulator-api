using System.Xml;
using System.Xml.Linq;

namespace Defra.TradeImportsCdsSimulator.Extensions;

public static class ElementExtensions
{
    public static bool IsErrorNotification(this string xml)
    {
        return xml.Contains(ElementNames.Error.Namespace);
    }

    public static bool IsDecisionNotification(this string xml)
    {
        return xml.Contains(ElementNames.Decision.Namespace);
    }

    public static string? GetElementStringValue(this XElement element, XName name)
    {
        var ele = element.Element(name);

        if (ele is null)
        {
            return null;
        }

        return ele.Value;
    }

    public static string GetDecisionMrn(this string xml)
    {
        return xml.GetMrn(
            ElementNames.Decision.DecisionNotification,
            ElementNames.Decision.Header,
            ElementNames.Decision.EntryReference
        );
    }

    public static string GetErrorMrn(this string xml)
    {
        if (xml == null)
            return string.Empty;

        using var reader = XmlReader.Create(new StringReader(xml.ToHtmlDecodedXml()));

        reader.ReadToFollowing(ElementNames.Error.Header.LocalName, ElementNames.Error.Header.NamespaceName);

        if (reader.NodeType != XmlNodeType.Element)
            return string.Empty;

        var header = XElement.Load(reader.ReadSubtree());

        return header.GetElementStringValue(ElementNames.Error.EntryReference)!;
    }

    private static string GetMrn(this string xml, XName parent, XName header, XName entryReference)
    {
        using var reader = XmlReader.Create(new StringReader(xml.ToHtmlDecodedXml()));
        reader.ReadToFollowing(parent.LocalName, parent.NamespaceName);

        if (reader.NodeType != XmlNodeType.Element)
            return string.Empty;

        var element = XElement.Load(reader.ReadSubtree());
        var decisionNumberElement = element.Descendants(header).Descendants(entryReference).FirstOrDefault();

        return decisionNumberElement?.Value!;
    }
}
