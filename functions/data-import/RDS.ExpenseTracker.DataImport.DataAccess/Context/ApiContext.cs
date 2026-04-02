
using System.Text.Json;
using System.Text;
using RDS.ExpenseTracker.DataImport.DataAccess.Context.Abstractions;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.DataImport.DataAccess.Settings;
using System.Net.Http.Headers;

namespace RDS.ExpenseTracker.DataImport.DataAccess.Context
{
    public class ApiContext : IApiContext
    {
        private readonly HttpClient _httpClient;
        private readonly ApiContextSettings _settings;

        public ApiContext(HttpClient client, IOptions<ApiContextSettings> settings)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
            SetupClient().Wait();
        }

        private async Task SetupClient()
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);

            var token = await TokenHelper.GetAccessTokenAsync(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret,
                _settings.Scope);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetAsync<T>(string uri)
        {
            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var dtos = JsonSerializer.Deserialize<T>(json);
            return dtos ?? throw new JsonException($"Failed to deserialize response to {typeof(T).Name}");
        }

        public async Task PostAsync<TRequest>(string uri, TRequest data)
        {
            var json = JsonSerializer.Serialize(data);
            var response = await _httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest data)
        {
            var json = JsonSerializer.Serialize(data);
            var response = await _httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(result)
                   ?? throw new JsonException($"Failed to deserialize response to {typeof(TResponse).Name}");
        }

        public async Task PutAsync<T>(string uri, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var response = await _httpClient.PutAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string uri)
        {
            var response = await _httpClient.DeleteAsync(uri);
            response.EnsureSuccessStatusCode();
        }
    }
}