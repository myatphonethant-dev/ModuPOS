using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModuPOS.Shared.Auditing;

namespace ModuPOS.Auth.Auditing;

/// <summary>Shared state between the worker and its health check.</summary>
public sealed class AuditWriterState
{
    private long _oldestUnsavedTicks;

    public DateTime? OldestUnsavedSince
    {
        get
        {
            var ticks = Interlocked.Read(ref _oldestUnsavedTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
        internal set => Interlocked.Exchange(ref _oldestUnsavedTicks, value?.Ticks ?? 0);
    }
}

/// <summary>
/// Saves queued (non-critical) audit entries in batches, retrying with backoff while storage is
/// down. Entries keep their original time. An alert is logged if anything stays unsaved for
/// more than 5 minutes (FR-040).
/// </summary>
internal sealed class AuditQueueWorker(
    AuditQueue queue,
    AuditWriterState state,
    IServiceScopeFactory scopes,
    TimeProvider time,
    IOptions<AuthOptions> options,
    ILogger<AuditQueueWorker> logger) : BackgroundService
{
    internal static readonly TimeSpan AlertAfter = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly AuditDeduplicator _dedup = new(TimeSpan.FromSeconds(options.Value.Audit.DedupWindowSeconds));
    private readonly List<AuditEntry> _pending = [];
    private DateTime _lastAlert = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var backoff = TimeSpan.FromSeconds(1);
        Task<bool>? waitForData = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            Drain();
            _pending.AddRange(_dedup.Flush(Now()));

            if (_pending.Count > 0)
            {
                if (await TrySaveAsync(stoppingToken))
                {
                    backoff = TimeSpan.FromSeconds(1);
                }
                else
                {
                    AlertIfStale();
                    await Delay(backoff, stoppingToken);
                    backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
                    continue;
                }
            }

            // Wake on new entries, or every second to close dedup windows.
            if (waitForData is null || waitForData.IsCompleted)
                waitForData = queue.Reader.WaitToReadAsync(stoppingToken).AsTask();

            await Task.WhenAny(waitForData, Delay(TimeSpan.FromSeconds(1), stoppingToken));
        }

        // Shutdown: best-effort final flush.
        Drain();
        _pending.AddRange(_dedup.Flush(Now(), force: true));
        if (_pending.Count > 0 && !await TrySaveAsync(CancellationToken.None))
            logger.LogCritical("Shutting down with {Count} unsaved audit entries.", _pending.Count);
    }

    private void Drain()
    {
        while (queue.Reader.TryRead(out var entry))
        {
            if (_dedup.Add(entry) is { } now)
                _pending.Add(now);
        }

        UpdateOldest();
    }

    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        if (await SaveAsync(_pending, ct))
        {
            _pending.Clear();
            UpdateOldest();
            return true;
        }

        if (_pending.Count == 1)
            return false;

        // The batch failed: save one by one so a single bad row cannot block the queue.
        var failed = new List<AuditEntry>();
        foreach (var entry in _pending)
        {
            if (!await SaveAsync([entry], ct))
                failed.Add(entry);
        }

        if (failed.Count < _pending.Count)
        {
            // Storage is up, so the remaining rows themselves are bad: drop them loudly.
            foreach (var entry in failed)
                logger.LogCritical("Dropping audit entry {Code} at {OccurredAt:o}: it cannot be saved.", entry.Code, entry.OccurredAt);
            _pending.Clear();
            UpdateOldest();
            return true;
        }

        return false;
    }

    private async Task<bool> SaveAsync(IReadOnlyCollection<AuditEntry> entries, CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();
            db.Set<AuditEntryRecord>().AddRange(entries.Select(AuditMapping.ToRecord));
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not save {Count} audit entries; will retry.", entries.Count);
            return false;
        }
    }

    private void UpdateOldest() =>
        state.OldestUnsavedSince = _pending.Count == 0 ? null : _pending.Min(e => e.OccurredAt);

    private void AlertIfStale()
    {
        var now = Now();
        if (state.OldestUnsavedSince is { } oldest && now - oldest > AlertAfter && now - _lastAlert > TimeSpan.FromMinutes(1))
        {
            _lastAlert = now;
            logger.LogCritical(
                "ALERT: {Count} audit entries unsaved since {Oldest:o} (over {Minutes} minutes).",
                _pending.Count, oldest, AlertAfter.TotalMinutes);
        }
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;

    private async Task Delay(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, time, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

/// <summary>Reports Degraded while audit entries have been unsaved for more than 5 minutes.</summary>
internal sealed class AuditHealthCheck(AuditWriterState state, TimeProvider time) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var oldest = state.OldestUnsavedSince;
        var stale = oldest is { } o && time.GetUtcNow().UtcDateTime - o > AuditQueueWorker.AlertAfter;

        return Task.FromResult(stale
            ? HealthCheckResult.Degraded($"Audit entries unsaved since {oldest:o}.")
            : HealthCheckResult.Healthy());
    }
}
