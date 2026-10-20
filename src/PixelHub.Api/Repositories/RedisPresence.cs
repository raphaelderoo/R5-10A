using StackExchange.Redis;

namespace PixelHub.Api.Repositories;

// Sorted set « enligne » : membre = pseudo, score = horodatage Unix (secondes)
// du dernier signe de vie. Un joueur est en ligne si son score date de moins de 60 s.
public class RedisPresence
{
    private const string Key = "enligne";
    private static readonly TimeSpan Fenetre = TimeSpan.FromSeconds(60);
    private readonly IDatabase _db;

    public RedisPresence(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    private static double Maintenant() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // ZADD enligne <maintenant> <pseudo> : ajoute ou met à jour le score
    public async Task TouchAsync(string pseudo) =>
        await _db.SortedSetAddAsync(Key, pseudo, Maintenant());

    // ZREMRANGEBYSCORE enligne -inf (maintenant - fenêtre), puis ZCARD enligne
    public async Task<long> CountOnlineAsync()
    {
        var limite = Maintenant() - Fenetre.TotalSeconds;
        await _db.SortedSetRemoveRangeByScoreAsync(Key, double.NegativeInfinity, limite, Exclude.Stop);
        return await _db.SortedSetLengthAsync(Key);
    }
}
