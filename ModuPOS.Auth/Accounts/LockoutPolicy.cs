namespace ModuPOS.Auth.Accounts;

/// <summary>N failures within a rolling window lock the account for a fixed time (FR-004).</summary>
public sealed class LockoutPolicy(LockoutOptions options)
{
    public bool IsLocked(UserAccount account, DateTime now) =>
        account.LockedUntil is { } until && until > now;

    /// <summary>Records a failed sign-in. Returns true when this failure locked the account.</summary>
    public bool RegisterFailure(UserAccount account, DateTime now)
    {
        var window = TimeSpan.FromMinutes(options.WindowMinutes);

        var windowExpired = account.FailureWindowStart is not { } start || now - start >= window;
        var lockExpired = account.LockedUntil is { } until && until <= now;

        if (windowExpired || lockExpired)
        {
            account.FailureWindowStart = now;
            account.FailedSignInCount = 0;
            account.LockedUntil = null;
        }

        account.FailedSignInCount++;

        if (account.FailedSignInCount < options.MaxFailures)
            return false;

        account.LockedUntil = now.AddMinutes(options.LockMinutes);
        return true;
    }

    public void Reset(UserAccount account)
    {
        account.FailedSignInCount = 0;
        account.FailureWindowStart = null;
        account.LockedUntil = null;
    }
}
