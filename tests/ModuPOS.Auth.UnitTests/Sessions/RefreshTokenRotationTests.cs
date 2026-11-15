using ModuPOS.Auth.Sessions;

namespace ModuPOS.Auth.UnitTests.Sessions;

public class RefreshTokenRotationTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc);

    private static (RefreshToken Token, Session Session) Pair(
        DateTime? usedAt = null,
        DateTime? tokenExpires = null,
        SessionStatus status = SessionStatus.Active,
        DateTime? sessionExpires = null)
    {
        var session = new Session { Status = status, ExpiresAt = sessionExpires ?? Now.AddDays(7) };
        var token = new RefreshToken
        {
            TokenHash = new byte[32],
            Session = session,
            ExpiresAt = tokenExpires ?? Now.AddDays(7),
            UsedAt = usedAt
        };
        return (token, session);
    }

    [Fact]
    public void Unused_token_on_active_session_can_rotate()
    {
        var (token, session) = Pair();
        Assert.Equal(RefreshCheck.Valid, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Fact]
    public void Used_token_is_reuse_even_if_session_still_active()
    {
        var (token, session) = Pair(usedAt: Now.AddMinutes(-1));
        Assert.Equal(RefreshCheck.Reused, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Fact]
    public void Used_token_on_revoked_session_is_still_reported_as_reuse()
    {
        var (token, session) = Pair(usedAt: Now.AddMinutes(-1), status: SessionStatus.Revoked);
        Assert.Equal(RefreshCheck.Reused, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Fact]
    public void Expired_token_is_invalid()
    {
        var (token, session) = Pair(tokenExpires: Now.AddSeconds(-1));
        Assert.Equal(RefreshCheck.Invalid, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Theory]
    [InlineData(SessionStatus.Revoked)]
    [InlineData(SessionStatus.Expired)]
    public void Token_of_ended_session_is_invalid(SessionStatus status)
    {
        var (token, session) = Pair(status: status);
        Assert.Equal(RefreshCheck.Invalid, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Fact]
    public void Token_of_session_past_expiry_is_invalid()
    {
        var (token, session) = Pair(sessionExpires: Now.AddSeconds(-1));
        Assert.Equal(RefreshCheck.Invalid, RefreshTokenEvaluator.Evaluate(token, session, Now));
    }

    [Fact]
    public void Generated_tokens_are_unique_and_hash_to_32_bytes()
    {
        var a = RefreshTokenEvaluator.NewToken();
        var b = RefreshTokenEvaluator.NewToken();

        Assert.NotEqual(a, b);
        Assert.Equal(32, RefreshTokenEvaluator.Hash(a).Length);
        Assert.Equal(RefreshTokenEvaluator.Hash(a), RefreshTokenEvaluator.Hash(a));
    }
}
