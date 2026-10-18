using TarkamArena.Core.Combat;
using TarkamArena.Core.Match;
using TarkamArena.Networking;
using TarkamArena.Persistence;
using TarkamArena.Core.Progression;

namespace TarkamArena.Tests;

public class NetworkingTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 20)]
    public void Two_rollback_peers_stay_in_sync(int latency, int loss)
    {
        var context = TestMatch.Create();
        var (ta, tb) = LoopbackTransport.CreatePair(latency, loss);
        var a = new RollbackSession(context, ta, localSlot: 0);
        var b = new RollbackSession(context, tb, localSlot: 1);
        var rngA = new Random(11);
        var rngB = new Random(22);

        for (int tick = 0; tick < 1500; tick++)
        {
            // Hold each random input for a few frames, like a human would.
            var ia = new InputFrame((InputButtons)(tick / 5 % 7 == 0 ? rngA.Next(0, 4096) : 0x8));
            var ib = new InputFrame((InputButtons)(tick / 4 % 5 == 0 ? rngB.Next(0, 4096) : 0x4));
            a.Tick(ia);
            b.Tick(ib);
        }

        Assert.False(a.Stats.Desynced);
        Assert.False(b.Stats.Desynced);
        Assert.True(a.Frame > 1000, $"a stalled at {a.Frame}");
        Assert.True(b.Frame > 1000, $"b stalled at {b.Frame}");
        if (latency > 0) Assert.True(a.Stats.Rollbacks > 0);

        int compared = 0;
        int last = Math.Min(a.Stats.ConfirmedFrame, b.Stats.ConfirmedFrame);
        for (int f = last - 20; f <= last; f++)
        {
            uint ca = a.ConfirmedChecksum(f), cb = b.ConfirmedChecksum(f);
            if (ca == 0 || cb == 0) continue;
            Assert.Equal(ca, cb);
            compared++;
        }
        Assert.True(compared > 0);
    }

    [Fact]
    public void Udp_peers_handshake_and_play_in_sync()
    {
        using var host = LiteNetTransport.Host(0);
        using var guest = LiteNetTransport.Join("127.0.0.1", host.LocalPort);
        var hostHello = new HandshakeExchange(host, new HandshakeInfo("Host", "pesilat", 1000, "pasar-becek", 77), isHost: true);
        var guestHello = new HandshakeExchange(guest, new HandshakeInfo("Guest", "pocong", 1000), isHost: false);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while ((!hostHello.Poll() | !guestHello.Poll()) && DateTime.UtcNow < deadline) Thread.Sleep(5);
        Assert.Equal("pocong", hostHello.Remote?.CharacterId);
        Assert.Equal("pasar-becek", guestHello.Remote?.StageId);

        var context = TestMatch.Create(GameMode.Versus, "pesilat", "pocong");
        var a = new RollbackSession(context, host, 0);
        var b = new RollbackSession(context, guest, 1);
        var rng = new Random(5);
        deadline = DateTime.UtcNow.AddSeconds(20);
        while ((a.Frame < 400 || b.Frame < 400) && DateTime.UtcNow < deadline)
        {
            a.Tick(new InputFrame((InputButtons)(rng.Next(8) == 0 ? rng.Next(4096) : 0)));
            b.Tick(new InputFrame((InputButtons)(rng.Next(8) == 0 ? rng.Next(4096) : 0)));
            Thread.Sleep(1);
        }
        Assert.True(a.Frame >= 400 && b.Frame >= 400, $"frames {a.Frame}/{b.Frame}");
        Assert.False(a.Stats.Desynced || b.Stats.Desynced);
    }

    [Fact]
    public void Arena_physics_props_fall_and_scatter()
    {
        using var physics = new TarkamArena.Physics.ArenaPhysics(7.5f, multithreaded: false);
        int crate = physics.AddProp(TarkamArena.Physics.PropShape.Box, new System.Numerics.Vector3(0.5f), new System.Numerics.Vector3(1, 2, -1), 2, 0x8C6A45);
        for (int i = 0; i < 120; i++) physics.Update(1 / 60f);
        Assert.InRange(physics.Props[crate].Position.Y, 0.2f, 0.4f);
        physics.Explode(new System.Numerics.Vector3(1, 0, -1), 3, 6);
        physics.SpawnDebris(System.Numerics.Vector3.UnitY, 10, 0xFFFFFF, 0.1f, new Random(1));
        for (int i = 0; i < 30; i++) physics.Update(1 / 60f);
        Assert.True(physics.AliveCount >= 2);
    }

    [Fact]
    public void Database_round_trips_profile_and_history()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tarkam-{Guid.NewGuid():N}.db");
        try
        {
            using (var db = new GameDatabase(path))
            {
                var profile = db.LoadProfile();
                profile.Coins = 1234;
                profile.Achievements.Add("first-win");
                profile.Finishers[FinisherKind.Fatality] = 3;
                db.SaveProfile(profile);
                db.RecordMatch(new MatchRecord(DateTimeOffset.UtcNow, "Versus", "pesilat", "pocong", true, 7, "Fatality"));
                var settings = db.LoadSettings();
                settings.MusicVolume = 0.25f;
                db.SaveSettings(settings);
            }
            using (var db = new GameDatabase(path))
            {
                var profile = db.LoadProfile();
                Assert.Equal(1234, profile.Coins);
                Assert.Contains("first-win", profile.Achievements);
                Assert.Equal(3, profile.FinisherCount(FinisherKind.Fatality));
                Assert.True(profile.HasCharacter("pesilat"));
                Assert.Single(db.RecentMatches());
                Assert.Equal((1, 1), db.CharacterStats()["pesilat"]);
                Assert.Equal(0.25f, db.LoadSettings().MusicVolume);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(path) + "*")) File.Delete(f);
        }
    }
}
