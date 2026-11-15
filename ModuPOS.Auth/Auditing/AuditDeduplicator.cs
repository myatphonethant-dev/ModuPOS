using ModuPOS.Shared.Auditing;

namespace ModuPOS.Auth.Auditing;

/// <summary>
/// Collapses entries that share a <see cref="AuditEntry.DedupKey"/> within a window into one
/// entry with a repeat count (FR-038). Entries without a key pass straight through.
/// </summary>
internal sealed class AuditDeduplicator(TimeSpan window)
{
    private readonly Dictionary<string, (AuditEntry First, int Count)> _open = new(StringComparer.Ordinal);

    public int OpenGroups => _open.Count;

    /// <summary>Returns the entry if it should be written now, or null if it was folded into a group.</summary>
    public AuditEntry? Add(AuditEntry entry)
    {
        if (entry.DedupKey is null)
            return entry;

        _open[entry.DedupKey] = _open.TryGetValue(entry.DedupKey, out var group)
            ? (group.First, group.Count + entry.RepeatCount)
            : (entry, entry.RepeatCount);

        return null;
    }

    /// <summary>Closes groups whose window has elapsed (or all, when forced) and returns them.</summary>
    public List<AuditEntry> Flush(DateTime now, bool force = false)
    {
        var ready = new List<AuditEntry>();

        foreach (var (key, group) in _open.ToList())
        {
            if (force || now - group.First.OccurredAt >= window)
            {
                ready.Add(group.First with { RepeatCount = group.Count });
                _open.Remove(key);
            }
        }

        return ready;
    }
}
