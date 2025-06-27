// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Client for sending webhook notifications about backup events.
/// Supports retries and custom headers for webhook validation.
/// </summary>
public class WebhookClient
{
    private readonly HttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookClient> _logger;
    private readonly int _maxRetries;

    public WebhookClient(
        HttpClientFactory httpClientFactory,
        ILogger<WebhookClient> logger,
        int maxRetries = 3)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _maxRetries = maxRetries;
    }

    /// <summary>
    /// Sends a webhook notification about a backup result.
    /// </summary>
    public async Task SendBackupNotificationAsync(
        string webhookUrl,
        BackupResult result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Webhook URL is empty, skipping notification");
            return;
        }

        var payload = new
        {
            eventType = "backup.completed",
            timestamp = DateTime.UtcNow,
            data = new
            {
                result.Id,
                result.ScheduleId,
                result.Status,
                result.BackupFilePath,
                result.BackupFileSizeBytes,
                result.Checksum,
                result.StartedAt,
                result.CompletedAt,
                result.ErrorMessage
            }
        };

        await SendWithRetryAsync(webhookUrl, payload, cancellationToken);
    }

    /// <summary>
    /// Sends a webhook notification about a schedule event.
    /// </summary>
    public async Task SendScheduleNotificationAsync(
        string webhookUrl,
        BackupSchedule schedule,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            eventType,
            timestamp = DateTime.UtcNow,
            data = new
            {
                schedule.Id,
                schedule.Name,
                schedule.DatabasePath,
                schedule.CronExpression,
                schedule.IsEnabled
            }
        };

        await SendWithRetryAsync(webhookUrl, payload, cancellationToken);
    }

    /// <summary>
    /// Sends a webhook with automatic retry logic.
    /// </summary>
    private async Task SendWithRetryAsync(
        string url,
        object payload,
        CancellationToken cancellationToken,
        int attempt = 0)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.GetClient("webhooks");
            var response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to {Url}", url);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                     response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            {
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                    _logger.LogWarning(
                        "Webhook request failed with {StatusCode}, retrying in {Delay}ms",
                        response.StatusCode,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                    await SendWithRetryAsync(url, payload, cancellationToken, attempt + 1);
                }
                else
                {
                    _logger.LogError("Webhook request failed after {Attempts} attempts", _maxRetries);
                }
            }
            else
            {
                _logger.LogError("Webhook request failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", url);
        }
    }
}
