#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Api;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller for backup integrity checks.
/// Exposes endpoints to run quick and full integrity checks against SQLite
/// database files or stored backup artifacts.
/// </summary>
public class IntegrityController
{
    private readonly IIntegrityCheckerService _integrityChecker;
    private readonly ILogger<IntegrityController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="IntegrityController"/>.
    /// </summary>
    public IntegrityController(
        IIntegrityCheckerService integrityChecker,
        ILogger<IntegrityController> logger)
    {
        _integrityChecker = integrityChecker;
        _logger = logger;
    }

    /// <summary>
    /// Runs a comprehensive integrity check (quick + full + foreign-key) on the database
    /// at the provided path.
    /// </summary>
    public async Task<ApiResponse<object>> CheckDatabase(
        CheckDatabaseRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var report = await _integrityChecker.CheckDatabaseAsync(
                request.DatabasePath, request.FullCheck, ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                report.Id,
                report.DatabasePath,
                report.CheckedAt,
                DurationMs = (long)report.Duration.TotalMilliseconds,
                report.IsHealthy,
                report.PassedQuickCheck,
                report.PassedFullCheck,
                report.PassedForeignKeyCheck,
                report.QuickCheckErrors,
                report.FullCheckErrors,
                report.ForeignKeyErrors,
                report.PageCount,
                report.PageSize,
                report.FreePageCount,
                report.JournalMode,
                report.HasUncheckpointedWal,
                report.TableCount,
                report.Summary
            }, report.IsHealthy ? "Database is healthy" : "Integrity issues detected");
        }
        catch (FileNotFoundException ex)
        {
            return ApiResponse<object>.ErrorResponse("DATABASE_NOT_FOUND", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Integrity check failed for {Path}", request.DatabasePath);
            return ApiResponse<object>.ErrorResponse("INTEGRITY_CHECK_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Runs a fast structural-only (<c>PRAGMA quick_check</c>) scan.
    /// Returns a lightweight result suitable for frequent health-polling.
    /// </summary>
    public async Task<ApiResponse<object>> QuickCheck(
        QuickCheckRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var passed = await _integrityChecker.QuickCheckAsync(request.DatabasePath, ct);
            return ApiResponse<object>.SuccessResponse(new
            {
                request.DatabasePath,
                Passed = passed,
                CheckedAt = DateTime.UtcNow
            }, passed ? "Quick check passed" : "Quick check failed");
        }
        catch (FileNotFoundException ex)
        {
            return ApiResponse<object>.ErrorResponse("DATABASE_NOT_FOUND", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick check failed for {Path}", request.DatabasePath);
            return ApiResponse<object>.ErrorResponse("QUICK_CHECK_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Runs a comprehensive integrity check against a stored backup file.
    /// </summary>
    public async Task<ApiResponse<object>> CheckBackupFile(
        CheckBackupFileRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var report = await _integrityChecker.CheckBackupFileAsync(request.BackupFilePath, ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                report.Id,
                report.DatabasePath,
                report.CheckedAt,
                DurationMs = (long)report.Duration.TotalMilliseconds,
                report.IsHealthy,
                report.PassedQuickCheck,
                report.PassedFullCheck,
                report.PassedForeignKeyCheck,
                report.TableCount,
                report.PageCount,
                report.Summary
            }, report.IsHealthy ? "Backup file integrity verified" : "Backup file has integrity issues");
        }
        catch (FileNotFoundException ex)
        {
            return ApiResponse<object>.ErrorResponse("BACKUP_FILE_NOT_FOUND", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup integrity check failed for {Path}", request.BackupFilePath);
            return ApiResponse<object>.ErrorResponse("BACKUP_INTEGRITY_CHECK_FAILED", ex.Message);
        }
    }
}

/// <summary>Request model for full database integrity check.</summary>
public class CheckDatabaseRequest
{
    /// <summary>Path to the SQLite database file.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c> (default), performs quick check + full integrity check + foreign key check.
    /// When <c>false</c>, performs only the quick structural check.
    /// </summary>
    public bool FullCheck { get; set; } = true;
}

/// <summary>Request model for a fast quick-check scan.</summary>
public class QuickCheckRequest
{
    /// <summary>Path to the SQLite database file.</summary>
    public string DatabasePath { get; set; } = string.Empty;
}

/// <summary>Request model for checking a backup file.</summary>
public class CheckBackupFileRequest
{
    /// <summary>Local path to the backup file.</summary>
    public string BackupFilePath { get; set; } = string.Empty;
}
