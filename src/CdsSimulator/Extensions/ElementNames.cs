using System.Xml.Linq;

namespace Defra.TradeImportsCdsSimulator.Extensions;

public static class ElementNames
{
    public static class Error
    {
        public static readonly string Namespace = "http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification";

        public static readonly XName Header = XName.Get(nameof(Header), Namespace);
        public static readonly XName EntryReference = XName.Get(nameof(EntryReference), Namespace);
    }

    public static class Decision
    {
        public static readonly string Namespace = "http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification";

        public static readonly XName DecisionNotification = XName.Get(nameof(DecisionNotification), Namespace);
        public static readonly XName Header = XName.Get(nameof(Header), Namespace);
        public static readonly XName EntryReference = XName.Get(nameof(EntryReference), Namespace);
    }
}
