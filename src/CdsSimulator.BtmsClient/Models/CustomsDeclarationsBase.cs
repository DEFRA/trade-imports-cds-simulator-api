using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models
{
    public record CustomsDeclarationsBase
    {
        [XmlElement(ElementName = "ServiceHeader")]
        [JsonPropertyName("serviceHeader")]
        public required ServiceHeader ServiceHeader { get; init; }
    }
}
