// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Http;

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Factory for creating configured HTTP clients with standard options.
/// Centralizes HTTP client configuration to ensure consistency and reusability.
/// </summary>
public class HttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = [];
    private readonly IHttpClientFactory? _factorySvc;

    public HttpClientFactory(IHttpClientFactory? factorySvc = null)
    {
        _factorySvc = factorySvc;
    }

    /// <summary>
    /// Gets or creates a named HTTP client with default configuration.
    /// </summary>
    public HttpClient GetClient(string name = "default")
    {
        if (_clients.TryGetValue(name, out var existing))
            return existing;

        var client = _factorySvc?.CreateClient(name) ?? new HttpClient();
        ConfigureClient(client);
        _clients[name] = client;

        return client;
    }

    /// <summary>
    /// Gets a client configured for a specific service.
    /// </summary>
    public HttpClient GetClientForService(string serviceName)
    {
        return GetClient($"service-{serviceName}");
    }

    /// <summary>
    /// Configures a client with standard options.
    /// </summary>
    private static void ConfigureClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "docker-sqlite-backup/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Clears all cached clients.
    /// </summary>
    public void ClearClients()
    {
        foreach (var client in _clients.Values)
            client.Dispose();

        _clients.Clear();
    }
}
