# Online play

![Online](../screenshots/17-online.png)

## How it works

Fights are **peer to peer** over UDP (LiteNetLib) with **rollback netcode** (`RollbackSession`):

- Each peer applies its own input with a 2-frame delay and sends the last 8 inputs every tick, so a
  lost packet is covered by the next one.
- The remote input is predicted (the last known input repeats). When the real input arrives and
  differs, the session restores the snapshot of that frame and re-simulates to the present. Snapshots
  are struct copies of `MatchState`, so a rollback costs no allocations.
- If a peer gets more than 8 frames ahead of the last confirmed input it waits (time sync).
- Every 60 confirmed frames the peers compare state checksums to detect desyncs.

The test suite runs two sessions over a loopback with 4 frames of latency and 20 % packet loss, and
two real UDP peers on localhost, and checks that both end in identical states.

## LAN / direct play (no server)

1. Player A: **Online → Host LAN match**, pick a fighter. The game listens on UDP 7777.
2. Player B: enter A's IP address, **Join LAN match**, pick a fighter.
3. The peers exchange a handshake (names, fighters; the host chooses arena and seed) and the match starts.

Open UDP 7777 in the host's firewall.

## Ranked, leaderboards and clans

`TarkamArena.Server` brokers ranked matches and stores ratings:

```bash
dotnet run --project src/TarkamArena.Server                     # SQLite: tarkam-server.db
ConnectionStrings__Postgres="Host=db;Database=tarkam;Username=tarkam;Password=secret" \
  dotnet run --project src/TarkamArena.Server                   # PostgreSQL
```

| Endpoint | Purpose |
|---|---|
| `/hubs/arena` (SignalR) | `JoinRankedQueue`, `LeaveQueue`, `ReportResult`, `Leaderboard`, `CreateClan`, `JoinClan`, `ClanStandings`; pushes `MatchFound` |
| `GET /health` | Status, queue length and database kind |
| `GET /api/leaderboard?count=20` | Top players |
| `GET /api/clans` | Clan standings |

The matchmaker pairs players whose ratings are within 100 points, widening the window by 20 points
per second of waiting. Both players report the result; it counts when the reports agree. Ratings use
Elo (K = 32) with tiers Kampung, Kecamatan, Kabupaten, Provinsi, Nasional and Legenda. A ranked win
against another clan earns your clan 10 war points.

In the game, set the server address under **Online** and press **Connect**.

Smoke test against a running server:

```bash
dotnet run --project tools/TarkamArena.Tools -- online-smoke http://localhost:5080
```

## Limits and next steps

- Players are identified by name; accounts and authentication are on the roadmap ([PLAN.md](../../PLAN.md)).
- Ranked peers connect directly, so one of them needs a reachable UDP port; NAT punch-through via
  LiteNetLib's `NatPunchModule` is planned.
- Online matches are two-player.
