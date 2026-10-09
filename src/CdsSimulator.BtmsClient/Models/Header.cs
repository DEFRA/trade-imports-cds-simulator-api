using System.Text.Json.Serialization;

namespace CdsSimulator.BtmsClient.Models;

public class Header
{
    [JsonPropertyName("entryReference")]
    public string? EntryReference { get; set; }

    [JsonPropertyName("entryVersionNumber")]
    public int? EntryVersionNumber { get; set; }

    [JsonPropertyName("previousVersionNumber")]
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
