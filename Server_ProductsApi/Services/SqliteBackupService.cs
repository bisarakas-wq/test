using Microsoft.Data.Sqlite;

namespace Server_ProductsApi.Services;

public sealed class SqliteBackupService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<SqliteBackupService> logger) : BackgroundService
{
    private const int RetainedBackups = 14;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var databaseSetting = configuration["Database:Path"] ?? "App_Data/bookings.db";
        var databasePath = Path.GetFullPath(databaseSetting, environment.ContentRootPath);

        await CreateBackupAsync(databasePath, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CreateBackupAsync(databasePath, stoppingToken);
        }
    }

    private async Task CreateBackupAsync(string databasePath, CancellationToken cancellationToken)
    {
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "Backups");
        var backupPath = Path.Combine(backupDirectory, $"bookings-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");

        try
        {
            Directory.CreateDirectory(backupDirectory);
            var sourceConnectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
            var destinationConnectionString = new SqliteConnectionStringBuilder { DataSource = backupPath }.ToString();
            await using var source = new SqliteConnection(sourceConnectionString);
            await using var destination = new SqliteConnection(destinationConnectionString);
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);

            foreach (var oldBackup in new DirectoryInfo(backupDirectory)
                         .GetFiles("bookings-*.db")
                         .OrderByDescending(file => file.CreationTimeUtc)
                         .Skip(RetainedBackups))
            {
                oldBackup.Delete();
            }

            logger.LogInformation("SQLite backup created at {BackupPath}", backupPath);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to create SQLite backup at {BackupPath}; the API will continue and retry at the next interval", backupPath);
        }
    }
}
