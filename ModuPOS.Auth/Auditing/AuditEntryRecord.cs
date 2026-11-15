using ModuPOS.Shared.Auditing;

namespace ModuPOS.Auth.Auditing;

/// <summary>Persisted, append-only audit row.</summary>
public class AuditEntryRecord
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public AuditSeverity Severity { get; set; }

    public DateTime OccurredAt { get; set; }

    public AuditOutcome Outcome { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? ActorProfileId { get; set; }

    /// <summary>Null for platform or pre-tenant events (e.g. failed sign-in of an unknown user).</summary>
    public Guid? BusinessId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? SessionId { get; set; }

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? SourceAddress { get; set; }

    public string? DeviceDescription { get; set; }

    /// <summary>JSON object of event-specific details; never contains credentials.</summary>
    public string Details { get; set; } = "{}";

    public int RepeatCount { get; set; } = 1;

    public string? DedupKey { get; set; }
}
