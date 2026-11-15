using ModuPOS.Auth;
using ModuPOS.Auth.Accounts;

namespace ModuPOS.Auth.UnitTests.Accounts;

public class LockoutTests
{
    private static readonly DateTime T0 = new(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly LockoutPolicy _policy = new(new LockoutOptions { MaxFailures = 5, WindowMinutes = 15, LockMinutes = 15 });

    private static UserAccount NewAccount() => new() { Username = "alice", Email = "alice@example.test" };

    [Fact]
    public void Fifth_failure_within_window_locks_for_15_minutes()
    {
        var account = NewAccount();

        for (var i = 1; i <= 4; i++)
            Assert.False(_policy.RegisterFailure(account, T0.AddMinutes(i)));

        Assert.True(_policy.RegisterFailure(account, T0.AddMinutes(5)));
        Assert.Equal(T0.AddMinutes(20), account.LockedUntil);
        Assert.True(_policy.IsLocked(account, T0.AddMinutes(19)));
        Assert.False(_policy.IsLocked(account, T0.AddMinutes(20)));
    }

    [Fact]
    public void Failures_outside_the_window_start_a_new_count()
    {
        var account = NewAccount();

        for (var i = 0; i < 4; i++)
            _policy.RegisterFailure(account, T0.AddMinutes(i));

        // 16 minutes after the first failure: window restarts at 1.
        Assert.False(_policy.RegisterFailure(account, T0.AddMinutes(16)));
        Assert.Equal(1, account.FailedSignInCount);
        Assert.Null(account.LockedUntil);
    }

    [Fact]
    public void Success_resets_the_counter()
    {
        var account = NewAccount();
        _policy.RegisterFailure(account, T0);
        _policy.RegisterFailure(account, T0.AddMinutes(1));

        _policy.Reset(account);

        Assert.Equal(0, account.FailedSignInCount);
        Assert.Null(account.FailureWindowStart);
        Assert.Null(account.LockedUntil);
    }

    [Fact]
    public void After_lock_expires_failures_count_from_zero()
    {
        var account = NewAccount();
        for (var i = 0; i < 5; i++)
            _policy.RegisterFailure(account, T0);

        Assert.False(_policy.RegisterFailure(account, T0.AddMinutes(30)));
        Assert.Equal(1, account.FailedSignInCount);
    }
}
