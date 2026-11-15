namespace ModuPOS.Shared.Auditing;

public interface IAuditWriter
{
    /// <summary>
    /// Adds the entry to the current unit of work. It is saved with the caller's change,
    /// so if the audit row cannot be saved the change fails too (FR-040).
    /// </summary>
    void WriteCritical(AuditEntry entry);

    /// <summary>Queues the entry for background saving with retry; never blocks the caller.</summary>
    void Enqueue(AuditEntry entry);
}
