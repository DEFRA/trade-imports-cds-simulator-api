using System.ComponentModel;
using System.Xml.Serialization;

namespace CdsSimulator.BtmsClient.Models;

[Serializable]
[DesignerCategory("code")]
[XmlType(TypeName = "ALVSClearanceRequestItem")]
public class AlvsClearanceRequestItem
{
    public byte? ItemNumber { get; set; }

    public uint? CustomsProcedureCode { get; set; }

    public uint? TaricCommodityCode { get; set; }

    public string? GoodsDescription { get; set; }

    public string? ConsigneeId { get; set; }

    public string? ConsigneeName { get; set; }

    public decimal? ItemNetMass { get; set; }

    public string? ItemOriginCountryCode { get; set; }

    public AlvsClearanceRequestItemDocument? Document { get; set; }

    public AlvsClearanceRequestItemCheck? Check { get; set; }
}
