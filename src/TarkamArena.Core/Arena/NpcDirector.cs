using System.Numerics;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;

namespace TarkamArena.Core.Arena;

/// <summary>How a background NPC feels about a specific fighter.</summary>
public enum NpcReaction : byte
{
    None,
    Cheer,
    Fear,
    Selfie,
    Scold,
}

/// <summary>One background character. Plain data, stored in a flat array the renderer reads directly.</summary>
public struct NpcAgent
{
    public NpcKind Kind;
    public NpcActivity Activity;
    public Vector3 Position;
    public Vector3 Home;
    public Vector3 Destination;
    public float Speed;
    public float Timer;
    public float Yaw;
    public float Bounce;
    public float Excitement;
    public NpcReaction Reaction;
    public int Seed;
}

/// <summary>
/// Living Arena system: drives the stage's NPCs (walking, trading, chatting, cheering, filming,
/// fleeing finishers and reacting to fighters they recognise). This is presentation only, so it
/// uses floats and does not need to be deterministic; it never feeds back into the fight.
/// </summary>
public sealed class NpcDirector
{
    private readonly NpcAgent[] _agents;
    private readonly StageDefinition _stage;
    private readonly Random _random;
    private float _fleeTimer;
    private Vector3 _fleeOrigin;
    private float _time;

    public NpcDirector(StageDefinition stage, int count, int seed = 1)
    {
        _stage = stage;
        _random = new Random(seed);
        _agents = new NpcAgent[Math.Max(0, count)];
        for (int i = 0; i < _agents.Length; i++)
        {
            var kind = stage.Npcs[i % stage.Npcs.Count];
            var home = RandomSpot();
            _agents[i] = new NpcAgent
            {
                Kind = kind,
                Position = home,
                Home = home,
                Destination = home,
                Activity = InitialActivity(kind),
                Speed = 0.8f + (float)_random.NextDouble() * 0.9f,
                Timer = (float)_random.NextDouble() * 4f,
                Yaw = (float)(_random.NextDouble() * Math.PI * 2),
                Seed = _random.Next(),
            };
        }
    }

    public ReadOnlySpan<NpcAgent> Agents => _agents;

    /// <summary>NPCs stand in a band behind the fighting line and around the sides.</summary>
    private Vector3 RandomSpot()
    {
        float halfWidth = _stage.HalfWidth + 3f;
        float x = ((float)_random.NextDouble() * 2f - 1f) * halfWidth;
        // Behind the fighting line only, so nobody stands between the camera and the fight.
        float z = -3.2f - (float)_random.NextDouble() * 4.5f;
        return new Vector3(x, 0, z);
    }

    private static NpcActivity InitialActivity(NpcKind kind) => kind switch
    {
        NpcKind.Trader or NpcKind.VegetableSeller or NpcKind.Hawker => NpcActivity.Trade,
        NpcKind.Journalist or NpcKind.Photographer => NpcActivity.Record,
        NpcKind.Chicken or NpcKind.Goat or NpcKind.Buffalo or NpcKind.Porter or NpcKind.Commuter or NpcKind.Passenger => NpcActivity.Walk,
        NpcKind.Musician => NpcActivity.Cheer,
        _ => NpcActivity.Chat,
    };

    /// <summary>Who in the crowd recognises which fighter, and how they react.</summary>
    public static NpcReaction Recognise(NpcKind kind, string fighterId) => (kind, fighterId) switch
    {
        (NpcKind.Trader or NpcKind.VegetableSeller, "preman-pasar") => NpcReaction.Fear,
        (NpcKind.Trader or NpcKind.Shopper, "abang-bakso") => NpcReaction.Cheer,
        (NpcKind.Shopper or NpcKind.Kid, "emak-emak-komplek") => NpcReaction.Scold,
        (NpcKind.Driver or NpcKind.Conductor, "tukang-parkir") => NpcReaction.Cheer,
        (NpcKind.Commuter or NpcKind.Passenger, "kasir-minimarket") => NpcReaction.Selfie,
        (NpcKind.Kid, "guru-matematika") => NpcReaction.Fear,
        (NpcKind.Ranger or NpcKind.Researcher, "orang-utan") => NpcReaction.Cheer,
        (NpcKind.OrangUtan, "orang-utan") => NpcReaction.Cheer,
        (NpcKind.Tourist or NpcKind.Photographer, "komodo" or "harimau-sumatra" or "cenderawasih") => NpcReaction.Selfie,
        (NpcKind.Fisherman, "nyi-roro-kidul") => NpcReaction.Fear,
        (NpcKind.Monk, "gatotkaca" or "jaka-tarub") => NpcReaction.Cheer,
        (NpcKind.Politician, "si-pitung" or "mahasiswi-demo") => NpcReaction.Fear,
        (NpcKind.Student, "mahasiswi-demo" or "anak-stm") => NpcReaction.Cheer,
        (NpcKind.Student or NpcKind.Judge, "pejabat-korup" or "anggota-dpr") => NpcReaction.Scold,
        (NpcKind.Journalist or NpcKind.Photographer, "pejabat-korup") => NpcReaction.Selfie,
        (NpcKind.OjolDriver, "kang-ojol") => NpcReaction.Cheer,
        (NpcKind.Dancer or NpcKind.SoundCrew or NpcKind.Kid, "horeg-dancer") => NpcReaction.Cheer,
        (NpcKind.Firefighter or NpcKind.Villager, "pemadam-karhutla") => NpcReaction.Cheer,
        (NpcKind.Warok or NpcKind.Musician, "reog-ponorogo") => NpcReaction.Cheer,
        (NpcKind.Pecalang, "ogoh-ogoh") => NpcReaction.Cheer,
        (NpcKind.Tourist or NpcKind.Kid, "influencer") => NpcReaction.Selfie,
        (NpcKind.Officer, "anak-punk" or "anak-stm") => NpcReaction.Scold,
        (_, "kuntilanak" or "pocong" or "genderuwo" or "wewe-gombel" or "begu-ganjang" or "leak-bali" or "kuyang" or "baphomet" or "dukun-santet")
            when kind is not (NpcKind.Monk or NpcKind.OrangUtan or NpcKind.Pecalang) => NpcReaction.Fear,
        (NpcKind.Journalist or NpcKind.Photographer, _) => NpcReaction.Selfie,
        _ => NpcReaction.None,
    };

    /// <summary>
    /// Advances every NPC. <paramref name="crowd01"/> is the crowd excitement meter in 0..1,
    /// <paramref name="fighters"/> the on-field fighter positions and ids.
    /// </summary>
    public void Update(float dt, float crowd01, ReadOnlySpan<(Vector3 Position, string Id)> fighters, ReadOnlySpan<MatchEvent> events)
    {
        foreach (ref readonly var e in events)
        {
            if (e.Kind is MatchEventKind.FinisherStart or MatchEventKind.HazardHit)
            {
                _fleeTimer = e.Kind == MatchEventKind.FinisherStart ? 4.5f : 1.5f;
                _fleeOrigin = new Vector3(e.Position.X.ToFloat(), 0, 0);
            }
            else if (e.Kind is MatchEventKind.KO or MatchEventKind.Combo or MatchEventKind.TeamCombo)
            {
                for (int i = 0; i < _agents.Length; i++) _agents[i].Excitement = 1f;
            }
        }
        if (_fleeTimer > 0) _fleeTimer -= dt;
        _time += dt;

        for (int i = 0; i < _agents.Length; i++)
        {
            ref var a = ref _agents[i];
            a.Timer -= dt;
            a.Excitement = MathF.Max(0, a.Excitement - dt * 0.4f);

            bool animal = a.Kind is NpcKind.Chicken or NpcKind.Goat or NpcKind.Buffalo or NpcKind.OrangUtan;

            if (_fleeTimer > 0 && Vector3.DistanceSquared(a.Position, _fleeOrigin) < 60f && !animal)
            {
                a.Activity = NpcActivity.Flee;
                var away = a.Position - _fleeOrigin;
                away.Y = 0;
                if (away.LengthSquared() < 0.01f) away = new Vector3(1, 0, -1);
                a.Destination = a.Position + Vector3.Normalize(away) * 4f;
                a.Destination.Z = Math.Clamp(a.Destination.Z, -9f, -2.8f);
            }
            else if (a.Activity == NpcActivity.Flee && _fleeTimer <= 0)
            {
                a.Activity = NpcActivity.Walk;
                a.Destination = a.Home;
            }
            else if (a.Timer <= 0)
            {
                Decide(ref a, crowd01, fighters);
            }

            float speed = a.Activity switch
            {
                NpcActivity.Flee => a.Speed * 3.2f,
                NpcActivity.Walk => a.Speed,
                _ => 0f,
            };
            var delta = a.Destination - a.Position;
            delta.Y = 0;
            float dist = delta.Length();
            if (speed > 0 && dist > 0.05f)
            {
                var step = delta / dist * MathF.Min(dist, speed * dt);
                a.Position += step;
                a.Yaw = MathF.Atan2(step.X, step.Z);
            }
            else if (a.Activity is NpcActivity.Cheer or NpcActivity.Record or NpcActivity.React)
            {
                // Face the fight.
                a.Yaw = MathF.Atan2(-a.Position.X * 0.2f, -a.Position.Z);
            }

            float bounceRate = a.Activity switch
            {
                NpcActivity.Cheer => 9f + a.Excitement * 6f,
                NpcActivity.React => 12f,
                NpcActivity.Flee => 14f,
                NpcActivity.Walk => 7f,
                _ => 0f,
            };
            a.Bounce = bounceRate > 0 ? MathF.Abs(MathF.Sin(_time * bounceRate + (a.Seed & 1023))) * (a.Activity == NpcActivity.Cheer ? 0.18f : 0.06f) : 0f;
        }
    }

    private void Decide(ref NpcAgent a, float crowd01, ReadOnlySpan<(Vector3 Position, string Id)> fighters)
    {
        a.Timer = 2f + (float)_random.NextDouble() * 4f;

        // Celebrity recognition: a fighter the NPC knows is close by.
        foreach (var (position, id) in fighters)
        {
            var reaction = Recognise(a.Kind, id);
            if (reaction == NpcReaction.None || Vector3.DistanceSquared(position, a.Position) > 36f) continue;
            a.Reaction = reaction;
            a.Activity = reaction switch
            {
                NpcReaction.Fear => NpcActivity.Flee,
                NpcReaction.Selfie => NpcActivity.Record,
                _ => NpcActivity.React,
            };
            if (a.Activity == NpcActivity.Flee)
            {
                a.Destination = a.Home + new Vector3(0, 0, -2f);
                a.Timer = 2.5f;
            }
            return;
        }
        a.Reaction = NpcReaction.None;

        bool animal = a.Kind is NpcKind.Chicken or NpcKind.Goat or NpcKind.Buffalo;
        double roll = _random.NextDouble();
        if (animal)
        {
            a.Activity = roll < 0.6 ? NpcActivity.Walk : NpcActivity.Idle;
            a.Destination = RandomSpot();
            return;
        }

        // The louder the crowd, the more people stop to cheer and film.
        double cheer = 0.15 + crowd01 * 0.55;
        if (roll < cheer) a.Activity = NpcActivity.Cheer;
        else if (roll < cheer + 0.1 + crowd01 * 0.15) a.Activity = NpcActivity.Record;
        else if (InitialActivity(a.Kind) == NpcActivity.Trade) a.Activity = NpcActivity.Trade;
        else if (roll < 0.8)
        {
            a.Activity = NpcActivity.Walk;
            a.Destination = RandomSpot();
        }
        else a.Activity = NpcActivity.Chat;
    }
}
