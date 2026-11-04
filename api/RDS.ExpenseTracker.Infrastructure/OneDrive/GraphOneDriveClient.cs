using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Infrastructure.OneDrive;

/// <summary>
/// Client Microsoft Graph (REST v1.0) per il OneDrive personale dell'utente, con token delegato
/// fornito da <see cref="IOneDriveTokenProvider"/>. Usa solo le poche chiamate necessarie all'import.
/// </summary>
public sealed class GraphOneDriveClient : IOneDriveClient
{
    public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0/";

    private readonly HttpClient _httpClient;
    private readonly IOneDriveTokenProvider _tokenProvider;

    public GraphOneDriveClient(HttpClient httpClient, IOneDriveTokenProvider tokenProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _httpClient.BaseAddress ??= new Uri(GraphBaseUrl);
    }

    public async Task<IReadOnlyList<OneDriveItem>> GetChildFoldersAsync(string folderId, CancellationToken cancellationToken = default)
    {
        var folders = new List<OneDriveItem>();
        string? url = $"me/drive/items/{Uri.EscapeDataString(folderId)}/children?$select=id,name,folder,file,parentReference&$top=200";

        while (url is not null)
        {
            var page = await SendForJsonAsync<DriveItemPage>(HttpMethod.Get, url, body: null, cancellationToken);
            folders.AddRange(page.Value.Select(ToModel).Where(i => i.IsFolder));
            url = page.NextLink;
        }

        return folders;
    }

    public async Task<OneDriveDeltaResult> GetDeltaAsync(string folderId, string? deltaLink, CancellationToken cancellationToken = default)
    {
        var items = new List<OneDriveItem>();
        string? url = deltaLink ?? $"me/drive/items/{Uri.EscapeDataString(folderId)}/delta?token=latest";

        while (true)
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, url!, body: null, cancellationToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Gone)
                throw new OneDriveDeltaExpiredException("The OneDrive delta link has expired (410 Gone): a resync is required.");

            await EnsureSuccessAsync(response, cancellationToken);
            var page = await response.Content.ReadFromJsonAsync<DriveItemPage>(cancellationToken)
                ?? throw new InvalidOperationException("Empty delta response from Microsoft Graph.");

            items.AddRange(page.Value.Select(ToModel));

            if (page.NextLink is not null)
            {
                url = page.NextLink;
                continue;
            }

            return new OneDriveDeltaResult(items,
                page.DeltaLink ?? throw new InvalidOperationException("Delta response without @odata.deltaLink."));
        }
    }

    public async Task<Stream> DownloadAsync(string itemId, CancellationToken cancellationToken = default)
    {
        // /content risponde 302 verso un URL pre-autenticato: HttpClient segue il redirect e non inoltra l'header Authorization.
        using var request = await CreateRequestAsync(HttpMethod.Get, $"me/drive/items/{Uri.EscapeDataString(itemId)}/content", null, cancellationToken);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public async Task<string> EnsureChildFolderAsync(string parentFolderId, string name, CancellationToken cancellationToken = default)
    {
        var existing = await TryGetChildAsync(parentFolderId, name, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["folder"] = new { },
            ["@microsoft.graph.conflictBehavior"] = "fail",
        };

        using var request = await CreateRequestAsync(HttpMethod.Post, $"me/drive/items/{Uri.EscapeDataString(parentFolderId)}/children", body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Creata nel frattempo da un'altra esecuzione.
            return (await TryGetChildAsync(parentFolderId, name, cancellationToken))?.Id
                ?? throw new InvalidOperationException($"OneDrive folder '{name}' reported as existing but not found.");
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<DriveItemDto>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response creating a OneDrive folder.");
        return created.Id;
    }

    public async Task MoveAsync(string itemId, string targetFolderId, CancellationToken cancellationToken = default)
    {
        var body = new { parentReference = new { id = targetFolderId } };
        using var request = await CreateRequestAsync(
            HttpMethod.Patch,
            $"me/drive/items/{Uri.EscapeDataString(itemId)}?@microsoft.graph.conflictBehavior=rename",
            body,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<OneDriveSubscription> CreateSubscriptionAsync(
        string resource, string notificationUrl, string clientState, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            changeType = "updated",
            notificationUrl,
            resource,
            expirationDateTime = expiresAt.UtcDateTime.ToString("o"),
            clientState,
        };

        var created = await SendForJsonAsync<SubscriptionDto>(HttpMethod.Post, "subscriptions", body, cancellationToken);
        return new OneDriveSubscription(created.Id, created.ExpirationDateTime);
    }

    public async Task<OneDriveSubscription?> RenewSubscriptionAsync(string subscriptionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var body = new { expirationDateTime = expiresAt.UtcDateTime.ToString("o") };
        using var request = await CreateRequestAsync(HttpMethod.Patch, $"subscriptions/{Uri.EscapeDataString(subscriptionId)}", body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        var renewed = await response.Content.ReadFromJsonAsync<SubscriptionDto>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response renewing a Graph subscription.");
        return new OneDriveSubscription(renewed.Id, renewed.ExpirationDateTime);
    }

    private async Task<DriveItemDto?> TryGetChildAsync(string parentFolderId, string name, CancellationToken cancellationToken)
    {
        // Indirizzamento per percorso relativo alla cartella: /items/{parent-id}:/{nome}
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"me/drive/items/{Uri.EscapeDataString(parentFolderId)}:/{Uri.EscapeDataString(name)}",
            null,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DriveItemDto>(cancellationToken);
    }

    private async Task<T> SendForJsonAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(method, url, body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException($"Empty response from Microsoft Graph ({method} {url}).");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync(cancellationToken));
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Microsoft Graph request {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {(int)response.StatusCode}: {content}",
            inner: null,
            statusCode: response.StatusCode);
    }

    private static OneDriveItem ToModel(DriveItemDto dto) => new(
        dto.Id,
        dto.Name ?? string.Empty,
        dto.ParentReference?.Id,
        IsFile: dto.File is not null,
        IsFolder: dto.Folder is not null,
        IsDeleted: dto.Deleted is not null);

    private sealed record DriveItemPage(
        [property: JsonPropertyName("value")] List<DriveItemDto> Value,
        [property: JsonPropertyName("@odata.nextLink")] string? NextLink,
        [property: JsonPropertyName("@odata.deltaLink")] string? DeltaLink);

    private sealed record DriveItemDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("parentReference")] ItemReferenceDto? ParentReference,
        [property: JsonPropertyName("file")] object? File,
        [property: JsonPropertyName("folder")] object? Folder,
        [property: JsonPropertyName("deleted")] object? Deleted);

    private sealed record ItemReferenceDto([property: JsonPropertyName("id")] string? Id);

    private sealed record SubscriptionDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("expirationDateTime")] DateTimeOffset ExpirationDateTime);
}
