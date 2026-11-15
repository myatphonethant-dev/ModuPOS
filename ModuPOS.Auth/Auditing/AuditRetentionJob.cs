using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ModuPOS.Auth.Auditing;

/// <summary>
/// Once a day, deletes audit entries older than one year in batches (FR-036). The database only
/// allows audit deletes from a connection that has set the retention session flag, and only for
/// rows past the retention period (see the AuditAppendOnly migration).
/// </summary>
public sealed class AuditRetentionJob(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<AuditRetentionJob> logger) : BackgroundService
{
    public const int BatchSize = 5_000;
    public static readonly TimeSpan Retention = TimeSpan.FromDays(365);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting before the first run.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), time, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1), time);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Audit retention run failed; will try again tomorrow.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    /// <summary>Deletes everything past retention. Returns the number of rows removed.</summary>
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var cutoff = time.GetUtcNow().UtcDateTime - Retention;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("EXEC sp_set_session_context @key = N'modupos.audit_retention', @value = 1;", ct);

            var total = 0;
            int deleted;
            do
            {
                deleted = await db.Set<AuditEntryRecord>()
                    .IgnoreQueryFilters()
                    .Where(a => a.OccurredAt < cutoff)
                    .OrderBy(a => a.OccurredAt)
                    .Take(BatchSize)
                    .ExecuteDeleteAsync(ct);
                total += deleted;
            }
            while (deleted == BatchSize);

            if (total > 0)
                logger.LogInformation("Audit retention removed {Count} entries older than {Cutoff:o}.", total, cutoff);

            return total;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
