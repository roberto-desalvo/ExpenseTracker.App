using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Api.Dtos
{
    public class FinancialAccountDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("availability")]
        public decimal? Availability { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
