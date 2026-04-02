using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Api.Dtos
{
    public class CategoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("tags")]
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
    }
}
