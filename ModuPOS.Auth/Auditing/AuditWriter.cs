using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModuPOS.Shared.Auditing;

namespace ModuPOS.Auth.Auditing;

/// <summary>Bounded in-process queue feeding <see cref="AuditQueueWorker"/>.</summary>
public sealed class AuditQueue
{
    private readonly Channel<AuditEntry> _channel = Channel.CreateBounded<AuditEntry>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    internal ChannelReader<AuditEntry> Reader => _channel.Reader;

    internal bool TryWrite(AuditEntry entry) => _channel.Writer.TryWrite(entry);
}

internal static class AuditMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Rejects unknown codes and any details key that could hold a credential (FR-035).</summary>
    public static AuditEntryRecord ToRecord(AuditEntry entry)
    {
        if (!AuditCodes.All.Contains(entry.Code))
            throw new ArgumentException($"'{entry.Code}' is not in the audit catalog.", nameof(entry));

        foreach (var key in entry.Details.Keys)
        {
            if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("token", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Audit details must not contain credential field '{key}'.", nameof(entry));
            }
        }

        return new AuditEntryRecord
        {
            Code = entry.Code,
            Severity = entry.Severity,
            OccurredAt = entry.OccurredAt,
            Outcome = entry.Outcome,
            ActorUserId = entry.ActorUserId,
            ActorProfileId = entry.ActorProfileId,
            BusinessId = entry.BusinessId,
            BranchId = entry.BranchId,
            SessionId = entry.SessionId,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            SourceAddress = entry.SourceAddress,
            DeviceDescription = entry.DeviceDescription,
            Details = JsonSerializer.Serialize(entry.Details, Json),
            RepeatCount = entry.RepeatCount,
            DedupKey = entry.DedupKey is null ? null : HashKey(entry.DedupKey)
        };
    }

    /// <summary>Fixed-length (64 hex chars) form of a dedup key, whatever its length.</summary>
    private static string HashKey(string key) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
}

internal sealed class AuditWriter(
    DbContext db,
    AuditQueue queue,
    ILogger<AuditWriter> logger) : IAuditWriter
{
    public void WriteCritical(AuditEntry entry) =>
        db.Set<AuditEntryRecord>().Add(AuditMapping.ToRecord(entry));

    public void Enqueue(AuditEntry entry)
    {
        // Validate now so a bad entry fails at the call site, not silently in the worker.
        _ = AuditMapping.ToRecord(entry);

        if (!queue.TryWrite(entry))
            logger.LogCritical("Audit queue is full; dropped {Code} at {OccurredAt:o}.", entry.Code, entry.OccurredAt);
    }
}
