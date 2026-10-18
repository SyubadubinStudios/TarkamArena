using TarkamArena.Core.Combat;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Match;

/// <summary>
/// Deterministic fixed-step fight simulation. <see cref="Step"/> never allocates, never reads the
/// clock and only uses fixed-point maths, so the same inputs always produce the same state.
/// </summary>
public static partial class MatchSimulation
{
    public const int IntroFrames = 120;
    public const int RoundOverFrames = 110;
    public const int FinisherWindowFrames = 330;
    public const int FinisherPerformFrames = 270;
    public const int ParryWindow = 6;
    public const int MaxJuggle = 6;
    public const int RageDuration = 480;
    public const int AdrenalineDuration = 360;
    public const int MeterMax = 1000;
    public const int CrowdMax = 1000;

    private static readonly Fixed s_gravity = Fixed.FromFloat(0.018f);
    private static readonly Fixed s_jumpVelocity = Fixed.FromFloat(0.31f);
    private static readonly Fixed s_airSpeed = Fixed.FromFloat(0.075f);
    private static readonly Fixed s_friction = Fixed.FromFloat(0.012f);
    private static readonly Fixed s_dodgeSpeed = Fixed.FromFloat(0.11f);
    private static readonly Fixed s_startGap = Fixed.FromFloat(1.6f);
    private static readonly Fixed s_hazardSpeed = Fixed.FromFloat(0.13f);
    private static readonly Fixed s_projectileHalf = Fixed.FromFloat(0.25f);

    // ───────────────────────── setup ─────────────────────────

    public static MatchState CreateInitialState(MatchContext context)
    {
        var state = new MatchState
        {
            Rng = DeterministicRandom.Seed(context.Setup.Seed),
            FighterCount = context.Characters.Length,
            Round = 1,
            RoundWinnerTeam = -1,
            MatchWinnerTeam = -1,
            FinisherExecutor = -1,
            FinisherVictim = -1,
            FinisherPerformed = -1,
            LastArenaEvent = -1,
            Crowd = 250,
        };

        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var f = ref state.Fighters[i];
            f.Present = true;
            f.Slot = (byte)i;
            f.Team = GameModes.TeamOf(context.Rules.Mode, i);
            int baseHealth = context.Characters[i].BaseHealth * context.Setup.Fighters[i].StartingHealthPercent / 100;
            f.MaxHealth = context.Characters[i].BaseHealth;
            f.Health = Math.Max(1, baseHealth);
        }

        ResetRound(ref state, context, keepHealth: true);
        return state;
    }

    private static void ResetRound(ref MatchState state, MatchContext context, bool keepHealth)
    {
        state.Phase = MatchPhase.Intro;
        state.PhaseFrame = 0;
        state.TimerFrames = context.Rules.RoundSeconds * MatchState.FramesPerSecond;
        state.RoundWinnerTeam = -1;
        state.Hazard = default;
        state.NextArenaEventFrame = state.Frame + 900 + DeterministicRandom.Range(ref state.Rng, 600);
        for (int i = 0; i < MatchState.MaxProjectiles; i++) state.Projectiles[i] = default;
        for (int i = 0; i < MatchState.MaxSummons; i++) state.Summons[i] = default;

        int onField = 0;
        for (int i = 0; i < state.FighterCount; i++)
            if (GameModes.StartsOnField(context.Rules.Mode, i)) onField++;

        int placed = 0;
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var f = ref state.Fighters[i];
            bool field = GameModes.StartsOnField(context.Rules.Mode, i);
            f.OnField = field;
            if (!keepHealth) f.Health = f.MaxHealth;

            // Spread fighters evenly around the centre, team 0 on the left.
            Fixed x;
            if (onField == 2)
                x = placed == 0 ? -s_startGap : s_startGap;
            else
                x = Fixed.FromRatio(placed * 2 - (onField - 1), 1) * Fixed.FromFloat(1.3f);
            if (field) placed++;

            f.Position = new FixedVector2(field ? x : (f.Team == 0 ? -context.HalfWidth : context.HalfWidth), Fixed.Zero);
            f.Velocity = default;
            f.Facing = (sbyte)(f.Position.X.Raw <= 0 ? 1 : -1);
            f.Grounded = true;
            f.Status = field ? FighterStatus.Idle : FighterStatus.Benched;
            f.StatusFrame = 0;
            f.StunFrames = 0;
            f.Move = MoveId.None;
            f.Hitstop = 0;
            f.Invulnerable = 0;
            f.ComboHits = 0;
            f.ComboDamage = 0;
            f.JuggleCount = 0;
            f.PoisonFrames = 0;
            f.RageTimer = 0;
            f.AdrenalineTimer = 0;
            f.LastAttacker = -1;
            f.Target = -1;
            f.BlockPressedFrame = -1000;
        }

        Emit(ref state, MatchEventKind.RoundStart, -1, -1, (short)state.Round, default);
    }

    // ───────────────────────── step ─────────────────────────

    /// <summary>Advances the match by one frame. <paramref name="inputs"/> holds one entry per slot.</summary>
    public static void Step(ref MatchState state, MatchContext context, ReadOnlySpan<InputFrame> inputs)
    {
        state.EventCount = 0;
        state.Frame++;
        state.PhaseFrame++;

        for (int i = 0; i < state.FighterCount; i++)
            RecordInput(ref state.Fighters[i], i < inputs.Length ? inputs[i] : InputFrame.Neutral);

        switch (state.Phase)
        {
            case MatchPhase.Intro:
                if (state.PhaseFrame >= IntroFrames)
                {
                    state.Phase = MatchPhase.Fight;
                    state.PhaseFrame = 0;
                    Emit(ref state, MatchEventKind.Fight, -1, -1, 0, default);
                }
                break;

            case MatchPhase.Fight:
                StepFight(ref state, context);
                break;

            case MatchPhase.RoundOver:
                StepFighters(ref state, context, allowActions: false);
                if (state.PhaseFrame >= RoundOverFrames) EndRound(ref state, context);
                break;

            case MatchPhase.FinisherWindow:
                StepFinisherWindow(ref state, context);
                break;

            case MatchPhase.FinisherPerforming:
                if (state.PhaseFrame >= FinisherPerformFrames)
                {
                    state.Phase = MatchPhase.MatchOver;
                    state.PhaseFrame = 0;
                    Emit(ref state, MatchEventKind.MatchWin, state.FinisherExecutor, -1, state.MatchWinnerTeam, default);
                }
                break;

            case MatchPhase.MatchOver:
                break;
        }
    }

    private static void RecordInput(ref FighterState f, InputFrame input)
    {
        ushort now = input.Pack();
        f.Pressed = (ushort)(now & ~f.Held);
        f.Held = now;
        f.HistoryHead = (byte)((f.HistoryHead + 1) % InputHistory.Length);
        f.History[f.HistoryHead] = now;

        if (f.Pressed == 0) return;
        var buttons = (InputButtons)f.Pressed;
        if ((buttons & InputButtons.Up) != 0) PushToken(ref f, InputToken.Up);
        if ((buttons & InputButtons.Down) != 0) PushToken(ref f, InputToken.Down);
        if ((buttons & InputButtons.Right) != 0) PushToken(ref f, f.Facing >= 0 ? InputToken.Forward : InputToken.Back);
        if ((buttons & InputButtons.Left) != 0) PushToken(ref f, f.Facing >= 0 ? InputToken.Back : InputToken.Forward);
        if ((buttons & InputButtons.Light) != 0) PushToken(ref f, InputToken.Light);
        if ((buttons & InputButtons.Heavy) != 0) PushToken(ref f, InputToken.Heavy);
        if ((buttons & InputButtons.Grab) != 0) PushToken(ref f, InputToken.Grab);
        if ((buttons & InputButtons.Special) != 0) PushToken(ref f, InputToken.Special);
    }

    private static void PushToken(ref FighterState f, InputToken token)
    {
        f.TokenHead = (byte)((f.TokenHead + 1) % TokenHistory.Length);
        f.Tokens[f.TokenHead] = (byte)token;
        if (f.TokenCount < TokenHistory.Length) f.TokenCount++;
    }

    private static void StepFight(ref MatchState state, MatchContext context)
    {
        if (context.Rules.TimerEnabled && state.TimerFrames > 0) state.TimerFrames--;

        StepFighters(ref state, context, allowActions: true);
        StepProjectiles(ref state, context);
        StepSummons(ref state, context);
        if (context.Rules.ArenaEventsEnabled) StepHazards(ref state, context);
        ResolveHits(ref state, context);
        StepCrowd(ref state, context);
        CheckRoundEnd(ref state, context);
    }

    // ───────────────────────── fighters ─────────────────────────

    private static void StepFighters(ref MatchState state, MatchContext context, bool allowActions)
    {
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var f = ref state.Fighters[i];
            if (!f.IsActive) continue;

            UpdateTarget(ref state, i);
            if (f.Hitstop > 0)
            {
                f.Hitstop--;
                continue;
            }

            f.StatusFrame++;
            if (f.Invulnerable > 0) f.Invulnerable--;
            if (f.RageTimer > 0) f.RageTimer--;
            if (f.AdrenalineTimer > 0) f.AdrenalineTimer--;
            TickPoison(ref state, ref f);
            // The Jurus meter trickles up on its own during a fight.
            if (allowActions && f.IsAlive && (state.Frame & 7) == 0 && f.Jurus < MeterMax) f.Jurus++;

            if (f.WasPressed(InputButtons.Block)) f.BlockPressedFrame = state.Frame;

            if (allowActions && f.IsAlive) UpdateStatus(ref state, context, i);
            else if (f.Status is FighterStatus.Walk or FighterStatus.Crouch) f.Status = FighterStatus.Idle;

            Integrate(ref state, context, ref f);
        }

        SeparateFighters(ref state, context);
    }

    private static void TickPoison(ref MatchState state, ref FighterState f)
    {
        if (f.PoisonFrames <= 0) return;
        f.PoisonFrames--;
        if (f.PoisonFrames % 30 == 0 && f.Health > 1) f.Health = Math.Max(1, f.Health - 6);
    }

    private static void UpdateTarget(ref MatchState state, int slot)
    {
        ref var f = ref state.Fighters[slot];
        int best = -1;
        Fixed bestDistance = Fixed.FromInt(10000);
        for (int j = 0; j < state.FighterCount; j++)
        {
            ref readonly var o = ref state.Fighters[j];
            if (j == slot || !o.IsActive || o.Team == f.Team || !o.IsAlive && state.Phase == MatchPhase.Fight) continue;
            Fixed d = Fixed.Abs(o.Position.X - f.Position.X);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = j;
            }
        }
        f.Target = (sbyte)best;
    }

    private static bool CanTurn(in FighterState f) =>
        f.Grounded && f.Status is FighterStatus.Idle or FighterStatus.Walk or FighterStatus.Crouch;

    private static void UpdateStatus(ref MatchState state, MatchContext context, int slot)
    {
        ref var f = ref state.Fighters[slot];

        if (f.Target >= 0 && CanTurn(f))
        {
            ref readonly var t = ref state.Fighters[f.Target];
            int dir = Fixed.Sign(t.Position.X - f.Position.X);
            if (dir != 0) f.Facing = (sbyte)dir;
        }

        switch (f.Status)
        {
            case FighterStatus.Attack:
                UpdateAttack(ref state, context, slot);
                return;

            case FighterStatus.Hitstun:
            case FighterStatus.Blockstun:
            case FighterStatus.Parried:
                if (--f.StunFrames <= 0)
                {
                    SetStatus(ref f, FighterStatus.Idle);
                    if (f.Status == FighterStatus.Idle) EndCombo(ref state, slot);
                }
                return;

            case FighterStatus.Juggle:
                // Landing is handled in Integrate.
                return;

            case FighterStatus.Knockdown:
                if (f.StatusFrame >= 40)
                {
                    EndCombo(ref state, slot);
                    SetStatus(ref f, FighterStatus.GetUp);
                    f.Invulnerable = 12;
                }
                return;

            case FighterStatus.GetUp:
                if (f.StatusFrame >= 12) SetStatus(ref f, FighterStatus.Idle);
                return;

            case FighterStatus.Dodge:
                if (f.StatusFrame >= 20) SetStatus(ref f, FighterStatus.Idle);
                return;

            case FighterStatus.Benched:
            case FighterStatus.KO:
            case FighterStatus.Dizzy:
            case FighterStatus.Victory:
            case FighterStatus.Finishing:
                return;
        }

        // Actionable: Idle, Walk, Crouch, Airborne.
        TryStartAction(ref state, context, slot);
    }

    private static void SetStatus(ref FighterState f, FighterStatus status)
    {
        f.Status = status;
        f.StatusFrame = 0;
    }

    private static int ForwardSign(in FighterState f) =>
        f.IsHeld(InputButtons.Right) ? (f.Facing >= 0 ? 1 : -1) :
        f.IsHeld(InputButtons.Left) ? (f.Facing >= 0 ? -1 : 1) : 0;

    private static void TryStartAction(ref MatchState state, MatchContext context, int slot)
    {
        ref var f = ref state.Fighters[slot];
        bool air = !f.Grounded;
        int fwd = ForwardSign(f);
        bool down = f.IsHeld(InputButtons.Down);

        // Rage mode: a burst that clears space, then a damage boost.
        if (f.WasPressed(InputButtons.Rage) && f.Rage >= MeterMax && !f.RageActive && !air)
        {
            f.Rage = 0;
            f.RageTimer = RageDuration;
            Emit(ref state, MatchEventKind.RageActivated, slot, -1, 0, f.Position);
            StartMove(ref state, context, slot, MoveId.RageBurst);
            return;
        }

        if (f.WasPressed(InputButtons.Tag) && context.Rules.Mode == GameMode.TagBattle && !air && TryTag(ref state, context, slot))
            return;

        if (f.WasPressed(InputButtons.Dodge) && !air)
        {
            SetStatus(ref f, FighterStatus.Dodge);
            f.Invulnerable = 14;
            f.Velocity.X = s_dodgeSpeed * (fwd >= 0 && fwd != 0 ? f.Facing : -f.Facing);
            Emit(ref state, MatchEventKind.Dodge, slot, -1, 0, f.Position);
            return;
        }

        if (f.WasPressed(InputButtons.Special) && !air && TrySignature(ref state, context, slot))
            return;

        if (f.WasPressed(InputButtons.Special))
        {
            MoveId special = down ? MoveId.SpecialDown : fwd > 0 ? MoveId.SpecialForward : MoveId.SpecialNeutral;
            if (!air || special == MoveId.SpecialNeutral)
            {
                StartMove(ref state, context, slot, special);
                return;
            }
        }

        bool lightPressed = f.WasPressed(InputButtons.Light);
        bool heavyPressed = f.WasPressed(InputButtons.Heavy);

        if ((lightPressed || heavyPressed) && !air)
        {
            // Motion inputs: quarter-circle forward = forward special, forward-down-downforward = uppercut.
            if (DetectQuarterCircleForward(f))
            {
                StartMove(ref state, context, slot, MoveId.SpecialForward);
                return;
            }
            if (DetectDragonPunch(f))
            {
                StartMove(ref state, context, slot, MoveId.SpecialDown);
                return;
            }
        }

        if (f.WasPressed(InputButtons.Grab) && !air)
        {
            StartMove(ref state, context, slot, MoveId.Grab);
            return;
        }

        if (heavyPressed)
        {
            StartMove(ref state, context, slot, air ? MoveId.AirHeavy : down ? MoveId.Launcher : MoveId.Heavy);
            return;
        }

        if (lightPressed)
        {
            StartMove(ref state, context, slot, air ? MoveId.AirLight : down ? MoveId.LowLight : MoveId.Jab);
            return;
        }

        if (air)
        {
            f.Status = FighterStatus.Airborne;
            return;
        }

        if (f.WasPressed(InputButtons.Up))
        {
            f.Grounded = false;
            f.Velocity = new FixedVector2(s_airSpeed * fwd * f.Facing, s_jumpVelocity);
            SetStatus(ref f, FighterStatus.Airborne);
            Emit(ref state, MatchEventKind.Jump, slot, -1, 0, f.Position);
            return;
        }

        f.Blocking = f.IsHeld(InputButtons.Block);
        f.LowBlocking = f.Blocking && down;

        if (down)
        {
            if (f.Status != FighterStatus.Crouch) SetStatus(ref f, FighterStatus.Crouch);
            f.Velocity.X = Fixed.Zero;
        }
        else if (fwd != 0 && !f.Blocking)
        {
            if (f.Status != FighterStatus.Walk) SetStatus(ref f, FighterStatus.Walk);
            Fixed speed = context.WalkSpeed[slot];
            if (fwd < 0) speed = speed * Fixed.FromRatio(3, 4);
            if (f.AdrenalineActive) speed = speed * Fixed.FromRatio(13, 10);
            f.Velocity.X = speed * fwd * f.Facing;
        }
        else
        {
            if (f.Status != FighterStatus.Idle) SetStatus(ref f, FighterStatus.Idle);
            f.Velocity.X = Fixed.Zero;
        }
    }

    private static bool DetectQuarterCircleForward(in FighterState f)
    {
        // Looks for Down, then Down+Forward, then Forward within the last 15 frames.
        InputButtons forward = f.Facing >= 0 ? InputButtons.Right : InputButtons.Left;
        int stage = 0;
        for (int back = 14; back >= 0; back--)
        {
            var b = f.HistoryAt(back) & InputButtons.Directions;
            switch (stage)
            {
                case 0 when b == InputButtons.Down: stage = 1; break;
                case 1 when b == (InputButtons.Down | forward): stage = 2; break;
                case 2 when b == forward: return true;
            }
        }
        return false;
    }

    private static bool DetectDragonPunch(in FighterState f)
    {
        InputButtons forward = f.Facing >= 0 ? InputButtons.Right : InputButtons.Left;
        int stage = 0;
        for (int back = 17; back >= 0; back--)
        {
            var b = f.HistoryAt(back) & InputButtons.Directions;
            switch (stage)
            {
                case 0 when b == forward: stage = 1; break;
                case 1 when b == InputButtons.Down: stage = 2; break;
                case 2 when b == (InputButtons.Down | forward): return true;
            }
        }
        return false;
    }

    private static bool TryTag(ref MatchState state, MatchContext context, int slot)
    {
        int partner = slot ^ 1;
        if (partner >= state.FighterCount) return false;
        ref var p = ref state.Fighters[partner];
        if (!p.Present || p.OnField || !p.IsAlive) return false;

        ref var f = ref state.Fighters[slot];
        SwapIn(ref state, context, slot, partner, f.Target >= 0 && state.Fighters[f.Target].Status is FighterStatus.Hitstun or FighterStatus.Juggle);
        return true;
    }

    private static void SwapIn(ref MatchState state, MatchContext context, int outgoing, int incoming, bool strike)
    {
        ref var o = ref state.Fighters[outgoing];
        ref var p = ref state.Fighters[incoming];

        p.OnField = true;
        p.Position = new FixedVector2(o.Position.X - Fixed.FromFloat(0.4f) * o.Facing, Fixed.Zero);
        p.Velocity = default;
        p.Facing = o.Facing;
        p.Grounded = true;
        p.Invulnerable = 20;
        p.Move = MoveId.None;
        SetStatus(ref p, FighterStatus.Idle);

        o.OnField = false;
        o.Velocity = default;
        SetStatus(ref o, FighterStatus.Benched);
        o.Position = new FixedVector2(o.Team == 0 ? -context.HalfWidth - Fixed.One : context.HalfWidth + Fixed.One, Fixed.Zero);

        Emit(ref state, MatchEventKind.TagSwitch, (sbyte)incoming, (sbyte)outgoing, 0, p.Position);
        if (strike) StartMove(ref state, context, incoming, MoveId.TagStrike);
    }

    private static void StartMove(ref MatchState state, MatchContext context, int slot, MoveId id)
    {
        ref var f = ref state.Fighters[slot];
        ref readonly var move = ref context.Move(slot, id);
        f.Move = id;
        f.MoveConnected = false;
        f.Blocking = false;
        SetStatus(ref f, FighterStatus.Attack);
        if (move.Has(MoveFlags.StartupInvulnerable)) f.Invulnerable = Math.Max(f.Invulnerable, move.Startup + 1);
        if (f.Grounded && !move.Has(MoveFlags.Lunge)) f.Velocity.X = Fixed.Zero;
        if (move.LaunchVelocity > Fixed.Zero && id == MoveId.SpecialDown)
        {
            // Rising uppercut: the attacker leaves the ground with the victim.
            f.Grounded = false;
            f.Velocity.Y = Fixed.FromFloat(0.22f);
        }
    }

    private static void UpdateAttack(ref MatchState state, MatchContext context, int slot)
    {
        ref var f = ref state.Fighters[slot];
        ref readonly var move = ref context.Move(slot, f.Move);
        int frame = f.StatusFrame;
        int activeStart = move.Startup + 1;
        int activeEnd = move.Startup + move.Active;

        int recovery = move.Recovery;
        if (f.AdrenalineActive) recovery = recovery * 2 / 3;

        if (move.Has(MoveFlags.Lunge) && frame <= activeEnd)
            f.Velocity.X = move.LungeSpeed * f.Facing;
        else if (f.Grounded)
            f.Velocity.X = Fixed.Approach(f.Velocity.X, Fixed.Zero, s_friction * 2);

        if (frame == activeStart)
        {
            if (move.Has(MoveFlags.Projectile)) SpawnProjectile(ref state, slot, in move);
            if (move.Has(MoveFlags.Summon)) SpawnSignature(ref state, context, slot);
            if (move.Has(MoveFlags.Teleport) && f.Target >= 0)
            {
                ref readonly var t = ref state.Fighters[f.Target];
                Fixed x = Fixed.Clamp(t.Position.X - Fixed.FromFloat(0.9f) * t.Facing, -context.HalfWidth, context.HalfWidth);
                f.Position = new FixedVector2(x, Fixed.Zero);
                f.Grounded = true;
                f.Facing = (sbyte)(Fixed.Sign(t.Position.X - x) is var s && s != 0 ? s : f.Facing);
            }
        }

        // Target combos and special cancels during the first part of recovery after a hit.
        if (f.MoveConnected && frame > activeEnd && frame <= activeEnd + 10)
        {
            if (f.WasPressed(InputButtons.Light) && move.ChainsInto != MoveId.None)
            {
                StartMove(ref state, context, slot, move.ChainsInto);
                return;
            }
            // Normals that hit can be cancelled into a signature move (the classic super cancel).
            if (f.WasPressed(InputButtons.Special) && f.Move is MoveId.Jab or MoveId.Cross or MoveId.Hook or MoveId.Heavy or MoveId.LowLight && TrySignature(ref state, context, slot))
                return;
            if (f.WasPressed(InputButtons.Special) && f.Move is MoveId.Jab or MoveId.Cross or MoveId.Hook or MoveId.Heavy or MoveId.LowLight)
            {
                bool down = f.IsHeld(InputButtons.Down);
                StartMove(ref state, context, slot, down ? MoveId.SpecialDown : ForwardSign(f) > 0 ? MoveId.SpecialForward : MoveId.SpecialNeutral);
                return;
            }
            if (f.WasPressed(InputButtons.Up) && f.Move == MoveId.Launcher)
            {
                // Jump-cancel the launcher to follow the victim for an air combo.
                f.Move = MoveId.None;
                f.Grounded = false;
                f.Velocity = new FixedVector2(s_airSpeed * f.Facing, s_jumpVelocity * Fixed.FromRatio(11, 10));
                SetStatus(ref f, FighterStatus.Airborne);
                return;
            }
        }

        if (frame >= move.Startup + move.Active + recovery)
        {
            if (!f.MoveConnected && move.Damage > 0) Emit(ref state, MatchEventKind.Whiff, slot, -1, (short)f.Move, f.Position);
            f.Move = MoveId.None;
            SetStatus(ref f, f.Grounded ? FighterStatus.Idle : FighterStatus.Airborne);
        }
    }

    private static void SpawnProjectile(ref MatchState state, int slot, in MoveData move)
    {
        ref readonly var f = ref state.Fighters[slot];
        for (int i = 0; i < MatchState.MaxProjectiles; i++)
        {
            ref var p = ref state.Projectiles[i];
            if (p.Active) continue;
            p = new Projectile
            {
                Active = true,
                Owner = (sbyte)slot,
                Team = f.Team,
                Position = new FixedVector2(f.Position.X + move.HitboxOffset.X * f.Facing, f.Position.Y + move.HitboxOffset.Y),
                VelocityX = move.ProjectileSpeed * f.Facing,
                Life = 100,
                Damage = move.Damage,
                Chip = move.ChipDamage,
                Hitstun = move.Hitstun,
                Blockstun = move.Blockstun,
                Size = move.HitboxSize,
            };
            Emit(ref state, MatchEventKind.ProjectileSpawn, slot, (sbyte)i, 0, p.Position);
            return;
        }
    }

    private static void Integrate(ref MatchState state, MatchContext context, ref FighterState f)
    {
        if (!f.Grounded)
        {
            f.Velocity.Y -= s_gravity;
            if (f.Status == FighterStatus.Juggle) f.Velocity.X = Fixed.Approach(f.Velocity.X, Fixed.Zero, Fixed.FromFloat(0.002f));
        }
        else if (f.Status is FighterStatus.Hitstun or FighterStatus.Blockstun or FighterStatus.Knockdown or FighterStatus.Dodge or FighterStatus.Parried)
        {
            f.Velocity.X = Fixed.Approach(f.Velocity.X, Fixed.Zero, s_friction);
        }

        f.Position += f.Velocity;

        if (f.Position.Y <= Fixed.Zero && (!f.Grounded || f.Velocity.Y < Fixed.Zero))
        {
            bool wasAirborne = !f.Grounded;
            f.Position.Y = Fixed.Zero;
            f.Velocity.Y = Fixed.Zero;
            f.Grounded = true;
            if (wasAirborne)
            {
                Emit(ref state, MatchEventKind.Land, (sbyte)f.Slot, -1, 0, f.Position);
                if (f.Status == FighterStatus.Juggle)
                {
                    SetStatus(ref f, f.IsAlive ? FighterStatus.Knockdown : FighterStatus.KO);
                    f.Invulnerable = 40;
                    f.JuggleCount = 0;
                    Emit(ref state, MatchEventKind.Knockdown, (sbyte)f.Slot, -1, 0, f.Position);
                }
                else if (f.Status == FighterStatus.Airborne)
                {
                    SetStatus(ref f, FighterStatus.Idle);
                    f.Velocity.X = Fixed.Zero;
                }
                else if (f.Status == FighterStatus.Attack && f.Move is MoveId.AirLight or MoveId.AirHeavy)
                {
                    // Landing cancels air attacks with a short recovery.
                    f.Move = MoveId.None;
                    SetStatus(ref f, FighterStatus.Idle);
                    f.Velocity.X = Fixed.Zero;
                }
            }
        }

        Fixed limit = context.HalfWidth;
        if (f.Position.X > limit) { f.Position.X = limit; f.Velocity.X = Fixed.Min(f.Velocity.X, Fixed.Zero); }
        if (f.Position.X < -limit) { f.Position.X = -limit; f.Velocity.X = Fixed.Max(f.Velocity.X, Fixed.Zero); }
    }

    private static void SeparateFighters(ref MatchState state, MatchContext context)
    {
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var a = ref state.Fighters[i];
            if (!a.IsActive || a.Status is FighterStatus.Dodge or FighterStatus.KO or FighterStatus.Benched) continue;
            for (int j = i + 1; j < state.FighterCount; j++)
            {
                ref var b = ref state.Fighters[j];
                if (!b.IsActive || b.Status is FighterStatus.Dodge or FighterStatus.KO or FighterStatus.Benched) continue;
                if (Fixed.Abs(a.Position.Y - b.Position.Y) > Fixed.FromFloat(1.2f)) continue;

                Fixed minGap = (context.HurtWidth[i] + context.HurtWidth[j]) / 2;
                Fixed dx = b.Position.X - a.Position.X;
                Fixed overlap = minGap - Fixed.Abs(dx);
                if (overlap <= Fixed.Zero) continue;

                int dir = Fixed.Sign(dx);
                if (dir == 0) dir = i < j ? 1 : -1;
                Fixed push = overlap / 2;
                a.Position.X -= push * dir;
                b.Position.X += push * dir;
                a.Position.X = Fixed.Clamp(a.Position.X, -context.HalfWidth, context.HalfWidth);
                b.Position.X = Fixed.Clamp(b.Position.X, -context.HalfWidth, context.HalfWidth);
            }
        }
    }

    // ───────────────────────── hits ─────────────────────────

    public static FixedBox HurtBox(MatchContext context, in FighterState f)
    {
        Fixed height = context.HurtHeight[f.Slot];
        if (f.Status == FighterStatus.Crouch || f.Status == FighterStatus.Attack && f.Move is MoveId.LowLight or MoveId.Launcher)
            height = height * Fixed.FromRatio(3, 5);
        if (f.Status is FighterStatus.Knockdown) height = Fixed.FromFloat(0.4f);
        Fixed hw = context.HurtWidth[f.Slot] / 2;
        return new FixedBox(f.Position.X - hw, f.Position.Y, f.Position.X + hw, f.Position.Y + height);
    }

    public static bool TryGetHitBox(MatchContext context, in FighterState f, out FixedBox box)
    {
        box = default;
        if (f.Status != FighterStatus.Attack || f.MoveConnected) return false;
        ref readonly var move = ref context.Move(f.Slot, f.Move);
        if (move.Has(MoveFlags.Projectile)) return false;
        int frame = f.StatusFrame;
        if (frame <= move.Startup || frame > move.Startup + move.Active) return false;
        Fixed scale = context.HurtHeight[f.Slot] / Fixed.FromFloat(1.72f);
        var offset = new FixedVector2(move.HitboxOffset.X, move.HitboxOffset.Y * scale);
        box = FixedBox.FromOffset(f.Position, offset, move.HitboxSize, f.Facing);
        return true;
    }

    private static void ResolveHits(ref MatchState state, MatchContext context)
    {
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var attacker = ref state.Fighters[i];
            if (!attacker.IsActive || attacker.Hitstop > 0) continue;
            if (!TryGetHitBox(context, attacker, out var hitBox)) continue;

            for (int j = 0; j < state.FighterCount; j++)
            {
                ref var victim = ref state.Fighters[j];
                if (j == i || !victim.IsActive || victim.Team == attacker.Team) continue;
                if (!CanBeHit(victim)) continue;
                var hurt = HurtBox(context, victim);
                if (!hitBox.Overlaps(hurt)) continue;

                ref readonly var move = ref context.Move(i, attacker.Move);
                ApplyHit(ref state, context, i, j, in move, isProjectile: false, hitBox.CenterX, hitBox.CenterY);
                attacker.MoveConnected = true;
                if (context.Rules.Mode != GameMode.FreeForAll) break;
            }
        }
    }

    private static bool CanBeHit(in FighterState v) =>
        v.Invulnerable <= 0 && v.Status is not (FighterStatus.Benched or FighterStatus.KO or FighterStatus.Dizzy or FighterStatus.Finishing or FighterStatus.Knockdown or FighterStatus.GetUp)
        && !(v.Status == FighterStatus.Juggle && v.JuggleCount >= MaxJuggle);

    private static void ApplyHit(ref MatchState state, MatchContext context, int attackerSlot, int victimSlot, in MoveData move, bool isProjectile, Fixed hitX, Fixed hitY, bool freezeAttacker = true)
    {
        ref var a = ref state.Fighters[attackerSlot];
        ref var v = ref state.Fighters[victimSlot];
        var hitPos = new FixedVector2(hitX, hitY);
        int dir = Fixed.Sign(v.Position.X - a.Position.X);
        if (dir == 0) dir = a.Facing;

        bool grab = move.Has(MoveFlags.Unblockable) && (move.Id == MoveId.Grab || move.Has(MoveFlags.CommandGrab));
        if (grab)
        {
            // Grabs whiff on airborne victims and on victims already in hitstun (throw protection).
            if (!v.Grounded || v.Status is FighterStatus.Hitstun or FighterStatus.Juggle) return;
            // Grab against grab startup = throw tech.
            if (v.Status == FighterStatus.Attack && v.Move == MoveId.Grab && v.StatusFrame <= context.Move(victimSlot, MoveId.Grab).Startup + 2)
            {
                a.Velocity.X = Fixed.FromFloat(-0.1f) * dir;
                v.Velocity.X = Fixed.FromFloat(0.1f) * dir;
                SetStatus(ref a, FighterStatus.Blockstun); a.StunFrames = 16;
                SetStatus(ref v, FighterStatus.Blockstun); v.StunFrames = 16;
                Emit(ref state, MatchEventKind.Tech, (sbyte)attackerSlot, (sbyte)victimSlot, 0, hitPos);
                return;
            }
        }

        bool facingAttacker = Fixed.Sign(a.Position.X - v.Position.X) == v.Facing || isProjectile;
        bool canGuard = !move.Has(MoveFlags.Unblockable) && v.Grounded && facingAttacker &&
                        v.Status is FighterStatus.Idle or FighterStatus.Walk or FighterStatus.Crouch or FighterStatus.Blockstun;
        bool guarding = canGuard && v.IsHeld(InputButtons.Block);

        // Parry: Block tapped just before the hit lands.
        if (canGuard && state.Frame - v.BlockPressedFrame <= ParryWindow && !isProjectile)
        {
            a.Hitstop = 10;
            v.Hitstop = 10;
            SetStatus(ref a, FighterStatus.Parried);
            a.StunFrames = 26;
            a.Move = MoveId.None;
            v.BlockPressedFrame = -1000;
            v.Adrenaline = Math.Min(MeterMax, v.Adrenaline + 120);
            v.Jurus = Math.Min(MeterMax, v.Jurus + 60);
            AddCrowd(ref state, context, 70);
            Emit(ref state, MatchEventKind.Parry, (sbyte)victimSlot, (sbyte)attackerSlot, 0, hitPos);
            CheckAdrenaline(ref state, victimSlot);
            return;
        }

        if (guarding)
        {
            bool wrongHeight = move.Has(MoveFlags.Low) && !v.LowBlocking || move.Has(MoveFlags.Overhead) && v.LowBlocking;
            if (!wrongHeight)
            {
                int chip = move.ChipDamage * context.Rules.DamagePercent / 100;
                v.Health = Math.Max(1, v.Health - chip);
                SetStatus(ref v, FighterStatus.Blockstun);
                v.StunFrames = move.Blockstun;
                v.Velocity.X = move.Pushback * dir;
                if (freezeAttacker) a.Hitstop = move.Hitstop / 2;
                v.Hitstop = move.Hitstop / 2;
                v.Rage = Math.Min(MeterMax, v.Rage + chip);
                Emit(ref state, MatchEventKind.Block, (sbyte)victimSlot, (sbyte)attackerSlot, (short)chip, hitPos);
                return;
            }
        }

        bool counter = v.Status == FighterStatus.Attack && v.StatusFrame <= context.Move(victimSlot, v.Move).Startup;

        // Damage with combo scaling, counter bonus, rage boost, team combo bonus and a low-health rally.
        int scalePercent = Math.Max(35, 100 - v.ComboHits * 10);
        int damage = move.Damage * scalePercent / 100;
        if (counter) damage = damage * 125 / 100;
        if (a.RageActive) damage = damage * 125 / 100;
        bool teamCombo = v.ComboHits > 0 && v.LastAttacker >= 0 && v.LastAttacker != attackerSlot &&
                         state.Fighters[v.LastAttacker].Team == a.Team;
        if (teamCombo) damage = damage * 120 / 100;
        damage = damage * context.Rules.DamagePercent / 100;
        damage = Math.Max(1, damage);

        v.Health = Math.Max(0, v.Health - damage);
        v.ComboHits++;
        v.ComboDamage += damage;
        v.LastAttacker = (sbyte)attackerSlot;
        v.Blocking = false;
        if (move.Has(MoveFlags.Poison)) v.PoisonFrames = 180;

        // Meters: the victim builds rage (faster when low), the attacker builds adrenaline.
        int rageGain = damage * (v.Health * 4 < v.MaxHealth ? 2 : 1);
        if (move.Id != MoveId.Signature)
        {
            a.Jurus = Math.Min(MeterMax, a.Jurus + damage / 2);
            v.Jurus = Math.Min(MeterMax, v.Jurus + damage / 3);
        }
        v.Rage = Math.Min(MeterMax, v.Rage + rageGain);
        a.Adrenaline = Math.Min(MeterMax, a.Adrenaline + move.MeterGain + (state.Crowd > 700 ? 10 : 0));
        CheckAdrenaline(ref state, attackerSlot);

        int hitstop = move.Hitstop + (counter ? 4 : 0);
        if (freezeAttacker) a.Hitstop = hitstop;
        v.Hitstop = hitstop;

        bool airborneVictim = !v.Grounded;
        if (move.Has(MoveFlags.Spike) && airborneVictim)
        {
            v.Velocity = new FixedVector2(move.Pushback * dir / 2, Fixed.FromFloat(-0.35f));
            SetStatus(ref v, FighterStatus.Juggle);
            v.JuggleCount = MaxJuggle;
        }
        else if (move.LaunchVelocity > Fixed.Zero || airborneVictim)
        {
            Fixed up = move.LaunchVelocity > Fixed.Zero ? move.LaunchVelocity : Fixed.FromFloat(0.12f);
            // Juggle decay: each extra air hit pops the victim a little less.
            up = up * Fixed.FromRatio(Math.Max(4, 10 - v.JuggleCount), 10);
            v.Velocity = new FixedVector2(move.Pushback * dir / 2, up);
            v.Grounded = false;
            v.JuggleCount++;
            SetStatus(ref v, FighterStatus.Juggle);
            if (move.Has(MoveFlags.Launcher)) Emit(ref state, MatchEventKind.Launch, (sbyte)attackerSlot, (sbyte)victimSlot, 0, hitPos);
        }
        else if (move.Has(MoveFlags.KnockDown))
        {
            v.Velocity = new FixedVector2(move.Pushback * dir, Fixed.FromFloat(0.12f));
            v.Grounded = false;
            v.JuggleCount = MaxJuggle;
            SetStatus(ref v, FighterStatus.Juggle);
        }
        else
        {
            SetStatus(ref v, FighterStatus.Hitstun);
            v.StunFrames = move.Hitstun + (counter ? 6 : 0);
            v.Velocity.X = move.Pushback * dir;
        }
        v.Move = MoveId.None;

        if (!isProjectile && grab)
        {
            // Throws swap sides: the victim lands behind the attacker.
            v.Position.X = Fixed.Clamp(a.Position.X - Fixed.FromFloat(0.6f) * a.Facing, -context.HalfWidth, context.HalfWidth);
            v.Velocity.X = -v.Velocity.X;
        }

        Emit(ref state, counter ? MatchEventKind.CounterHit : MatchEventKind.Hit, (sbyte)attackerSlot, (sbyte)victimSlot, (short)damage, hitPos);
        if (teamCombo) Emit(ref state, MatchEventKind.TeamCombo, (sbyte)attackerSlot, (sbyte)victimSlot, (short)v.ComboHits, hitPos);

        AddCrowd(ref state, context, 8 + (v.ComboHits >= 3 ? 14 : 0) + (counter ? 40 : 0) + (teamCombo ? 60 : 0) + (airborneVictim ? 16 : 0));

        if (v.Health <= 0)
        {
            v.Velocity = new FixedVector2(Fixed.FromFloat(0.12f) * dir, Fixed.FromFloat(0.22f));
            v.Grounded = false;
            SetStatus(ref v, FighterStatus.Juggle);
            v.JuggleCount = MaxJuggle;
            a.Hitstop = 24;
            v.Hitstop = 24;
            AddCrowd(ref state, context, 160);
            Emit(ref state, MatchEventKind.KO, (sbyte)attackerSlot, (sbyte)victimSlot, (short)v.ComboHits, hitPos);
        }
    }

    private static void CheckAdrenaline(ref MatchState state, int slot)
    {
        ref var f = ref state.Fighters[slot];
        if (f.Adrenaline < MeterMax || f.AdrenalineActive) return;
        f.Adrenaline = 0;
        f.AdrenalineTimer = AdrenalineDuration;
        Emit(ref state, MatchEventKind.AdrenalineActivated, (sbyte)slot, -1, 0, f.Position);
    }

    private static void EndCombo(ref MatchState state, int slot)
    {
        ref var v = ref state.Fighters[slot];
        if (v.ComboHits >= 3)
            Emit(ref state, MatchEventKind.Combo, v.LastAttacker, (sbyte)slot, (short)v.ComboHits, v.Position);
        v.ComboHits = 0;
        v.ComboDamage = 0;
        v.JuggleCount = 0;
    }

    // ───────────────────────── projectiles & hazards ─────────────────────────

    private static void StepProjectiles(ref MatchState state, MatchContext context)
    {
        for (int i = 0; i < MatchState.MaxProjectiles; i++)
        {
            ref var p = ref state.Projectiles[i];
            if (!p.Active) continue;
            p.Position.X += p.VelocityX;
            if (--p.Life <= 0 || Fixed.Abs(p.Position.X) > context.HalfWidth + Fixed.One)
            {
                p.Active = false;
                continue;
            }

            var box = new FixedBox(p.Position.X - s_projectileHalf, p.Position.Y - s_projectileHalf, p.Position.X + s_projectileHalf, p.Position.Y + s_projectileHalf);

            // Opposing projectiles cancel each other out.
            for (int k = i + 1; k < MatchState.MaxProjectiles; k++)
            {
                ref var q = ref state.Projectiles[k];
                if (!q.Active || q.Team == p.Team) continue;
                var qBox = new FixedBox(q.Position.X - s_projectileHalf, q.Position.Y - s_projectileHalf, q.Position.X + s_projectileHalf, q.Position.Y + s_projectileHalf);
                if (!box.Overlaps(qBox)) continue;
                p.Active = false;
                q.Active = false;
                Emit(ref state, MatchEventKind.ProjectileClash, p.Owner, q.Owner, 0, p.Position);
                break;
            }
            if (!p.Active) continue;

            for (int j = 0; j < state.FighterCount; j++)
            {
                ref var v = ref state.Fighters[j];
                if (!v.IsActive || v.Team == p.Team || !CanBeHit(v)) continue;
                if (!box.Overlaps(HurtBox(context, v))) continue;
                var move = new MoveData
                {
                    Id = MoveId.SpecialNeutral, Damage = p.Damage, ChipDamage = p.Chip, Hitstun = p.Hitstun, Blockstun = p.Blockstun,
                    Hitstop = 6, Pushback = Fixed.FromFloat(0.08f), MeterGain = 30,
                };
                ApplyHit(ref state, context, p.Owner, j, in move, isProjectile: true, p.Position.X, p.Position.Y);
                p.Active = false;
                break;
            }
        }
    }

    private static void StepHazards(ref MatchState state, MatchContext context)
    {
        ref var h = ref state.Hazard;
        var events = context.Stage.Events;
        if (!h.Active && state.Frame >= state.NextArenaEventFrame && events.Count > 0)
        {
            int index = DeterministicRandom.Range(ref state.Rng, events.Count);
            state.LastArenaEvent = (sbyte)index;
            state.NextArenaEventFrame = state.Frame + 1100 + DeterministicRandom.Range(ref state.Rng, 900);
            Emit(ref state, MatchEventKind.ArenaEvent, -1, -1, (short)index, default);

            if (events[index].AffectsFight)
            {
                bool fromLeft = DeterministicRandom.Chance(ref state.Rng, 50);
                bool falling = events[index].Kind is Content.ArenaEventKind.FallingTree or Content.ArenaEventKind.FruitThrow or Content.ArenaEventKind.Eruption
                    or Content.ArenaEventKind.BassDrop or Content.ArenaEventKind.FireSpread or Content.ArenaEventKind.WaterBomb;
                h = new Hazard
                {
                    Active = true,
                    EventIndex = (byte)index,
                    Warning = 75,
                    Life = falling ? (short)40 : (short)260,
                    Size = falling ? new FixedVector2(Fixed.FromFloat(1.6f), Fixed.FromFloat(2.2f)) : new FixedVector2(Fixed.FromFloat(1.2f), Fixed.FromFloat(1.1f)),
                    Damage = 45,
                    VelocityX = falling ? Fixed.Zero : (fromLeft ? s_hazardSpeed : -s_hazardSpeed),
                    Position = falling
                        ? new FixedVector2(Fixed.FromRatio(DeterministicRandom.Range(ref state.Rng, -60, 61), 10), Fixed.Zero)
                        : new FixedVector2(fromLeft ? -context.HalfWidth - Fixed.FromInt(2) : context.HalfWidth + Fixed.FromInt(2), Fixed.Zero),
                };
            }
        }

        if (!h.Active) return;
        if (h.Warning > 0)
        {
            h.Warning--;
            return;
        }

        h.Position.X += h.VelocityX;
        if (--h.Life <= 0)
        {
            h.Active = false;
            return;
        }

        var box = new FixedBox(h.Position.X - h.Size.X / 2, h.Position.Y, h.Position.X + h.Size.X / 2, h.Position.Y + h.Size.Y);
        for (int j = 0; j < state.FighterCount; j++)
        {
            ref var v = ref state.Fighters[j];
            if (!v.IsActive || h.HasHit(j) || !CanBeHit(v) || !v.Grounded) continue;
            if (!box.Overlaps(HurtBox(context, v))) continue;
            h.MarkHit(j);
            int dir = h.VelocityX == Fixed.Zero ? (v.Position.X >= h.Position.X ? 1 : -1) : Fixed.Sign(h.VelocityX);
            v.Health = Math.Max(1, v.Health - h.Damage * context.Rules.DamagePercent / 100);
            v.Velocity = new FixedVector2(Fixed.FromFloat(0.14f) * dir, Fixed.FromFloat(0.18f));
            v.Grounded = false;
            v.JuggleCount = MaxJuggle;
            v.Move = MoveId.None;
            SetStatus(ref v, FighterStatus.Juggle);
            AddCrowd(ref state, context, 60);
            Emit(ref state, MatchEventKind.HazardHit, -1, (sbyte)j, h.Damage, v.Position);
        }
    }

    // ───────────────────────── crowd ─────────────────────────

    private static void AddCrowd(ref MatchState state, MatchContext context, int amount)
    {
        if (!context.Rules.CrowdEnabled) return;
        state.Crowd = Math.Clamp(state.Crowd + amount, 0, CrowdMax);
    }

    private static void StepCrowd(ref MatchState state, MatchContext context)
    {
        if (!context.Rules.CrowdEnabled) return;
        // The crowd cools off slowly, and gets bored quickly when nobody attacks.
        bool anyoneAttacking = false;
        for (int i = 0; i < state.FighterCount; i++)
            if (state.Fighters[i].IsActive && state.Fighters[i].Status == FighterStatus.Attack) anyoneAttacking = true;
        if (state.Frame % (anyoneAttacking ? 20 : 6) == 0 && state.Crowd > 150) state.Crowd--;
    }

    // ───────────────────────── rounds & finishers ─────────────────────────

    private static void CheckRoundEnd(ref MatchState state, MatchContext context)
    {
        // Tag battle: a KO'd point fighter is replaced by the partner instead of losing the round.
        if (context.Rules.Mode == GameMode.TagBattle)
        {
            for (int i = 0; i < state.FighterCount; i++)
            {
                ref var f = ref state.Fighters[i];
                if (f.IsActive && !f.IsAlive && f.Grounded && f.Status is FighterStatus.KO or FighterStatus.Knockdown)
                {
                    int partner = i ^ 1;
                    if (partner < state.FighterCount && state.Fighters[partner].Present && !state.Fighters[partner].OnField && state.Fighters[partner].IsAlive)
                        SwapIn(ref state, context, i, partner, strike: false);
                }
            }
        }

        int aliveTeams = 0;
        int lastAliveTeam = -1;
        Span<int> teamHealth = stackalloc int[MatchState.MaxFighters];
        Span<int> teamMaxHealth = stackalloc int[MatchState.MaxFighters];
        Span<bool> teamAlive = stackalloc bool[MatchState.MaxFighters];
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref readonly var f = ref state.Fighters[i];
            if (!f.Present) continue;
            teamHealth[f.Team] += f.Health;
            teamMaxHealth[f.Team] += f.MaxHealth;
            if (f.IsAlive && !teamAlive[f.Team])
            {
                teamAlive[f.Team] = true;
                aliveTeams++;
                lastAliveTeam = f.Team;
            }
        }

        int teamCount = 0;
        for (int i = 0; i < state.FighterCount; i++) teamCount = Math.Max(teamCount, state.Fighters[i].Team + 1);

        bool timeUp = context.Rules.TimerEnabled && state.TimerFrames <= 0;
        if (context.Rules.Mode == GameMode.Training)
        {
            // Training dummies refill instead of dying.
            for (int i = 0; i < state.FighterCount; i++)
            {
                ref var f = ref state.Fighters[i];
                if (f.Status is FighterStatus.Idle && f.Health < f.MaxHealth) f.Health = Math.Min(f.MaxHealth, f.Health + 8);
                if (!f.IsAlive) f.Health = f.MaxHealth;
                // ...and the Jurus meter refills so every signature can be practised.
                if (f.Move != MoveId.Signature) f.Jurus = Math.Min(MeterMax, f.Jurus + 12);
            }
            return;
        }

        if (aliveTeams > 1 && !timeUp) return;

        int winner;
        if (aliveTeams == 1)
        {
            winner = lastAliveTeam;
        }
        else if (aliveTeams == 0)
        {
            winner = -1;
        }
        else
        {
            // Time up: the team with the highest share of its health left wins. Ties are draws.
            winner = -1;
            long best = -1;
            bool tie = false;
            for (int t = 0; t < teamCount; t++)
            {
                if (!teamAlive[t] || teamMaxHealth[t] == 0) continue;
                long share = teamHealth[t] * 10000L / teamMaxHealth[t];
                if (share > best) { best = share; winner = t; tie = false; }
                else if (share == best) tie = true;
            }
            if (tie) winner = -1;
            Emit(ref state, MatchEventKind.TimeUp, -1, -1, 0, default);
        }

        state.RoundWinnerTeam = (sbyte)winner;
        if (winner >= 0) state.RoundWins[winner]++;
        state.Phase = MatchPhase.RoundOver;
        state.PhaseFrame = 0;
        Emit(ref state, MatchEventKind.RoundWin, -1, -1, (short)winner, default);

        for (int i = 0; i < state.FighterCount; i++)
        {
            ref var f = ref state.Fighters[i];
            if (!f.IsActive) continue;
            if (f.Team == winner && f.IsAlive && f.Status is not FighterStatus.Attack) SetStatus(ref f, FighterStatus.Victory);
        }
    }

    private static void EndRound(ref MatchState state, MatchContext context)
    {
        int winner = state.RoundWinnerTeam;
        bool matchWon = winner >= 0 && state.RoundWins[winner] >= context.Rules.RoundsToWin;

        if (!matchWon)
        {
            state.Round++;
            ResetRound(ref state, context, keepHealth: false);
            return;
        }

        state.MatchWinnerTeam = (sbyte)winner;

        if (!context.Rules.FinishersEnabled || !TryPickFinisherPair(ref state, winner, out int executor, out int victim))
        {
            state.Phase = MatchPhase.MatchOver;
            state.PhaseFrame = 0;
            Emit(ref state, MatchEventKind.MatchWin, -1, -1, (short)winner, default);
            return;
        }

        // "Habisi!" — the loser stands dazed while the winner enters a finisher.
        state.FinisherExecutor = (sbyte)executor;
        state.FinisherVictim = (sbyte)victim;
        state.Phase = MatchPhase.FinisherWindow;
        state.PhaseFrame = 0;
        ref var e = ref state.Fighters[executor];
        ref var v = ref state.Fighters[victim];
        v.Health = 0;
        v.Position = new FixedVector2(v.Position.X, Fixed.Zero);
        v.Grounded = true;
        v.Velocity = default;
        v.Move = MoveId.None;
        SetStatus(ref v, FighterStatus.Dizzy);
        e.Move = MoveId.None;
        e.TokenCount = 0;
        SetStatus(ref e, FighterStatus.Idle);
        Emit(ref state, MatchEventKind.FinisherPrompt, (sbyte)executor, (sbyte)victim, 0, v.Position);
    }

    private static bool TryPickFinisherPair(ref MatchState state, int winnerTeam, out int executor, out int victim)
    {
        executor = -1;
        victim = -1;
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref readonly var f = ref state.Fighters[i];
            if (!f.IsActive) continue;
            if (f.Team == winnerTeam && f.IsAlive && executor < 0) executor = i;
            if (f.Team != winnerTeam && victim < 0) victim = i;
        }
        // Prefer the fighter who landed the deciding blow.
        if (victim >= 0)
        {
            int last = state.Fighters[victim].LastAttacker;
            if (last >= 0 && state.Fighters[last].Team == winnerTeam && state.Fighters[last].IsActive && state.Fighters[last].IsAlive) executor = last;
        }
        return executor >= 0 && victim >= 0;
    }

    private static void StepFinisherWindow(ref MatchState state, MatchContext context)
    {
        int e = state.FinisherExecutor;
        int vIndex = state.FinisherVictim;
        ref var exec = ref state.Fighters[e];

        // The executor may walk to set up close-range finishers.
        UpdateTarget(ref state, e);
        if (exec.Target >= 0)
        {
            int dir = Fixed.Sign(state.Fighters[exec.Target].Position.X - exec.Position.X);
            if (dir != 0) exec.Facing = (sbyte)dir;
        }
        int fwd = ForwardSign(exec);
        exec.Velocity.X = fwd != 0 ? context.WalkSpeed[e] * fwd * exec.Facing : Fixed.Zero;
        Integrate(ref state, context, ref exec);
        SeparateFighters(ref state, context);

        if (exec.Pressed != 0 && exec.TokenCount >= 4)
        {
            var finishers = context.Finishers[e];
            for (int i = 0; i < finishers.Length; i++)
            {
                var def = finishers[i];
                if (!Matches(exec, def.Sequence)) continue;
                if (def.Kind == FinisherKind.Legendary && state.Crowd < FinisherDefinition.LegendaryCrowdRequirement) continue;
                if (def.Kind == FinisherKind.Mercy)
                {
                    if (state.MercyUsed) continue;
                    PerformMercy(ref state, context, e, vIndex);
                    return;
                }

                state.FinisherPerformed = (sbyte)i;
                state.Phase = MatchPhase.FinisherPerforming;
                state.PhaseFrame = 0;
                SetStatus(ref exec, FighterStatus.Finishing);
                exec.Velocity = default;
                AddCrowd(ref state, context, def.Kind == FinisherKind.Friendship ? 120 : 250);
                Emit(ref state, MatchEventKind.FinisherStart, (sbyte)e, (sbyte)vIndex, (short)i, state.Fighters[vIndex].Position);
                return;
            }
        }

        if (state.PhaseFrame >= FinisherWindowFrames)
        {
            state.Phase = MatchPhase.MatchOver;
            state.PhaseFrame = 0;
            SetStatus(ref state.Fighters[vIndex], FighterStatus.KO);
            SetStatus(ref exec, FighterStatus.Victory);
            Emit(ref state, MatchEventKind.MatchWin, (sbyte)e, -1, state.MatchWinnerTeam, default);
        }
    }

    private static bool Matches(in FighterState f, InputToken[] sequence)
    {
        if (f.TokenCount < sequence.Length) return false;
        for (int k = 0; k < sequence.Length; k++)
        {
            if (f.TokenAt(k) != sequence[sequence.Length - 1 - k]) return false;
        }
        return true;
    }

    private static void PerformMercy(ref MatchState state, MatchContext context, int executor, int victim)
    {
        // Mercy: the loser gets back on their feet with a sliver of health and the round continues.
        state.MercyUsed = true;
        int winner = state.MatchWinnerTeam;
        state.RoundWins[winner]--;
        state.MatchWinnerTeam = -1;
        state.FinisherExecutor = -1;
        state.FinisherVictim = -1;

        ref var v = ref state.Fighters[victim];
        v.Health = Math.Max(1, v.MaxHealth * 15 / 100);
        SetStatus(ref v, FighterStatus.Idle);
        v.Invulnerable = 60;
        v.Rage = MeterMax;

        state.Phase = MatchPhase.Fight;
        state.PhaseFrame = 0;
        state.TimerFrames = Math.Max(state.TimerFrames, 30 * MatchState.FramesPerSecond);
        SetStatus(ref state.Fighters[executor], FighterStatus.Idle);
        AddCrowd(ref state, context, 200);
        Emit(ref state, MatchEventKind.Mercy, (sbyte)executor, (sbyte)victim, 0, v.Position);
    }

    // ───────────────────────── events ─────────────────────────

    private static void Emit(ref MatchState state, MatchEventKind kind, int slot, int target, short value, FixedVector2 position)
    {
        if (state.EventCount >= MatchState.MaxEvents) return;
        ref var e = ref state.Events[state.EventCount++];
        e.Kind = kind;
        e.Slot = (sbyte)slot;
        e.Target = (sbyte)target;
        e.Value = value;
        e.Position = position;
        e.Frame = state.Frame;
    }
}
