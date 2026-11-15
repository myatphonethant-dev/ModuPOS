using ModuPOS.Auth.Auditing;
using ModuPOS.Shared.Auditing;

namespace ModuPOS.Auth.UnitTests.Auditing;

public class AuditWriterTests
{
    private static readonly DateTime T0 = new(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc);

    private static AuditEntry Entry(string? key, DateTime at, string code = AuditCodes.PermissionDenied) =>
        new() { Code = code, OccurredAt = at, DedupKey = key };

    [Fact]
    public void Entries_without_a_key_pass_straight_through()
    {
        var dedup = new AuditDeduplicator(TimeSpan.FromMinutes(1));
        var entry = Entry(null, T0, AuditCodes.SignInSucceeded);

        Assert.Same(entry, dedup.Add(entry));
        Assert.Empty(dedup.Flush(T0.AddHours(1)));
    }

    [Fact]
    public void Same_key_within_the_window_becomes_one_entry_with_a_count()
    {
        var dedup = new AuditDeduplicator(TimeSpan.FromMinutes(1));

        Assert.Null(dedup.Add(Entry("k", T0)));
        Assert.Null(dedup.Add(Entry("k", T0.AddSeconds(10))));
        Assert.Null(dedup.Add(Entry("k", T0.AddSeconds(50))));
        Assert.Empty(dedup.Flush(T0.AddSeconds(59)));

        var flushed = Assert.Single(dedup.Flush(T0.AddSeconds(60)));
        Assert.Equal(3, flushed.RepeatCount);
        Assert.Equal(T0, flushed.OccurredAt); // keeps the first occurrence time
        Assert.Equal(0, dedup.OpenGroups);
    }

    [Fact]
    public void Different_keys_are_kept_apart_and_forced_flush_closes_everything()
    {
        var dedup = new AuditDeduplicator(TimeSpan.FromMinutes(1));
        dedup.Add(Entry("a", T0));
        dedup.Add(Entry("b", T0));
        dedup.Add(Entry("a", T0));

        var flushed = dedup.Flush(T0, force: true);

        Assert.Equal(2, flushed.Count);
        Assert.Equal(2, flushed.Single(e => e.DedupKey == "a").RepeatCount);
        Assert.Equal(1, flushed.Single(e => e.DedupKey == "b").RepeatCount);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("newPassword")]
    [InlineData("refreshToken")]
    [InlineData("accessToken")]
    [InlineData("token")]
    public void Credential_fields_are_rejected(string key)
    {
        var entry = new AuditEntry { Code = AuditCodes.SignInFailed, Details = new Dictionary<string, object?> { [key] = "secret" } };

        Assert.Throws<ArgumentException>(() => AuditMapping.ToRecord(entry));
    }

    [Fact]
    public void Codes_outside_the_catalog_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => AuditMapping.ToRecord(new AuditEntry { Code = "SALE.CREATED" }));
    }

    [Fact]
    public void Dedup_key_is_stored_as_a_fixed_length_hash()
    {
        var record = AuditMapping.ToRecord(Entry(new string('x', 1000), T0));

        Assert.Equal(64, record.DedupKey!.Length);
    }

    [Fact]
    public void Details_are_stored_as_json()
    {
        var record = AuditMapping.ToRecord(new AuditEntry
        {
            Code = AuditCodes.SignOutAll,
            Details = new Dictionary<string, object?> { ["sessionsEnded"] = 2 }
        });

        Assert.Equal("{\"sessionsEnded\":2}", record.Details);
    }
}
