using System.ComponentModel;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models;

[Serializable]
[DesignerCategory("code")]
[XmlType(TypeName = "ALVSClearanceRequestServiceHeader")]
public class AlvsClearanceRequestServiceHeader
{
    public string? SourceSystem { get; set; }

    public string? DestinationSystem { get; set; }

    public ulong? CorrelationId { get; set; }

    public DateTime? ServiceCallTimestamp { get; set; }
}
