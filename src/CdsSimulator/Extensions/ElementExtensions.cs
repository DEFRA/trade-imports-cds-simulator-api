using System.Xml;
using System.Xml.Linq;

namespace Defra.TradeImportsCdsSimulator.Extensions;

public static class ElementExtensions
{
    public static string? GetElementStringValue(this XElement element, XName name)
    {
        var ele = element.Element(name);

        if (ele is null)
        {
            return null;
        }

        return ele.Value;
    }

    public static int GetElementIntValue(this XElement element, XName name)
    {
        var ele = element.Element(name);

        if (ele is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return int.Parse(ele.Value);
    }

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

    public static int GetErrorEntryVersionNumber(this string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml.ToHtmlDecodedXml()));
        reader.ReadToFollowing(
            ErrorElementNames.HMRCErrorNotification.LocalName,
            ErrorElementNames.HMRCErrorNotification.NamespaceName
        );

        if (reader.NodeType != XmlNodeType.Element)
            return 0;

        var element = XElement.Load(reader.ReadSubtree());
        var entryVersionNumber = element
            .Descendants(ErrorElementNames.Header)
            .Descendants(ErrorElementNames.EntryVersionNumber)
            .FirstOrDefault();

        if (entryVersionNumber?.Value != null)
            return int.Parse(entryVersionNumber.Value);

        return 0;
    }
}
