using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models
{
    [Serializable]
    [XmlType(TypeName = "ALVSClearanceRequest")]
    [XmlRoot(
        ElementName = "ALVSClearanceRequest",
        Namespace = "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com",
        IsNullable = false
    )]
    public record AlvsClearanceRequest
    {
        [XmlElement(ElementName = "ServiceHeader")]
        public required AlvsClearanceRequestServiceHeader? ServiceHeader { get; set; }

        [XmlElement(ElementName = "Header")]
        public required AlvsClearanceRequestHeader? Header { get; set; }

        [XmlElement("Item")]
        public required AlvsClearanceRequestItem[]? Items { get; set; }
    }
}
