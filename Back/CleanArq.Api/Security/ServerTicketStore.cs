using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace CleanArq.Api.Security;

// The browser cookie contains only a protected reference. Claims and tokens stay here.
// Single-instance development store: restarting the API invalidates all sessions.
public sealed class ServerTicketStore(IMemoryCache cache) : ITicketStore
{
    private readonly object gate = new();
    public const string SessionId = "bff_session_id";
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        ticket.Properties.Items[SessionId] = key;
        return Store(key, ticket);
    }
    private async Task<string> Store(string key, AuthenticationTicket ticket) { await RenewAsync(key, ticket); return key; }
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        lock (gate)
        {
            if (!cache.TryGetValue("revoked:" + key, out _))
                cache.Set("ticket:" + key, TicketSerializer.Default.Serialize(ticket), ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddMinutes(30));
        }
        return Task.CompletedTask;
    }
    public Task<AuthenticationTicket?> RetrieveAsync(string key) => Task.FromResult(
        cache.TryGetValue<byte[]>("ticket:" + key, out var value) && value is not null ? TicketSerializer.Default.Deserialize(value) : null);
    public Task RemoveAsync(string key)
    {
        lock (gate) { cache.Remove("ticket:" + key); cache.Set("revoked:" + key, true, TimeSpan.FromHours(8)); }
        return Task.CompletedTask;
    }
    public SemaphoreSlim RefreshLock(string key)
    {
        lock (gate) return cache.GetOrCreate("lock:" + key, entry =>
        { entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8); return new SemaphoreSlim(1, 1); })!;
    }
}
