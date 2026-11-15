using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ModuPOS.Auth.Sessions;

/// <summary>
/// Fast "is this session still usable?" check for pipeline step 1. Revocation calls
/// <see cref="InvalidateAsync"/>, so sign-out takes effect on the very next request.
/// </summary>
public sealed class SessionStatusCache(DbContext db, HybridCache cache, TimeProvider time)
{
    private static readonly HybridCacheEntryOptions CacheFor1Minute = new()
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public async Task<bool> IsActiveAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Cached value: the session's expiry while Active, or 0 when it is not usable.
        var expiresTicks = await cache.GetOrCreateAsync(
            Key(sessionId),
            (db, sessionId),
            static async (s, token) =>
            {
                var session = await s.db.Set<Session>()
                    .AsNoTracking()
                    .Where(x => x.Id == s.sessionId)
                    .Select(x => new { x.Status, x.ExpiresAt })
                    .FirstOrDefaultAsync(token);

                return session is { Status: SessionStatus.Active } ? session.ExpiresAt.Ticks : 0L;
            },
            CacheFor1Minute,
            cancellationToken: ct);

        return expiresTicks > time.GetUtcNow().UtcDateTime.Ticks;
    }

    public ValueTask InvalidateAsync(Guid sessionId, CancellationToken ct = default) =>
        cache.RemoveAsync(Key(sessionId), ct);

    private static string Key(Guid sessionId) => $"sess:{sessionId:N}";
}
