using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Infrastructure.OneDrive;

/// <summary>
/// Client Microsoft Graph (REST v1.0) per la cartella dell'app nel OneDrive personale di un utente, con il suo
/// token delegato fornito da <see cref="IOneDriveTokenService"/>. Usa solo le poche chiamate necessarie all'import.
/// </summary>
public sealed class GraphOneDriveClient : IOneDriveClient
{
    public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0/";

    private readonly HttpClient _httpClient;
    private readonly IOneDriveTokenService _tokenProvider;

    public GraphOneDriveClient(HttpClient httpClient, IOneDriveTokenService tokenProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _httpClient.BaseAddress ??= new Uri(GraphBaseUrl);
    }

    public async Task<OneDriveAppFolder> GetAppFolderAsync(int userId, CancellationToken cancellationToken = default)
    {
        var folder = await SendForJsonAsync<DriveItemDto>(
            userId, HttpMethod.Get, "me/drive/special/approot?$select=id,name,parentReference", body: null, cancellationToken);

        // parentReference.path: "/drive/root:/Apps" → "Apps/<nome>"
        var parentPath = folder.ParentReference?.Path ?? string.Empty;
        var separator = parentPath.IndexOf(':');
        var relativeParent = separator >= 0 ? Uri.UnescapeDataString(parentPath[(separator + 1)..]).Trim('/') : string.Empty;
        var path = string.IsNullOrEmpty(relativeParent) ? folder.Name ?? string.Empty : $"{relativeParent}/{folder.Name}";

        return new OneDriveAppFolder(folder.Id, path);
    }

    public async Task<string?> GetDriveOwnerAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = await SendForJsonAsync<DriveDto>(userId, HttpMethod.Get, "me/drive?$select=id,owner", body: null, cancellationToken);
            return drive.Owner?.User?.DisplayName;
        }
        catch (HttpRequestException)
        {
            // Solo informativo: con il permesso limitato alla cartella dell'app Graph può negare i dati del drive.
            return null;
        }
    }

    public async Task<IReadOnlyList<OneDriveItem>> GetChildrenAsync(int userId, string folderId, CancellationToken cancellationToken = default)
    {
        var items = new List<OneDriveItem>();
        string? url = $"me/drive/items/{Uri.EscapeDataString(folderId)}/children?$select=id,name,folder,file,parentReference&$top=200";

        while (url is not null)
        {
            var page = await SendForJsonAsync<DriveItemPage>(userId, HttpMethod.Get, url, body: null, cancellationToken);
            items.AddRange(page.Value.Select(ToModel));
            url = page.NextLink;
        }

        return items;
    }

    public async Task<Stream> DownloadAsync(int userId, string itemId, CancellationToken cancellationToken = default)
    {
        // /content risponde 302 verso un URL pre-autenticato: HttpClient segue il redirect e non inoltra l'header Authorization.
        using var request = await CreateRequestAsync(userId, HttpMethod.Get, $"me/drive/items/{Uri.EscapeDataString(itemId)}/content", null, cancellationToken);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public async Task<string> EnsureChildFolderAsync(int userId, string parentFolderId, string name, CancellationToken cancellationToken = default)
    {
        var existing = await TryGetChildAsync(userId, parentFolderId, name, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["folder"] = new { },
            ["@microsoft.graph.conflictBehavior"] = "fail",
        };

        using var request = await CreateRequestAsync(userId, HttpMethod.Post, $"me/drive/items/{Uri.EscapeDataString(parentFolderId)}/children", body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Creata nel frattempo da un'altra esecuzione.
            return (await TryGetChildAsync(userId, parentFolderId, name, cancellationToken))?.Id
                ?? throw new InvalidOperationException($"OneDrive folder '{name}' reported as existing but not found.");
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<DriveItemDto>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response creating a OneDrive folder.");
        return created.Id;
    }

    public async Task MoveAsync(int userId, string itemId, string targetFolderId, CancellationToken cancellationToken = default)
    {
        var body = new { parentReference = new { id = targetFolderId } };
        using var request = await CreateRequestAsync(userId, HttpMethod.Patch,
            $"me/drive/items/{Uri.EscapeDataString(itemId)}?@microsoft.graph.conflictBehavior=rename",
            body,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<OneDriveSubscription> CreateSubscriptionAsync(
        int userId, string resource, string notificationUrl, string clientState, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            changeType = "updated",
            notificationUrl,
            resource,
            expirationDateTime = expiresAt.UtcDateTime.ToString("o"),
            clientState,
        };

        var created = await SendForJsonAsync<SubscriptionDto>(userId, HttpMethod.Post, "subscriptions", body, cancellationToken);
        return new OneDriveSubscription(created.Id, created.ExpirationDateTime);
    }

    public async Task<OneDriveSubscription?> RenewSubscriptionAsync(int userId, string subscriptionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var body = new { expirationDateTime = expiresAt.UtcDateTime.ToString("o") };
        using var request = await CreateRequestAsync(userId, HttpMethod.Patch, $"subscriptions/{Uri.EscapeDataString(subscriptionId)}", body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        var renewed = await response.Content.ReadFromJsonAsync<SubscriptionDto>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response renewing a Graph subscription.");
        return new OneDriveSubscription(renewed.Id, renewed.ExpirationDateTime);
    }

    public async Task DeleteSubscriptionAsync(int userId, string subscriptionId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(userId, HttpMethod.Delete, $"subscriptions/{Uri.EscapeDataString(subscriptionId)}", null, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.NotFound)
            await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<DriveItemDto?> TryGetChildAsync(int userId, string parentFolderId, string name, CancellationToken cancellationToken)
    {
        // Indirizzamento per percorso relativo alla cartella: /items/{parent-id}:/{nome}
        using var request = await CreateRequestAsync(userId, HttpMethod.Get,
            $"me/drive/items/{Uri.EscapeDataString(parentFolderId)}:/{Uri.EscapeDataString(name)}",
            null,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DriveItemDto>(cancellationToken);
    }

    private async Task<T> SendForJsonAsync<T>(int userId, HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(userId, method, url, body, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException($"Empty response from Microsoft Graph ({method} {url}).");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(int userId, HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync(userId, cancellationToken));
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
        IsFolder: dto.Folder is not null);

    private sealed record DriveItemPage(
        [property: JsonPropertyName("value")] List<DriveItemDto> Value,
        [property: JsonPropertyName("@odata.nextLink")] string? NextLink);

    private sealed record DriveItemDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("parentReference")] ItemReferenceDto? ParentReference,
        [property: JsonPropertyName("file")] object? File,
        [property: JsonPropertyName("folder")] object? Folder);

    private sealed record ItemReferenceDto(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("path")] string? Path);

    private sealed record DriveDto([property: JsonPropertyName("owner")] IdentitySetDto? Owner);

    private sealed record IdentitySetDto([property: JsonPropertyName("user")] IdentityDto? User);

    private sealed record IdentityDto([property: JsonPropertyName("displayName")] string? DisplayName);

    private sealed record SubscriptionDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("expirationDateTime")] DateTimeOffset ExpirationDateTime);
}
