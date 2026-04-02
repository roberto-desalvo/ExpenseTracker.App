

using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Api.Dtos
{
    public class TransactionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("accountId")]
        public int AccountId { get; set; }

        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }
}
