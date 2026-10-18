using Microsoft.AspNetCore.SignalR.Client;

namespace TarkamArena.Networking;

/// <summary>Sent by the server when two players have been paired for a ranked match.</summary>
public sealed record MatchFound(string MatchId, bool IsHost, string PeerAddress, int PeerPort, uint Seed, string OpponentName, int OpponentRating, string StageId, string OpponentCharacterId);

public sealed record LeaderboardEntry(string Name, int Rating, int Wins, int Losses, string? Clan);

public sealed record ClanInfo(string Name, string Tag, int Members, int WarPoints);

public sealed record PlayerTicket(string Name, int Rating, string CharacterId, int UdpPort);

/// <summary>
/// Client for TarkamArena.Server: ranked matchmaking, leaderboards, clans and clan wars over
/// SignalR. The match itself runs peer to peer on <see cref="LiteNetTransport"/>.
/// </summary>
public sealed class OnlineServices : IAsyncDisposable
{
    private readonly HubConnection _hub;

    public event Action<MatchFound>? MatchFound;
    public event Action<string>? Notice;

    public OnlineServices(string serverUrl)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(serverUrl), "/hubs/arena"))
            .WithAutomaticReconnect()
            .Build();
        _hub.On<MatchFound>("MatchFound", m => MatchFound?.Invoke(m));
        _hub.On<string>("Notice", n => Notice?.Invoke(n));
    }

    public bool IsConnected => _hub.State == HubConnectionState.Connected;

    public Task ConnectAsync(CancellationToken ct = default) => _hub.StartAsync(ct);

    public Task JoinRankedQueueAsync(PlayerTicket ticket) => _hub.InvokeAsync("JoinRankedQueue", ticket);

    public Task LeaveQueueAsync() => _hub.InvokeAsync("LeaveQueue");

    /// <summary>Reports a finished match; the server updates both ratings and clan war points.</summary>
    public Task<int> ReportResultAsync(string matchId, bool won) => _hub.InvokeAsync<int>("ReportResult", matchId, won);

    public Task<IReadOnlyList<LeaderboardEntry>> LeaderboardAsync(int count = 20) =>
        _hub.InvokeAsync<IReadOnlyList<LeaderboardEntry>>("Leaderboard", count);

    public Task<ClanInfo> CreateClanAsync(string name, string tag) => _hub.InvokeAsync<ClanInfo>("CreateClan", name, tag);

    public Task<ClanInfo> JoinClanAsync(string name) => _hub.InvokeAsync<ClanInfo>("JoinClan", name);

    public Task<IReadOnlyList<ClanInfo>> ClanStandingsAsync() => _hub.InvokeAsync<IReadOnlyList<ClanInfo>>("ClanStandings");

    public ValueTask DisposeAsync() => _hub.DisposeAsync();
}
