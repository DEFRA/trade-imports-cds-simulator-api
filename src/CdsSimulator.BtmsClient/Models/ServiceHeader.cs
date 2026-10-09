using System.Text.Json.Serialization;

namespace CdsSimulator.BtmsClient.Models;

public class ServiceHeader
{
    [JsonPropertyName("sourceSystem")]
    public required string SourceSystem { get; init; }

    [JsonPropertyName("destinationSystem")]
    public string? DestinationSystem { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("serviceCallTimestamp")]
    public DateTime? ServiceCallTimestamp { get; set; }
}
