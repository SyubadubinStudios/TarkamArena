using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.AI;

public enum CpuDifficulty : byte
{
    Easy,
    Normal,
    Hard,
    Legend,
}

/// <summary>
/// A CPU opponent that reads the match state and produces controller input, exactly like a human
/// would. It never touches the simulation directly. Allocation-free per frame.
/// </summary>
public sealed class CpuBrain
{
    private readonly int _slot;
    private readonly CpuDifficulty _difficulty;
    private uint _rng;

    private InputButtons _plan;
    private int _planFrames;
    private int _reactionTimer;
    private int _sequenceStep = -1;
    private InputToken[]? _sequence;
    private int _sequenceTimer;

    public CpuBrain(int slot, CpuDifficulty difficulty, uint seed = 7)
    {
        _slot = slot;
        _difficulty = difficulty;
        _rng = DeterministicRandom.Seed(seed * 2654435761u + (uint)slot);
    }

    public CpuDifficulty Difficulty => _difficulty;

    public static LocalizedText Name(CpuDifficulty difficulty) => difficulty switch
    {
        CpuDifficulty.Easy => new("Easy", "Santai"),
        CpuDifficulty.Normal => new("Normal", "Normal"),
        CpuDifficulty.Hard => new("Hard", "Sulit"),
        _ => new("Legend", "Legenda"),
    };

    private int ReactionFrames => _difficulty switch
    {
        CpuDifficulty.Easy => 26,
        CpuDifficulty.Normal => 16,
        CpuDifficulty.Hard => 9,
        _ => 5,
    };

    private int Aggression => _difficulty switch
    {
        CpuDifficulty.Easy => 25,
        CpuDifficulty.Normal => 45,
        CpuDifficulty.Hard => 60,
        _ => 70,
    };

    private int DefenseSkill => _difficulty switch
    {
        CpuDifficulty.Easy => 10,
        CpuDifficulty.Normal => 35,
        CpuDifficulty.Hard => 60,
        _ => 80,
    };

    public InputFrame Think(in MatchState state, MatchContext context)
    {
        ref readonly var me = ref state.Fighters[_slot];
        if (!me.IsActive) return InputFrame.Neutral;

        if (state.Phase == MatchPhase.FinisherWindow)
            return state.FinisherExecutor == _slot ? DoFinisher(in state, context, in me) : InputFrame.Neutral;

        if (state.Phase != MatchPhase.Fight || !me.IsAlive || me.Target < 0) return InputFrame.Neutral;

        ref readonly var foe = ref state.Fighters[me.Target];
        Fixed distance = Fixed.Abs(foe.Position.X - me.Position.X);
        InputButtons toward = foe.Position.X > me.Position.X ? InputButtons.Right : InputButtons.Left;
        InputButtons away = toward == InputButtons.Right ? InputButtons.Left : InputButtons.Right;

        // Keep pressing the combo while our hits are landing.
        if (me.Status == FighterStatus.Attack && me.MoveConnected)
        {
            if (me.Move == MoveId.Launcher && Roll(70)) return new(InputButtons.Up);
            if (me.Move is MoveId.Jab or MoveId.Cross) return Pulse(InputButtons.Light, state.Frame);
            if (me.Move == MoveId.Hook && Roll(40)) return new(InputButtons.Special | toward);
            return InputFrame.Neutral;
        }

        // Air combo after a launcher.
        if (!me.Grounded && foe.Status == FighterStatus.Juggle && distance < Fixed.FromFloat(1.6f))
            return Pulse(state.Frame % 24 < 12 ? InputButtons.Light : InputButtons.Heavy, state.Frame);

        // Defend against an incoming attack.
        if (foe.Status == FighterStatus.Attack && distance < Fixed.FromFloat(2.4f) && foe.StatusFrame <= 4)
        {
            if (_reactionTimer <= 0) _reactionTimer = ReactionFrames;
        }
        if (_reactionTimer > 0 && --_reactionTimer == 0 && Roll(DefenseSkill))
        {
            var move = context.Move(foe.Slot, foe.Move);
            if (Roll(_difficulty >= CpuDifficulty.Hard ? 35 : 10)) return new(InputButtons.Block);
            if (move.Has(MoveFlags.Unblockable)) return new(InputButtons.Dodge | away);
            SetPlan(InputButtons.Block | (move.Has(MoveFlags.Low) ? InputButtons.Down : InputButtons.None), 18);
        }

        // Rage when it is full and we are losing.
        if (me.Rage >= MatchSimulation.MeterMax && me.Health * 2 < me.MaxHealth && Roll(30)) return new(InputButtons.Rage);

        // Anti-air an incoming jump.
        if (!foe.Grounded && foe.Status == FighterStatus.Airborne && distance < Fixed.FromFloat(2.0f) && Roll(DefenseSkill / 2))
            return new(InputButtons.Special | InputButtons.Down);

        if (_planFrames > 0)
        {
            _planFrames--;
            return new(_plan);
        }

        if (me.Status is not (FighterStatus.Idle or FighterStatus.Walk or FighterStatus.Crouch)) return InputFrame.Neutral;

        // Pick a new short-term plan.
        Fixed close = Fixed.FromFloat(1.25f);
        Fixed mid = Fixed.FromFloat(3.5f);
        int roll = DeterministicRandom.Range(ref _rng, 100);

        if (distance <= close)
        {
            if (roll < Aggression)
            {
                int pick = DeterministicRandom.Range(ref _rng, 6);
                return pick switch
                {
                    0 => new(InputButtons.Grab),
                    1 => new(InputButtons.Heavy | InputButtons.Down),
                    2 => new(InputButtons.Heavy),
                    3 => new(InputButtons.Light | InputButtons.Down),
                    _ => new(InputButtons.Light),
                };
            }
            if (roll < Aggression + 15) return new(InputButtons.Dodge | away);
            SetPlan(InputButtons.Block, 12);
            return new(InputButtons.Block);
        }

        if (distance <= mid)
        {
            if (roll < Aggression / 2) return new(InputButtons.Special | toward);
            if (roll < Aggression / 2 + 10) return new(InputButtons.Up | toward);
            SetPlan(toward, 10 + DeterministicRandom.Range(ref _rng, 15));
            return new(toward);
        }

        if (roll < 25) return new(InputButtons.Special);
        SetPlan(toward, 20 + DeterministicRandom.Range(ref _rng, 20));
        return new(toward);
    }

    private InputFrame DoFinisher(in MatchState state, MatchContext context, in FighterState me)
    {
        if (_sequence is null)
        {
            if (state.PhaseFrame < 50 || !Roll(_difficulty == CpuDifficulty.Easy ? 1 : 6)) return InputFrame.Neutral;
            var finishers = context.Finishers[_slot];
            var choice = finishers[DeterministicRandom.Range(ref _rng, finishers.Length)];
            if (choice.Kind == FinisherKind.Legendary && state.Crowd < FinisherDefinition.LegendaryCrowdRequirement)
                choice = finishers[0];
            _sequence = choice.Sequence;
            _sequenceStep = 0;
            _sequenceTimer = 0;
        }

        // Press one token every 8 frames, releasing in between so each counts as a new press.
        _sequenceTimer++;
        if (_sequenceTimer % 8 != 0) return InputFrame.Neutral;
        if (_sequenceStep >= _sequence.Length)
        {
            _sequence = null;
            return InputFrame.Neutral;
        }
        return new(TokenToButtons(_sequence[_sequenceStep++], me.Facing));
    }

    public static InputButtons TokenToButtons(InputToken token, int facing) => token switch
    {
        InputToken.Up => InputButtons.Up,
        InputToken.Down => InputButtons.Down,
        InputToken.Forward => facing >= 0 ? InputButtons.Right : InputButtons.Left,
        InputToken.Back => facing >= 0 ? InputButtons.Left : InputButtons.Right,
        InputToken.Light => InputButtons.Light,
        InputToken.Heavy => InputButtons.Heavy,
        InputToken.Grab => InputButtons.Grab,
        _ => InputButtons.Special,
    };

    private static InputFrame Pulse(InputButtons button, int frame) => frame % 6 < 3 ? new(button) : InputFrame.Neutral;

    private void SetPlan(InputButtons plan, int frames)
    {
        _plan = plan;
        _planFrames = frames;
    }

    private bool Roll(int percent) => DeterministicRandom.Chance(ref _rng, percent);
}
