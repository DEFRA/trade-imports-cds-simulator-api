using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models
{
    [XmlRoot(
        ElementName = "ALVSClearanceRequest",
        Namespace = "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com",
        IsNullable = false
    )]
    public record ClearanceRequest : CustomsDeclarationsBase
    {
        [XmlElement(ElementName = "Header")]
        public required Header? Header { get; set; }

        [XmlElement(ElementName = "Item")]
        public required Item[]? Items { get; set; }
    }
}
