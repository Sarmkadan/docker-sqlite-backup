#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Zaiets.docker.sqlite.backup.Tests")]

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Sends webhook notifications about backup events with exponential-backoff retry.
/// Supports HMAC-SHA256 payload signing using a shared secret for secure webhook verification.
/// </summary>
/// <remarks>
/// When a webhook secret is configured via <see cref="AppSettings.WebhookSecret"/> or the constructor,
/// all webhook requests include an <c>X-Signature</c> header in the format:
/// <c>t=&lt;timestamp&gt;,v1=&lt;hex-hmac-sha256&gt;</c>
///
/// The signature is computed as:
/// <list type="bullet">
/// <item><description>Timestamp (t): Unix timestamp in seconds when the signature was created</description></item>
/// <item><description>Signature (v1): HMAC-SHA256 of the JSON payload using the shared secret</description></item>
/// </list>
///
/// Verification on the receiving end should:
/// <list type="number">
/// <item><description>Extract the timestamp and signature from the header</description></item>
/// <item><description>Verify the timestamp is recent (e.g., within 5 minutes)</description></item>
/// <item><description>Recompute the HMAC-SHA256 signature using the same secret and payload</description></item>
/// <item><description>Compare the computed signature with the received signature (constant-time comparison recommended)</description></item>
/// </list>
///
/// Example verification pseudocode:
/// <code>
/// string receivedSignature = request.Headers["X-Signature"];
/// var parts = receivedSignature.Split(',');
/// string timestamp = parts[0].Substring(2);
/// string signature = parts[1].Substring(3);
///
/// // Verify timestamp is recent
/// long timestampSeconds = long.Parse(timestamp);
/// if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestampSeconds) > 300) // 5 minutes
/// {
///     throw new UnauthorizedAccessException("Invalid timestamp");
/// }
///
/// // Recompute signature
/// string payload = JsonSerializer.Serialize(payload);
/// byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
/// byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecret);
/// using var hmac = new HMACSHA256(secretBytes);
/// byte[] computedSignatureBytes = hmac.ComputeHash(payloadBytes);
/// string computedSignature = BitConverter.ToString(computedSignatureBytes).Replace("-", "").ToLowerInvariant();
///
/// // Constant-time comparison
/// if (!CryptographicEquals(computedSignature, signature))
/// {
///     throw new UnauthorizedAccessException("Invalid signature");
/// }
/// </code>
/// </remarks>
public class WebhookClient
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "docker-sqlite-backup/2.0" } }
    };

    private readonly ILogger<WebhookClient> _logger;
    private readonly int _maxRetries;
    private readonly string? _webhookSecret;

    public WebhookClient(ILogger<WebhookClient> logger, int maxRetries = 3, string? webhookSecret = null)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _webhookSecret = webhookSecret;
    }

    /// <summary>
    /// Creates a WebhookClient configured with AppSettings.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="settings">Application settings containing webhook secret.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    public WebhookClient(ILogger<WebhookClient> logger, AppSettings settings, int maxRetries = 3)
        : this(logger, maxRetries, settings.WebhookSecret)
    {
    }

    /// <summary>
    /// Creates an HMAC-SHA256 signature header for webhook payloads.
    /// </summary>
    /// <param name="payload">The JSON payload string to sign.</param>
    /// <param name="secret">The secret key to use for signing. If null, uses the secret provided via constructor.</param>
    /// <returns>The signature header value in format: "t=<timestamp>,v1=<hex-hmac-sha256>"</returns>
    public string CreateSignatureHeader(string payload, string? secret = null)
    {
        if (string.IsNullOrEmpty(_webhookSecret) && string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("No webhook secret configured, skipping signature header");
            return string.Empty;
        }

        var signingSecret = secret ?? _webhookSecret;
        if (string.IsNullOrEmpty(signingSecret))
        {
            _logger.LogWarning("No webhook secret provided for signature, skipping signature header");
            return string.Empty;
        }

        // Generate timestamp (seconds since Unix epoch)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Create HMAC-SHA256 signature
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);

        using var hmac = new HMACSHA256(secretBytes);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var signatureHex = BitConverter.ToString(signatureBytes).Replace("-", "").ToLowerInvariant();

        // Format: t=<timestamp>,v1=<hex-hmac-sha256>
        return $"t={timestamp},v1={signatureHex}";
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

            // Add HMAC-SHA256 signature header if secret is configured
            if (!string.IsNullOrEmpty(_webhookSecret))
            {
                var signatureHeader = CreateSignatureHeader(json);
                if (!string.IsNullOrEmpty(signatureHeader))
                {
                    content.Headers.Add("X-Signature", signatureHeader);
                }
            }

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
