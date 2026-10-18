using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Progression;
using TarkamArena.Networking;

namespace TarkamArena.App.Screens;

/// <summary>
/// Online play. Ranked matchmaking, leaderboards and clans go through TarkamArena.Server
/// (SignalR); the match itself is peer to peer over UDP with rollback netcode. Direct LAN play
/// needs no server at all.
/// </summary>
public sealed class OnlineScreen : Screen
{
    private static OnlineServices? s_services;

    private readonly TextBox _server = new() { Width = 420 };
    private readonly TextBox _address = new() { Width = 260, Text = "127.0.0.1" };
    private readonly TextBox _clan = new() { Width = 260 };
    private readonly TextBlock _status = Ui.Wrap("", "body", "muted").With(t => t.MaxWidth = 560);
    private readonly StackPanel _board = new() { Spacing = 4 };
    private DispatcherTimer? _poll;

    public OnlineScreen()
    {
        _server.Text = Game.Settings.ServerUrl;
        _clan.Text = Game.Profile.ClanName ?? "";
        _status.Text = s_services?.IsConnected == true ? Loc.T("online.rating", Game.Profile.Rating) : Loc.T("online.disconnected");

        var connect = Ui.Banner(Loc.T("online.connect").ToUpperInvariant(), () => _ = ConnectAsync(), small: true);
        var ranked = Ui.Banner(Loc.T("online.ranked").ToUpperInvariant(), () => PickThen(c => _ = RankedAsync(c)));
        var host = Ui.Banner(Loc.T("online.hostLan").ToUpperInvariant(), () => PickThen(c => StartDirect(c, null)));
        var join = Ui.Banner(Loc.T("online.joinLan").ToUpperInvariant(), () => PickThen(c => StartDirect(c, _address.Text)));
        var createClan = Ui.Chip(Loc.T("online.createClan"), () => _ = ClanAsync(create: true));
        var joinClan = Ui.Chip(Loc.T("online.joinClan"), () => _ = ClanAsync(create: false));
        var refresh = Ui.Chip(Loc.T("online.leaderboard"), () => _ = LeaderboardAsync());
        Nav(connect, ranked, host, join, createClan, joinClan, refresh);

        var left = Ui.V(16,
            new SignText(Loc.T("online.title").ToUpperInvariant(), 56),
            Ui.Text(Loc.T("online.server").ToUpperInvariant(), "eyebrow"), Ui.H(10, _server, connect),
            ranked,
            Ui.Text("LAN", "eyebrow"), Ui.H(10, Ui.Text("IP", "h3"), _address), Ui.H(10, host, join),
            Ui.Text(Loc.T("online.clan").ToUpperInvariant(), "eyebrow"), Ui.H(10, _clan, createClan, joinClan),
            _status);
        var right = Ui.Panel(Ui.V(10, Ui.H(10, Ui.Text(Loc.T("online.leaderboard").ToUpperInvariant(), "eyebrow"), refresh), _board), 24);
        right.Width = 520;
        right.VerticalAlignment = VerticalAlignment.Top;
        var root = Ui.Columns("*,Auto", left, right);
        root.Margin = new Thickness(80, 50, 80, 50);
        Content = root;
    }

    private void PickThen(Action<CharacterDefinition> next) =>
        Window.Show(new CharacterSelectScreen(MatchPlan.For(GameMode.OnlineRanked), c => { Window.Back(); next(c); }));

    private async Task<OnlineServices?> EnsureServicesAsync()
    {
        if (s_services?.IsConnected == true) return s_services;
        Game.Settings.ServerUrl = _server.Text ?? Game.Settings.ServerUrl;
        Game.Save();
        try
        {
            if (s_services is not null) await s_services.DisposeAsync();
            s_services = new OnlineServices(Game.Settings.ServerUrl);
            s_services.Notice += n => Dispatcher.UIThread.Post(() => _status.Text = n);
            await s_services.ConnectAsync();
            _status.Text = Loc.T("online.rating", Game.Profile.Rating) + " · " + Rating.Tier(Game.Profile.Rating);
            return s_services;
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException or TaskCanceledException or UriFormatException)
        {
            _status.Text = $"{Loc.T("online.disconnected")} ({e.Message})";
            return null;
        }
    }

    private async Task ConnectAsync()
    {
        if (await EnsureServicesAsync() is not null) await LeaderboardAsync();
    }

    private async Task LeaderboardAsync()
    {
        if (await EnsureServicesAsync() is not { } services) return;
        var rows = await services.LeaderboardAsync(15);
        _board.Children.Clear();
        int rank = 1;
        foreach (var r in rows)
            _board.Children.Add(Ui.Columns("40,*,90,Auto",
                Ui.Text($"{rank++}", "mono"), Ui.Text($"{r.Name}{(r.Clan is null ? "" : $" [{r.Clan}]")}", "h3"),
                Ui.Text($"{r.Rating}", "mono"), Ui.Text(Rating.Tier(r.Rating).ToString(), "mono", "muted")));
    }

    private async Task ClanAsync(bool create)
    {
        if (await EnsureServicesAsync() is not { } services || string.IsNullOrWhiteSpace(_clan.Text)) return;
        var name = _clan.Text.Trim();
        var clan = create ? await services.CreateClanAsync(name, new string([.. name.Where(char.IsLetter).Take(4)]).ToUpperInvariant()) : await services.JoinClanAsync(name);
        Game.Profile.ClanName = clan.Name;
        Game.Save();
        _status.Text = $"{clan.Name} [{clan.Tag}] · {clan.Members} · {clan.WarPoints} pts";
    }

    private async Task RankedAsync(CharacterDefinition character)
    {
        if (await EnsureServicesAsync() is not { } services) return;
        _status.Text = Loc.T("online.searching");
        var transport = LiteNetTransport.Host(0);
        void OnFound(MatchFound m) => Dispatcher.UIThread.Post(() =>
        {
            services.MatchFound -= OnFound;
            LiteNetTransport net = transport;
            if (!m.IsHost)
            {
                transport.Dispose();
                net = LiteNetTransport.Join(m.PeerAddress, m.PeerPort);
            }
            var opponent = Roster.TryGet(m.OpponentCharacterId, out var oc) ? oc : Roster.Get("pesilat");
            WaitAndStart(net, m.IsHost, character, opponent, Stages.Get(m.StageId), m.Seed, m.MatchId, ranked: true);
        });
        services.MatchFound += OnFound;
        await services.JoinRankedQueueAsync(new PlayerTicket(Game.Profile.Name, Game.Profile.Rating, character.Id, transport.LocalPort));
    }

    private void StartDirect(CharacterDefinition character, string? address)
    {
        bool isHost = address is null;
        LiteNetTransport transport;
        try
        {
            transport = isHost ? LiteNetTransport.Host() : LiteNetTransport.Join(address!);
        }
        catch (InvalidOperationException e)
        {
            _status.Text = e.Message;
            return;
        }
        var stage = Stages.All[Random.Shared.Next(Stages.All.Count)];
        var local = new HandshakeInfo(Game.Profile.Name, character.Id, Game.Profile.Rating, isHost ? stage.Id : null, isHost ? (uint)Environment.TickCount : 0);
        var exchange = new HandshakeExchange(transport, local, isHost);
        _status.Text = isHost ? $"{Loc.T("online.searching")} UDP {LiteNetTransport.DefaultPort}" : Loc.T("online.searching");
        _poll?.Stop();
        _poll = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, (_, _) =>
        {
            if (!exchange.Poll()) return;
            _poll!.Stop();
            var remote = exchange.Remote!;
            var opponent = Roster.TryGet(remote.CharacterId, out var oc) ? oc : Roster.Get("pesilat");
            var stageId = isHost ? local.StageId! : remote.StageId ?? stage.Id;
            var seed = isHost ? local.Seed : remote.Seed;
            Launch(transport, isHost, character, opponent, Stages.Get(stageId), seed, null);
        });
        _poll.Start();
    }

    private void WaitAndStart(LiteNetTransport transport, bool isHost, CharacterDefinition me, CharacterDefinition opponent, StageDefinition stage, uint seed, string matchId, bool ranked)
    {
        _poll?.Stop();
        _poll = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, (_, _) =>
        {
            transport.Poll(_ => { });
            if (!transport.IsConnected) return;
            _poll!.Stop();
            Launch(transport, isHost, me, opponent, stage, seed, ranked ? matchId : null);
        });
        _poll.Start();
    }

    private void Launch(LiteNetTransport transport, bool isHost, CharacterDefinition me, CharacterDefinition opponent, StageDefinition stage, uint seed, string? matchId)
    {
        // Both peers build the same setup: the host is always slot 0.
        var plan = MatchPlan.For(GameMode.OnlineRanked);
        plan.Controllers = [isHost ? SlotController.Player1 : SlotController.Remote, isHost ? SlotController.Remote : SlotController.Player1];
        plan.Fighters = isHost ? [me, opponent] : [opponent, me];
        plan.Stage = stage;
        plan.Seed = seed;
        plan.OpponentsFixed = plan.StageFixed = true;
        var context = new MatchContext(plan.ToSetup());
        plan.Online = new RollbackSession(context, transport, isHost ? 0 : 1);
        plan.OnlineTransport = transport;
        plan.OnlineMatchId = matchId;
        plan.OnFinished = (summary, reward) =>
        {
            if (matchId is not null && s_services is not null)
                _ = s_services.ReportResultAsync(matchId, summary.Won).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully) Dispatcher.UIThread.Post(() => { Game.Profile.Rating = t.Result; Game.Save(); });
                });
            return false;
        };
        Window.Show(new FightScreen(plan));
    }

    public override void OnLeave() => _poll?.Stop();
}
