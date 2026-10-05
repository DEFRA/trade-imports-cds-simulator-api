using System.ComponentModel;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models;

[Serializable]
[DesignerCategory("code")]
[XmlType(TypeName = "ALVSClearanceRequestHeader")]
public class AlvsClearanceRequestHeader
{
    public string? EntryReference { get; set; }

    public byte? EntryVersionNumber { get; set; }

    public byte? PreviousVersionNumber { get; set; }

    public string? DeclarationUCR { get; set; }

    public string? DeclarationType { get; set; }

    public ulong? ArrivalDateTime { get; set; }

    public ulong? SubmitterTURN { get; set; }

    public string? DeclarantId { get; set; }

    public string? DeclarantName { get; set; }

    public string? DispatchCountryCode { get; set; }

    public string? GoodsLocationCode { get; set; }

    public string? MasterUCR { get; set; }
}