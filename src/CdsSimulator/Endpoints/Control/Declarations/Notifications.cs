using System.Globalization;
using System.Xml.Linq;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Control.Declarations;

public record DecisionCheck(string? CheckCode, string? DecisionCode, string? DecisionReason);

public record DecisionItem(int? ItemNumber, List<DecisionCheck> Checks);

public record Decision(DateTime Timestamp, int? EntryVersionNumber, int? DecisionNumber, List<DecisionItem> Items);

public record ErrorDetail(string? ErrorCode, string? ErrorMessage);

public record ErrorNotification(DateTime Timestamp, int? EntryVersionNumber, List<ErrorDetail> Errors);

// What BTMS sent back, read out of the SOAP the simulator stored. Elements are found by local name:
// BTMS's namespace prefixes (NS1, NS2, ...) aren't fixed.
public static class Notifications
{
    public static Decision ParseDecision(DateTime timestamp, string xml)
    {
        var document = XDocument.Parse(xml);
        var header = Descendant(document, "Header", within: "DecisionNotification");

        return new Decision(
            timestamp,
            Number(Child(header, "EntryVersionNumber")),
            Number(Child(header, "DecisionNumber")),
            Descendants(document, "Item")
                .Select(item => new DecisionItem(
                    Number(Child(item, "ItemNumber")),
                    Children(item, "Check")
                        .Select(check => new DecisionCheck(
                            Child(check, "CheckCode")?.Value,
                            Child(check, "DecisionCode")?.Value,
                            Joined(Children(check, "DecisionReason"))
                        ))
                        .ToList()
                ))
                .ToList()
        );
    }

    public static ErrorNotification ParseError(DateTime timestamp, string xml)
    {
        var document = XDocument.Parse(xml);
        var header = Descendant(document, "Header", within: "HMRCErrorNotification");

        return new ErrorNotification(
            timestamp,
            Number(Child(header, "EntryVersionNumber")),
            Descendants(document, "Error")
                .Select(error => new ErrorDetail(Child(error, "ErrorCode")?.Value, Child(error, "ErrorMessage")?.Value))
                .ToList()
        );
    }

    private static IEnumerable<XElement> Descendants(XContainer container, string name) =>
        container.Descendants().Where(element => element.Name.LocalName == name);

    // The SOAP envelope has a Header of its own, so the notification's is found inside the notification.
    private static XElement? Descendant(XContainer container, string name, string within) =>
        Descendants(container, within).SelectMany(element => Descendants(element, name)).FirstOrDefault();

    private static IEnumerable<XElement> Children(XElement? element, string name) =>
        element?.Elements().Where(child => child.Name.LocalName == name) ?? [];

    private static XElement? Child(XElement? element, string name) => Children(element, name).FirstOrDefault();

    private static int? Number(XElement? element) =>
        int.TryParse(element?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static string? Joined(IEnumerable<XElement> elements)
    {
        var text = string.Join(" ", elements.Select(element => element.Value.Trim()));
        return text.Length == 0 ? null : text;
    }
}
