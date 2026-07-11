#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using System.Text;
using System.Text.Json;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Sends webhook notifications about backup events with exponential-backoff retry.
/// </summary>
public class WebhookClient
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "docker-sqlite-backup/2.0" } }
    };

    private readonly ILogger<WebhookClient> _logger;
    private readonly int _maxRetries;

    public WebhookClient(ILogger<WebhookClient> logger, int maxRetries = 3)
    {
        _logger = logger;
        _maxRetries = maxRetries;
    }

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

    private async Task SendWithRetryAsync(
        string url,
        object payload,
        CancellationToken cancellationToken,
        int attempt = 0)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to {Url}", url);
            }
            else if ((response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                      response.StatusCode == System.Net.HttpStatusCode.RequestTimeout) &&
                     attempt < _maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                _logger.LogWarning("Webhook failed with {Status}, retrying in {Delay}ms",
                    response.StatusCode, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
                await SendWithRetryAsync(url, payload, cancellationToken, attempt + 1);
            }
            else
            {
                _logger.LogError("Webhook request failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown or caller-initiated cancellation must not be swallowed.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", url);
        }
    }
}
