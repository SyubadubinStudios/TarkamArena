using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Match;

/// <summary>
/// Everything about one fighter that changes during a match. It is an unmanaged value type so the
/// whole match can be snapshotted with a struct copy (no allocations, no GC) for rollback.
/// </summary>
public struct FighterState
{
    public bool Present;
    public bool OnField;
    public byte Slot;
    public byte Team;

    public FixedVector2 Position;
    public FixedVector2 Velocity;
    public sbyte Facing;
    public bool Grounded;

    public int Health;
    public int MaxHealth;
    public int Rage;
    public int Adrenaline;
    public int RageTimer;
    public int AdrenalineTimer;

    public FighterStatus Status;
    public int StatusFrame;
    public int StunFrames;
    public MoveId Move;
    public bool MoveConnected;
    public bool Blocking;
    public bool LowBlocking;

    public int Hitstop;
    public int Invulnerable;
    public int BlockPressedFrame;
    public int PoisonFrames;

    public int ComboHits;
    public int ComboDamage;
    public int JuggleCount;
    public sbyte LastAttacker;
    public sbyte Target;

    public ushort Held;
    public ushort Pressed;
    public InputHistory History;
    public byte HistoryHead;
    public TokenHistory Tokens;
    public byte TokenHead;
    public byte TokenCount;

    public readonly bool IsAlive => Health > 0;
    public readonly bool IsActive => Present && OnField;
    public readonly bool RageActive => RageTimer > 0;
    public readonly bool AdrenalineActive => AdrenalineTimer > 0;
    public readonly bool IsAirborne => !Grounded;

    public readonly bool IsHeld(InputButtons b) => (Held & (ushort)b) != 0;
    public readonly bool WasPressed(InputButtons b) => (Pressed & (ushort)b) != 0;

    /// <summary>Raw input from <paramref name="framesAgo"/> frames ago (0 = this frame).</summary>
    public readonly InputButtons HistoryAt(int framesAgo)
    {
        int index = (HistoryHead - framesAgo + InputHistory.Length * 4) % InputHistory.Length;
        return (InputButtons)History[index];
    }

    /// <summary>The n-th most recent pressed token (0 = latest).</summary>
    public readonly InputToken TokenAt(int back) => (InputToken)Tokens[(TokenHead - back + TokenHistory.Length * 4) % TokenHistory.Length];
}

[InlineArray(MatchState.MaxFighters)]
public struct FighterArray
{
    private FighterState _element;
}

[InlineArray(MatchState.MaxProjectiles)]
public struct ProjectileArray
{
    private Projectile _element;
}

[InlineArray(MatchState.MaxEvents)]
public struct EventArray
{
    private MatchEvent _element;
}

[InlineArray(MatchState.MaxFighters)]
public struct TeamIntArray
{
    private int _element;
}

/// <summary>
/// The complete, deterministic state of a match. <see cref="MatchSimulation.Step"/> advances it one
/// 60 Hz frame. Copy the struct to save a rollback snapshot; assign it back to restore.
/// </summary>
public struct MatchState
{
    public const int MaxFighters = 4;
    public const int MaxProjectiles = 8;
    public const int MaxEvents = 24;
    public const int FramesPerSecond = 60;

    public int Frame;
    public MatchPhase Phase;
    public int PhaseFrame;
    public int Round;
    public int TimerFrames;
    public uint Rng;

    public int FighterCount;
    public FighterArray Fighters;
    public ProjectileArray Projectiles;
    public Hazard Hazard;
    public int NextArenaEventFrame;
    public sbyte LastArenaEvent;

    public TeamIntArray RoundWins;
    public int Crowd;

    public sbyte RoundWinnerTeam;
    public sbyte MatchWinnerTeam;
    public sbyte FinisherExecutor;
    public sbyte FinisherVictim;
    public sbyte FinisherPerformed;
    public bool MercyUsed;

    public int EventCount;
    public EventArray Events;

    public readonly int TimerSeconds => (TimerFrames + FramesPerSecond - 1) / FramesPerSecond;

    public readonly bool IsOver => Phase == MatchPhase.MatchOver;

    public readonly ReadOnlySpan<MatchEvent> CurrentEvents =>
        MemoryMarshal.CreateReadOnlySpan(in Events[0], EventCount);

    /// <summary>Stable checksum of the state, used to detect desyncs between online peers.</summary>
    public readonly uint Checksum()
    {
        var copy = this;
        copy.EventCount = 0;
        copy.Events = default;
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref copy, 1));
        uint hash = 2166136261;
        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }
}
