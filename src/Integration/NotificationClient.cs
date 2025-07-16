#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Interface for different notification delivery channels.
/// Abstracts away the details of how notifications are sent.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Sends a notification message.
    /// </summary>
    Task SendAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the notification client is properly configured.
    /// </summary>
    bool IsConfigured();
}

/// <summary>
/// Console-based notification client for testing and development.
/// Simply outputs notifications to the console.
/// </summary>
public class ConsoleNotificationClient : INotificationClient
{
    public Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{title}] {message}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public bool IsConfigured() => true;
}

/// <summary>
/// Email notification client for sending notifications via email.
/// Requires SMTP configuration.
/// </summary>
public class EmailNotificationClient : INotificationClient
{
    private readonly string? _smtpServer;
    private readonly int _smtpPort;
    private readonly string? _fromAddress;
    private readonly string? _toAddress;
    private readonly string? _username;
    private readonly string? _password;

    public EmailNotificationClient(
        string? smtpServer = null,
        int smtpPort = 587,
        string? fromAddress = null,
        string? toAddress = null,
        string? username = null,
        string? password = null)
    {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _fromAddress = fromAddress;
        _toAddress = toAddress;
        _username = username;
        _password = password;
    }

    public async Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            throw new InvalidOperationException("Email notification client is not configured");

        // Email sending would be implemented here
        // For now, this is a stub that would use System.Net.Mail.SmtpClient
        await Task.CompletedTask;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_smtpServer) &&
               !string.IsNullOrEmpty(_fromAddress) &&
               !string.IsNullOrEmpty(_toAddress);
    }
}

/// <summary>
/// Slack notification client for sending messages to Slack channels.
/// Uses Slack webhook URLs for message delivery.
/// </summary>
public class SlackNotificationClient : INotificationClient
{
    private readonly string? _webhookUrl;
    private readonly HttpClientFactory _httpClientFactory;

    public SlackNotificationClient(string? webhookUrl = null, HttpClientFactory? httpClientFactory = null)
    {
        _webhookUrl = webhookUrl;
        _httpClientFactory = httpClientFactory ?? new HttpClientFactory();
    }

    public async Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            throw new InvalidOperationException("Slack notification client is not configured");

        var payload = new
        {
            text = title,
            blocks = new object[]
            {
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*{title}*\n{message}"
                    }
                }
            }
        };

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var client = _httpClientFactory.GetClient("slack");
            var response = await client.PostAsync(_webhookUrl!, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send Slack notification: {ex.Message}", ex);
        }
    }

    public bool IsConfigured() => !string.IsNullOrEmpty(_webhookUrl);
}
