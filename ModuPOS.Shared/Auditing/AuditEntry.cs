namespace ModuPOS.Shared.Auditing;

public enum AuditSeverity
{
    Info,
    Warning,
    High
}

public enum AuditOutcome
{
    Succeeded,
    Denied,
    Blocked
}

/// <summary>One security event from the FR-033 catalog.</summary>
public sealed record AuditEntry
{
    public required string Code { get; init; }

    public AuditSeverity Severity { get; init; } = AuditSeverity.Info;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public AuditOutcome Outcome { get; init; } = AuditOutcome.Succeeded;

    public Guid? ActorUserId { get; init; }

    public Guid? ActorProfileId { get; init; }

    public Guid? BusinessId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? SessionId { get; init; }

    public string? TargetType { get; init; }

    public string? TargetId { get; init; }

    public string? SourceAddress { get; init; }

    public string? DeviceDescription { get; init; }

    public IReadOnlyDictionary<string, object?> Details { get; init; } =
        new Dictionary<string, object?>();

    public int RepeatCount { get; init; } = 1;

    /// <summary>When set, identical entries within 1 minute are collapsed into one (FR-038).</summary>
    public string? DedupKey { get; init; }
}
