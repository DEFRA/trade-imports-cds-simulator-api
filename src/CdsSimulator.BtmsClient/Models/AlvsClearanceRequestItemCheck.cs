using System.ComponentModel;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models;

[Serializable]
[DesignerCategory("code")]
[XmlType(TypeName = "ALVSClearanceRequestItemCheck")]
public class AlvsClearanceRequestItemCheck
{
    public string? CheckCode { get; set; }

    public string? DepartmentCode { get; set; }
}
