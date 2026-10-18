using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TarkamArena.Core.Content;
using TarkamArena.Core.Progression;

namespace TarkamArena.Server;

public sealed record PlayerTicket(string Name, int Rating, string CharacterId, int UdpPort);

public sealed record MatchFound(string MatchId, bool IsHost, string PeerAddress, int PeerPort, uint Seed, string OpponentName, int OpponentRating, string StageId, string OpponentCharacterId);

public sealed record LeaderboardEntry(string Name, int Rating, int Wins, int Losses, string? Clan);

public sealed record ClanInfo(string Name, string Tag, int Members, int WarPoints);

/// <summary>A ranked match between two connections, waiting for both result reports.</summary>
internal sealed class PendingMatch
{
    public required string Id;
    public required string HostConnection;
    public required string GuestConnection;
    public required string HostName;
    public required string GuestName;
    public bool? HostWon;
    public bool? GuestWon;
}

/// <summary>
/// The online lobby: a rating-window matchmaking queue, result reporting with Elo updates, the
/// leaderboard and clans (clan wars award points to the winner's clan). Fights themselves run peer
/// to peer, so the server only brokers and records.
/// </summary>
public sealed class ArenaHub(Matchmaker matchmaker, IArenaStore store) : Hub
{
    public Task JoinRankedQueue(PlayerTicket ticket)
    {
        var address = Context.GetHttpContext()?.Connection.RemoteIpAddress;
        string ip = address is null || System.Net.IPAddress.IsLoopback(address) ? "127.0.0.1" : address.MapToIPv4().ToString();
        matchmaker.Enqueue(new QueuedPlayer(Context.ConnectionId, ticket, ip, DateTimeOffset.UtcNow));
        return Clients.Caller.SendAsync("Notice", $"Queued as {ticket.Name} ({ticket.Rating})");
    }

    public Task LeaveQueue()
    {
        matchmaker.Remove(Context.ConnectionId);
        return Task.CompletedTask;
    }

    /// <summary>Both players report; the result counts once the reports agree. Returns the caller's new rating.</summary>
    public async Task<int> ReportResult(string matchId, bool won)
    {
        var match = matchmaker.Find(matchId);
        var me = await store.GetOrCreatePlayerAsync(Context.ConnectionId == match?.HostConnection ? match.HostName : match?.GuestName ?? "?");
        if (match is null) return me.Rating;

        lock (match)
        {
            if (Context.ConnectionId == match.HostConnection) match.HostWon = won;
            else if (Context.ConnectionId == match.GuestConnection) match.GuestWon = won;
        }
        if (match.HostWon is not { } h || match.GuestWon is not { } g) return me.Rating;
        if (h == g) return me.Rating; // disputed result: ignored
        matchmaker.Complete(matchId);

        var winnerName = h ? match.HostName : match.GuestName;
        var loserName = h ? match.GuestName : match.HostName;
        var winner = await store.GetOrCreatePlayerAsync(winnerName);
        var loser = await store.GetOrCreatePlayerAsync(loserName);
        var (wr, lr) = Rating.Update(winner.Rating, loser.Rating);
        winner = winner with { Rating = wr, Wins = winner.Wins + 1 };
        loser = loser with { Rating = lr, Losses = loser.Losses + 1 };
        await store.SavePlayerAsync(winner);
        await store.SavePlayerAsync(loser);
        if (winner.Clan is { } clan && loser.Clan != clan) await store.AddClanPointsAsync(clan, 10);
        return me.Name == winner.Name ? wr : lr;
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> Leaderboard(int count) =>
        [.. (await store.TopPlayersAsync(Math.Clamp(count, 1, 100))).Select(p => new LeaderboardEntry(p.Name, p.Rating, p.Wins, p.Losses, p.Clan))];

    public async Task<ClanInfo> CreateClan(string name, string tag)
    {
        name = name.Trim();
        var clan = await store.GetClanAsync(name) ?? await store.CreateClanAsync(name, string.IsNullOrWhiteSpace(tag) ? name[..Math.Min(4, name.Length)].ToUpperInvariant() : tag);
        return new ClanInfo(clan.Name, clan.Tag, clan.Members, clan.WarPoints);
    }

    public async Task<ClanInfo> JoinClan(string name)
    {
        var clan = await store.GetClanAsync(name.Trim()) ?? throw new HubException("Clan not found.");
        var player = matchmaker.NameOf(Context.ConnectionId) is { } n ? await store.GetOrCreatePlayerAsync(n) : null;
        if (player is not null) await store.SavePlayerAsync(player with { Clan = clan.Name });
        clan = (await store.GetClanAsync(clan.Name))!;
        return new ClanInfo(clan.Name, clan.Tag, clan.Members, clan.WarPoints);
    }

    public async Task<IReadOnlyList<ClanInfo>> ClanStandings() =>
        [.. (await store.ClanStandingsAsync()).Select(c => new ClanInfo(c.Name, c.Tag, c.Members, c.WarPoints))];

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        matchmaker.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

public sealed record QueuedPlayer(string ConnectionId, PlayerTicket Ticket, string Address, DateTimeOffset Since);

/// <summary>
/// Pairs queued players. The acceptable rating gap widens the longer someone waits, so nobody is
/// stuck forever in a quiet queue.
/// </summary>
public sealed class Matchmaker(IHubContext<ArenaHub> hub, ILogger<Matchmaker> log) : BackgroundService
{
    private readonly List<QueuedPlayer> _queue = [];
    private readonly ConcurrentDictionary<string, PendingMatch> _matches = new();
    private readonly ConcurrentDictionary<string, string> _names = new();
    private readonly Lock _lock = new();

    public void Enqueue(QueuedPlayer p)
    {
        _names[p.ConnectionId] = p.Ticket.Name;
        lock (_lock)
        {
            _queue.RemoveAll(q => q.ConnectionId == p.ConnectionId);
            _queue.Add(p);
        }
    }

    public void Remove(string connectionId)
    {
        lock (_lock) _queue.RemoveAll(q => q.ConnectionId == connectionId);
    }

    public string? NameOf(string connectionId) => _names.TryGetValue(connectionId, out var n) ? n : null;

    internal PendingMatch? Find(string id) => _matches.TryGetValue(id, out var m) ? m : null;

    internal void Complete(string id) => _matches.TryRemove(id, out _);

    public int QueueLength
    {
        get { lock (_lock) return _queue.Count; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        while (!stoppingToken.IsCancellationRequested)
        {
            var pairs = new List<(QueuedPlayer A, QueuedPlayer B)>();
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                _queue.Sort((a, b) => a.Ticket.Rating.CompareTo(b.Ticket.Rating));
                for (int i = 0; i + 1 < _queue.Count; i++)
                {
                    var a = _queue[i];
                    var b = _queue[i + 1];
                    double waited = (now - (a.Since < b.Since ? a.Since : b.Since)).TotalSeconds;
                    int window = 100 + (int)(waited * 20);
                    if (Math.Abs(a.Ticket.Rating - b.Ticket.Rating) > window) continue;
                    pairs.Add((a, b));
                    _queue.RemoveAt(i + 1);
                    _queue.RemoveAt(i);
                    i--;
                }
            }

            foreach (var (host, guest) in pairs)
            {
                var id = Guid.NewGuid().ToString("N");
                uint seed = (uint)random.Next();
                var stage = Stages.All[random.Next(Stages.All.Count)].Id;
                _matches[id] = new PendingMatch { Id = id, HostConnection = host.ConnectionId, GuestConnection = guest.ConnectionId, HostName = host.Ticket.Name, GuestName = guest.Ticket.Name };
                await hub.Clients.Client(host.ConnectionId).SendAsync("MatchFound",
                    new MatchFound(id, true, guest.Address, guest.Ticket.UdpPort, seed, guest.Ticket.Name, guest.Ticket.Rating, stage, guest.Ticket.CharacterId), stoppingToken);
                await hub.Clients.Client(guest.ConnectionId).SendAsync("MatchFound",
                    new MatchFound(id, false, host.Address, host.Ticket.UdpPort, seed, host.Ticket.Name, host.Ticket.Rating, stage, host.Ticket.CharacterId), stoppingToken);
                log.LogInformation("Paired {Host} vs {Guest} on {Stage}", host.Ticket.Name, guest.Ticket.Name, stage);
            }
            await Task.Delay(500, stoppingToken);
        }
    }
}
