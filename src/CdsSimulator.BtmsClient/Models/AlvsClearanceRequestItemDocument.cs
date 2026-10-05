using System.ComponentModel;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models;

[Serializable]
[DesignerCategory("code")]
[XmlType(TypeName = "ALVSClearanceRequestItemDocument")]
public class AlvsClearanceRequestItemDocument
{
    public string? DocumentCode { get; set; }

    public string? DocumentReference { get; set; }

    public string? DocumentStatus { get; set; }

    public string? DocumentControl { get; set; }
}