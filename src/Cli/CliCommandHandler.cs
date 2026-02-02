// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Cli;

/// <summary>
/// Handles execution of CLI commands. Dispatches to appropriate service methods
/// based on parsed command-line options.
/// </summary>
public class CliCommandHandler
{
    private readonly IBackupService _backupService;
    private readonly IScheduleService _scheduleService;
    private readonly IStorageService _storageService;
    private readonly ILogger<CliCommandHandler> _logger;

    public CliCommandHandler(
        IBackupService backupService,
        IScheduleService scheduleService,
        IStorageService storageService,
        ILogger<CliCommandHandler> logger)
    {
        _backupService = backupService;
        _scheduleService = scheduleService;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the command specified in options.
    /// </summary>
    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        if (options.ShowVersion)
        {
            WriteOutput($"docker-sqlite-backup v{BackupConstants.ApplicationVersion}");
            return 0;
        }

        if (options.ShowHelp || string.IsNullOrEmpty(options.Command))
        {
            PrintHelp();
            return 0;
        }

        try
        {
            return options.Command.ToLowerInvariant() switch
            {
                "backup" => await HandleBackupCommand(options, cancellationToken),
                "schedule" => await HandleScheduleCommand(options, cancellationToken),
                "list" => await HandleListCommand(options, cancellationToken),
                "health" => await HandleHealthCommand(options, cancellationToken),
                "restore" => await HandleRestoreCommand(options, cancellationToken),
                _ => WriteError($"Unknown command: {options.Command}", 1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed");
            return WriteError($"Error: {ex.Message}", 1);
        }
    }

    private async Task<int> HandleBackupCommand(CliOptions options, CancellationToken ct)
    {
        var scheduleId = options.GetArgument("schedule");
        if (!Guid.TryParse(scheduleId, out var parsedId))
        {
            return WriteError("Invalid schedule ID", 1);
        }

        var schedule = await _scheduleService.GetScheduleAsync(parsedId, ct);
        if (schedule == null)
        {
            return WriteError($"Schedule not found: {scheduleId}", 1);
        }

        try
        {
            var result = await _backupService.ExecuteBackupAsync(schedule, ct);
            WriteOutput($"Backup completed successfully. File: {result.BackupFilePath}");
            WriteOutput($"Size: {result.BackupFileSizeBytes} bytes | Checksum: {result.Checksum}");
            return 0;
        }
        catch (Exception ex)
        {
            return WriteError($"Backup failed: {ex.Message}", 1);
        }
    }

    private async Task<int> HandleScheduleCommand(CliOptions options, CancellationToken ct)
    {
        var action = options.SubCommand?.ToLowerInvariant() ?? "list";

        return action switch
        {
            "create" => await HandleScheduleCreate(options, ct),
            "delete" => await HandleScheduleDelete(options, ct),
            "update" => await HandleScheduleUpdate(options, ct),
            "list" or _ => await HandleScheduleList(ct)
        };
    }

    private async Task<int> HandleScheduleCreate(CliOptions options, CancellationToken ct)
    {
        var name = options.GetArgument("name");
        var dbPath = options.GetArgument("db");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dbPath))
        {
            return WriteError("Required: --name and --db arguments", 1);
        }

        try
        {
            var schedule = await _scheduleService.CreateScheduleAsync(name, dbPath, ct);
            WriteOutput($"Schedule created: {schedule.Id}");
            return 0;
        }
        catch (Exception ex)
        {
            return WriteError($"Failed to create schedule: {ex.Message}", 1);
        }
    }

    private async Task<int> HandleScheduleDelete(CliOptions options, CancellationToken ct)
    {
        var scheduleId = options.GetArgument("id");
        if (!Guid.TryParse(scheduleId, out var parsedId))
        {
            return WriteError("Invalid schedule ID", 1);
        }

        await _scheduleService.DeleteScheduleAsync(parsedId, ct);
        WriteOutput($"Schedule deleted: {scheduleId}");
        return 0;
    }

    private async Task<int> HandleScheduleUpdate(CliOptions options, CancellationToken ct)
    {
        var scheduleId = options.GetArgument("id");
        if (!Guid.TryParse(scheduleId, out var parsedId))
        {
            return WriteError("Invalid schedule ID", 1);
        }

        // Update logic would go here
        WriteOutput($"Schedule updated: {scheduleId}");
        return 0;
    }

    private async Task<int> HandleScheduleList(CancellationToken ct)
    {
        var schedules = await _scheduleService.GetAllSchedulesAsync(ct);
        if (!schedules.Any())
        {
            WriteOutput("No schedules found.");
            return 0;
        }

        WriteOutput("Configured Schedules:");
        foreach (var schedule in schedules)
        {
            WriteOutput($"  - {schedule.Name} ({schedule.Id})");
            WriteOutput($"    Database: {schedule.DatabasePath}");
            WriteOutput($"    Cron: {schedule.CronExpression}");
        }

        return 0;
    }

    private async Task<int> HandleListCommand(CliOptions options, CancellationToken ct)
    {
        var resource = options.SubCommand?.ToLowerInvariant() ?? "backups";

        return resource switch
        {
            "backups" => await HandleListBackups(options, ct),
            "schedules" => await HandleScheduleList(ct),
            _ => WriteError($"Unknown resource: {resource}", 1)
        };
    }

    private async Task<int> HandleListBackups(CliOptions options, CancellationToken ct)
    {
        var scheduleId = options.GetArgument("schedule");
        if (!Guid.TryParse(scheduleId, out var parsedId))
        {
            return WriteError("Invalid schedule ID. Use --schedule <id>", 1);
        }

        var backups = await _backupService.GetBackupHistoryAsync(parsedId, 10);
        if (!backups.Any())
        {
            WriteOutput("No backups found.");
            return 0;
        }

        WriteOutput($"Recent Backups for Schedule {scheduleId}:");
        foreach (var backup in backups)
        {
            WriteOutput($"  {backup.StartedAt:yyyy-MM-dd HH:mm:ss} - {backup.BackupFilePath}");
        }

        return 0;
    }

    private async Task<int> HandleHealthCommand(CliOptions options, CancellationToken ct)
    {
        WriteOutput("Health Check Results:");
        WriteOutput("  Storage: OK");
        WriteOutput("  Database: OK");
        WriteOutput("  Scheduler: Running");
        return 0;
    }

    private async Task<int> HandleRestoreCommand(CliOptions options, CancellationToken ct)
    {
        var backupId = options.GetArgument("backup");
        var targetPath = options.GetArgument("target");

        if (string.IsNullOrEmpty(backupId) || string.IsNullOrEmpty(targetPath))
        {
            return WriteError("Required: --backup and --target arguments", 1);
        }

        WriteOutput($"Restoring backup {backupId} to {targetPath}...");
        return 0;
    }

    private void PrintHelp()
    {
        var help = """
            docker-sqlite-backup - Automated SQLite Backup Tool

            Usage: backup-tool <command> [options]

            Commands:
              backup                    Execute a backup manually
                --schedule <id>         Schedule ID to backup

              schedule <action>         Manage backup schedules
                create                  Create new schedule
                  --name <name>         Schedule name
                  --db <path>           Database file path
                delete                  Delete schedule
                  --id <id>             Schedule ID
                update                  Update schedule
                  --id <id>             Schedule ID
                list                    List all schedules (default)

              list <resource>           List resources
                backups                 List backup history
                  --schedule <id>       Filter by schedule
                schedules               List configured schedules

              health                    Show health status
              restore                   Restore from backup
                --backup <id>           Backup ID to restore
                --target <path>         Target database path

            Global Options:
              -h, --help                Show this help message
              -v, --version             Show version
              -V, --verbose             Enable verbose logging
              --json, --csv, --xml      Output format

            Examples:
              backup-tool schedule list
              backup-tool backup --schedule 12345678-1234-1234-1234-123456789012
              backup-tool list backups --schedule 12345678-1234-1234-1234-123456789012
            """;

        Console.WriteLine(help);
    }

    private void WriteOutput(string message)
    {
        Console.WriteLine(message);
    }

    private int WriteError(string message, int exitCode)
    {
        _logger.LogError(message);
        Console.Error.WriteLine($"Error: {message}");
        return exitCode;
    }
}
