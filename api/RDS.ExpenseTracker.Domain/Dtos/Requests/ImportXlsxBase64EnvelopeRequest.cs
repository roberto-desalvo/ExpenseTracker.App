using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class ImportXlsxBase64EnvelopeRequest
{
    [JsonPropertyName("$content-type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("$content")]
    public string Content { get; set; } = string.Empty;
}
