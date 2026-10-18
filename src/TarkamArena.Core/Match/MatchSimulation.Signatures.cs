using TarkamArena.Core.Combat;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Match;

/// <summary>
/// Signature moves ("Jurus Pamungkas"): input detection, the super freeze, and the deterministic
/// behaviour of every <see cref="SignatureKind"/>. Effects live in <see cref="MatchState.Summons"/>,
/// so they roll back with the rest of the match.
/// </summary>
public static partial class MatchSimulation
{
    public const int SignatureFreezeFrames = 28;

    private static readonly Fixed s_liftHeight = Fixed.FromFloat(2.8f);
    private static readonly Fixed s_liftSpeed = Fixed.FromFloat(0.12f);

    /// <summary>Damage multiplier per signature slot: the dearer the move, the harder it hits.</summary>
    private static int SlotPower(int slot) => slot switch { 0 => 100, 1 => 130, _ => 170 };

    /// <summary>
    /// Checks the motion before a Special press and, when the meter allows, starts that signature:
    /// ↓↓+S (slot 2), ↓↘→+S (slot 0), ↓↙←+S (slot 1).
    /// </summary>
    private static bool TrySignature(ref MatchState state, MatchContext context, int slot)
    {
        ref var f = ref state.Fighters[slot];
        int sig = DetectDoubleDown(f) ? 2 : DetectQuarterCircleForward(f) ? 0 : DetectQuarterCircleBack(f) ? 1 : -1;
        if (sig < 0) return false;
        int cost = SignatureMove.Costs[sig];
        if (f.Jurus < cost) return false;

        f.Jurus -= cost;
        f.SignatureSlot = (byte)sig;
        StartMove(ref state, context, slot, MoveId.Signature);

        // Super freeze: everyone else stops while the cut-in plays.
        for (int i = 0; i < state.FighterCount; i++)
        {
            if (i == slot) continue;
            ref var o = ref state.Fighters[i];
            if (o.IsActive) o.Hitstop = Math.Max(o.Hitstop, SignatureFreezeFrames);
        }
        AddCrowd(ref state, context, 90);
        Emit(ref state, MatchEventKind.SignatureStart, slot, f.Target, (short)sig, f.Position);
        return true;
    }

    private static bool DetectQuarterCircleBack(in FighterState f)
    {
        InputButtons back = f.Facing >= 0 ? InputButtons.Left : InputButtons.Right;
        int stage = 0;
        for (int i = 14; i >= 0; i--)
        {
            var b = f.HistoryAt(i) & InputButtons.Directions;
            switch (stage)
            {
                case 0 when b == InputButtons.Down: stage = 1; break;
                case 1 when b == (InputButtons.Down | back): stage = 2; break;
                case 2 when b == back: return true;
            }
        }
        return false;
    }

    /// <summary>Down, released, down again (within 18 frames), with Down held on the Special press.</summary>
    private static bool DetectDoubleDown(in FighterState f)
    {
        if ((f.HistoryAt(0) & InputButtons.Down) == 0) return false;
        int stage = 0;
        for (int i = 1; i <= 18; i++)
        {
            bool down = (f.HistoryAt(i) & InputButtons.Down) != 0;
            if (stage == 0 && !down) stage = 1;
            else if (stage == 1 && down) return true;
        }
        return false;
    }

    private static int FreeSummon(ref MatchState state)
    {
        for (int i = 0; i < MatchState.MaxSummons; i++)
            if (!state.Summons[i].Active) return i;
        return -1;
    }

    private static void SpawnSignature(ref MatchState state, MatchContext context, int slot)
    {
        ref readonly var f = ref state.Fighters[slot];
        var sig = context.Signatures[slot][f.SignatureSlot];
        int index = FreeSummon(ref state);
        if (index < 0) return;
        int power = SlotPower(f.SignatureSlot);
        int facing = f.Facing >= 0 ? 1 : -1;
        Fixed fx = Fixed.FromInt(facing);
        sbyte target = f.Target;
        Fixed targetX = target >= 0 ? state.Fighters[target].Position.X : f.Position.X + Fixed.FromInt(3) * facing;

        var s = new Summon
        {
            Active = true,
            Kind = sig.Kind,
            Slot = f.SignatureSlot,
            Owner = (sbyte)slot,
            Team = f.Team,
            Target = target,
            Facing = (sbyte)facing,
        };

        switch (sig.Kind)
        {
            case SignatureKind.Summon:
                s.Position = new FixedVector2(f.Position.X - Fixed.FromFloat(0.6f) * facing, f.Position.Y + Fixed.FromFloat(1.4f));
                s.Size = FixedVector2.Of(0.9f, 0.9f);
                s.Life = 180;
                s.MaxHits = (byte)(f.SignatureSlot == 2 ? 6 : 4);
                s.Damage = (short)(30 * power / 100);
                break;
            case SignatureKind.Crowd:
                s.Position = new FixedVector2(f.Position.X - Fixed.FromFloat(1.4f) * facing, Fixed.Zero);
                s.Velocity = new FixedVector2(Fixed.FromFloat(0.17f) * fx, Fixed.Zero);
                s.Size = FixedVector2.Of(2.0f, 1.9f);
                s.Life = 150;
                s.Damage = (short)(75 * power / 100);
                break;
            case SignatureKind.Weapon:
                s.Position = new FixedVector2(f.Position.X + Fixed.FromFloat(1.4f) * facing, Fixed.Zero);
                s.Size = FixedVector2.Of(3.0f, 2.0f);
                s.Life = 24;
                s.Damage = (short)(110 * power / 100);
                break;
            case SignatureKind.Storm:
                s.Position = new FixedVector2(targetX, Fixed.Zero);
                s.Size = FixedVector2.Of(1.6f, 7f);
                s.Life = (short)(f.SignatureSlot == 2 ? 124 : 102);
                s.Damage = (short)(42 * power / 100);
                break;
            case SignatureKind.Levitate:
                s.Position = f.Position;
                s.Life = 100;
                s.Damage = (short)(130 * power / 100);
                s.Target = PickLiftTarget(ref state, context, slot);
                if (s.Target < 0) s.Life = 24; // nobody in range: the effect fizzles visibly
                break;
            case SignatureKind.Tornado:
                s.Position = new FixedVector2(f.Position.X + Fixed.FromFloat(1.2f) * facing, Fixed.Zero);
                s.Velocity = new FixedVector2(Fixed.FromFloat(0.075f) * fx, Fixed.Zero);
                s.Size = FixedVector2.Of(1.7f, 3.4f);
                s.Life = 160;
                s.MaxHits = 8;
                s.Damage = (short)(16 * power / 100);
                break;
            case SignatureKind.Barrage:
                s.Position = f.Position;
                s.MaxHits = (byte)(f.SignatureSlot == 2 ? 9 : 6);
                s.Life = (short)(s.MaxHits * 6 + 2);
                s.Damage = (short)(26 * power / 100);
                break;
            case SignatureKind.Quake:
                s.Position = f.Position;
                s.Life = 70;
                s.Damage = (short)(85 * power / 100);
                break;
            case SignatureKind.Wave:
                s.Position = new FixedVector2(f.Position.X + Fixed.FromFloat(0.8f) * facing, Fixed.Zero);
                s.Velocity = new FixedVector2(Fixed.FromFloat(0.13f) * fx, Fixed.Zero);
                s.Size = FixedVector2.Of(1.9f, 2.6f);
                s.Life = 130;
                s.Damage = (short)(65 * power / 100);
                break;
        }
        state.Summons[index] = s;
    }

    private static sbyte PickLiftTarget(ref MatchState state, MatchContext context, int slot)
    {
        ref readonly var f = ref state.Fighters[slot];
        int best = -1;
        Fixed bestDistance = Fixed.FromFloat(7.5f);
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref readonly var o = ref state.Fighters[i];
            if (i == slot || !o.IsActive || o.Team == f.Team || !o.IsAlive || o.Invulnerable > 0 || o.Status is FighterStatus.Benched or FighterStatus.KO) continue;
            Fixed d = Fixed.Abs(o.Position.X - f.Position.X);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return (sbyte)best;
    }

    private static void StepSummons(ref MatchState state, MatchContext context)
    {
        for (int i = 0; i < MatchState.MaxSummons; i++)
        {
            ref var s = ref state.Summons[i];
            if (!s.Active) continue;
            s.Age++;
            if (s.HitTimer > 0) s.HitTimer--;
            if (s.Owner < 0 || s.Owner >= state.FighterCount)
            {
                s.Active = false;
                continue;
            }

            switch (s.Kind)
            {
                case SignatureKind.Summon: StepHelper(ref state, context, ref s); break;
                case SignatureKind.Crowd:
                case SignatureKind.Wave: StepSweep(ref state, context, ref s, i); break;
                case SignatureKind.Weapon: StepWeapon(ref state, context, ref s); break;
                case SignatureKind.Storm: StepStorm(ref state, context, ref s, i); break;
                case SignatureKind.Levitate: StepLift(ref state, context, ref s, i); break;
                case SignatureKind.Tornado: StepTornado(ref state, context, ref s); break;
                case SignatureKind.Barrage: StepBarrage(ref state, ref s); break;
                case SignatureKind.Quake: StepQuake(ref state, context, ref s, i); break;
            }

            if (--s.Life <= 0) s.Active = false;
        }
    }

    private static FixedBox SummonBox(in Summon s) =>
        new(s.Position.X - s.Size.X / 2, s.Position.Y, s.Position.X + s.Size.X / 2, s.Position.Y + s.Size.Y);

    private static bool IsEnemy(in MatchState state, in Summon s, int j)
    {
        ref readonly var v = ref state.Fighters[j];
        return v.IsActive && v.Team != s.Team && CanBeHit(v);
    }

    private static void SummonHit(ref MatchState state, MatchContext context, in Summon s, int victim, int damage, Fixed launch, bool knockDown, bool unblockable)
    {
        var move = new MoveData
        {
            Id = MoveId.Signature,
            Damage = (short)damage,
            ChipDamage = (short)(damage / 6),
            Hitstun = 26,
            Blockstun = 16,
            Hitstop = 7,
            Pushback = Fixed.FromFloat(0.12f),
            LaunchVelocity = launch,
            Flags = (knockDown ? MoveFlags.KnockDown : MoveFlags.None) | (unblockable ? MoveFlags.Unblockable : MoveFlags.None),
        };
        ref readonly var v = ref state.Fighters[victim];
        ApplyHit(ref state, context, s.Owner, victim, in move, isProjectile: true, v.Position.X, v.Position.Y + Fixed.One, freezeAttacker: false);
    }

    /// <summary>Summoned helpers fly at their target and strike every 18 frames.</summary>
    private static void StepHelper(ref MatchState state, MatchContext context, ref Summon s)
    {
        if (s.Target < 0 || !state.Fighters[s.Target].IsActive || !state.Fighters[s.Target].IsAlive)
        {
            s.Target = state.Fighters[s.Owner].Target;
            if (s.Target < 0) return;
        }
        ref readonly var t = ref state.Fighters[s.Target];
        var goal = new FixedVector2(t.Position.X, t.Position.Y + Fixed.FromFloat(1.0f));
        Fixed speed = Fixed.FromFloat(0.13f);
        s.Velocity = new FixedVector2(Fixed.Clamp(goal.X - s.Position.X, -speed, speed), Fixed.Clamp(goal.Y - s.Position.Y, -speed, speed));
        s.Position += s.Velocity;
        if (s.HitTimer > 0 || s.Hits >= s.MaxHits || !IsEnemy(state, s, s.Target)) return;
        var box = new FixedBox(s.Position.X - s.Size.X / 2, s.Position.Y - s.Size.Y / 2, s.Position.X + s.Size.X / 2, s.Position.Y + s.Size.Y / 2);
        if (!box.Overlaps(HurtBox(context, t))) return;
        s.Hits++;
        s.HitTimer = 18;
        SummonHit(ref state, context, s, s.Target, s.Damage, s.Hits == s.MaxHits ? Fixed.FromFloat(0.2f) : Fixed.Zero, s.Hits == s.MaxHits, unblockable: false);
        if (s.Hits >= s.MaxHits) s.Life = Math.Min(s.Life, (short)20);
    }

    /// <summary>Crowds and waves sweep across the arena and hit each enemy once.</summary>
    private static void StepSweep(ref MatchState state, MatchContext context, ref Summon s, int index)
    {
        s.Position += s.Velocity;
        var box = SummonBox(s);
        for (int j = 0; j < state.FighterCount; j++)
        {
            if (s.HasHit(j) || !IsEnemy(state, s, j)) continue;
            ref var v = ref state.Fighters[j];
            if (!box.Overlaps(HurtBox(context, v))) continue;
            s.MarkHit(j);
            bool wave = s.Kind == SignatureKind.Wave;
            SummonHit(ref state, context, s, j, s.Damage, Fixed.FromFloat(wave ? 0.2f : 0.26f), knockDown: true, unblockable: !wave);
            if (wave) state.Fighters[j].Velocity.X += Fixed.FromFloat(0.18f) * s.Facing;
            Emit(ref state, MatchEventKind.SignatureImpact, s.Owner, (sbyte)j, (short)index, v.Position);
        }
        if (Fixed.Abs(s.Position.X) > context.HalfWidth + Fixed.FromInt(3)) s.Life = 0;
    }

    /// <summary>The giant weapon: the arc is live from frame 4 to 14 of the swing.</summary>
    private static void StepWeapon(ref MatchState state, MatchContext context, ref Summon s)
    {
        ref readonly var owner = ref state.Fighters[s.Owner];
        s.Position = new FixedVector2(owner.Position.X + Fixed.FromFloat(1.4f) * s.Facing, owner.Position.Y);
        if (s.Age < 4 || s.Age > 14) return;
        var box = SummonBox(s);
        for (int j = 0; j < state.FighterCount; j++)
        {
            if (s.HasHit(j) || !IsEnemy(state, s, j)) continue;
            if (!box.Overlaps(HurtBox(context, state.Fighters[j]))) continue;
            s.MarkHit(j);
            SummonHit(ref state, context, s, j, s.Damage, Fixed.FromFloat(0.3f), knockDown: true, unblockable: false);
        }
    }

    /// <summary>Weather strikes: a warning, then bolts at the target's position (re-aimed before each strike).</summary>
    private static void StepStorm(ref MatchState state, MatchContext context, ref Summon s, int index)
    {
        bool strike = s.Age is 34 or 56 or 78 or 100;
        if (s.Age > 100 && s.Slot != 2) return;
        if (s.Target >= 0 && !strike && (s.Age is 30 or 52 or 74 or 96))
            s.Position = new FixedVector2(state.Fighters[s.Target].Position.X, Fixed.Zero);
        if (!strike || (s.Age == 100 && s.Slot != 2)) return;
        s.Phase++;
        var box = SummonBox(s);
        for (int j = 0; j < state.FighterCount; j++)
        {
            if (!IsEnemy(state, s, j)) continue;
            if (!box.Overlaps(HurtBox(context, state.Fighters[j]))) continue;
            bool last = s.Age >= 78;
            SummonHit(ref state, context, s, j, s.Damage, last ? Fixed.FromFloat(0.24f) : Fixed.Zero, last, unblockable: false);
        }
        Emit(ref state, MatchEventKind.SignatureImpact, s.Owner, -1, (short)index, s.Position);
    }

    /// <summary>Lift and slam: the target rises and hangs in the air, then is driven into the floor.</summary>
    private static void StepLift(ref MatchState state, MatchContext context, ref Summon s, int index)
    {
        if (s.Target < 0) return;
        ref var v = ref state.Fighters[s.Target];
        if (!v.IsActive || v.Status is FighterStatus.Benched)
        {
            s.Life = 0;
            return;
        }
        if (s.Phase == 0)
        {
            // Rise and hold.
            v.Status = FighterStatus.Juggle;
            v.JuggleCount = MaxJuggle;
            v.Grounded = false;
            v.Move = MoveId.None;
            v.Velocity = default;
            v.Position.Y = Fixed.Min(s_liftHeight, v.Position.Y + s_liftSpeed);
            s.Position = v.Position;
            if (s.Age >= 46)
            {
                s.Phase = 1;
                v.Velocity = new FixedVector2(Fixed.Zero, Fixed.FromFloat(-0.6f));
            }
        }
        else if (s.Phase == 1)
        {
            s.Position = v.Position;
            if (v.Grounded)
            {
                s.Phase = 2;
                v.Invulnerable = 0;
                v.Status = FighterStatus.Idle; // so the slam can connect
                SummonHit(ref state, context, s, s.Target, s.Damage, Fixed.FromFloat(0.16f), knockDown: true, unblockable: true);
                Emit(ref state, MatchEventKind.SignatureImpact, s.Owner, s.Target, (short)index, v.Position);
                s.Life = Math.Min(s.Life, (short)18);
            }
        }
    }

    /// <summary>A travelling whirlwind: multi-hit, pulls victims towards its centre and juggles them.</summary>
    private static void StepTornado(ref MatchState state, MatchContext context, ref Summon s)
    {
        s.Position += s.Velocity;
        if ((s.Age % 10) != 0 || s.Hits >= s.MaxHits) return;
        var box = SummonBox(s);
        bool any = false;
        for (int j = 0; j < state.FighterCount; j++)
        {
            ref var v = ref state.Fighters[j];
            if (!v.IsActive || v.Team == s.Team || v.Status is FighterStatus.Benched or FighterStatus.KO) continue;
            if (!box.Overlaps(HurtBox(context, v))) continue;
            v.Position.X = Fixed.Approach(v.Position.X, s.Position.X, Fixed.FromFloat(0.25f));
            if (!CanBeHit(v) && v.Status != FighterStatus.Juggle) continue;
            v.JuggleCount = Math.Min(v.JuggleCount, MaxJuggle - 1);
            SummonHit(ref state, context, s, j, s.Damage, Fixed.FromFloat(0.14f), knockDown: false, unblockable: true);
            any = true;
        }
        if (any) s.Hits++;
        if (Fixed.Abs(s.Position.X) > context.HalfWidth + Fixed.One) s.Life = 0;
    }

    /// <summary>A barrage emits one signature projectile every six frames.</summary>
    private static void StepBarrage(ref MatchState state, ref Summon s)
    {
        if ((s.Age % 6) != 1 || s.Hits >= s.MaxHits) return;
        ref readonly var f = ref state.Fighters[s.Owner];
        int lane = s.Hits % 3;
        for (int i = 0; i < MatchState.MaxProjectiles; i++)
        {
            ref var p = ref state.Projectiles[i];
            if (p.Active) continue;
            p = new Projectile
            {
                Active = true,
                Owner = s.Owner,
                Team = s.Team,
                Position = new FixedVector2(f.Position.X + Fixed.FromFloat(0.6f) * s.Facing, f.Position.Y + Fixed.FromFloat(lane == 0 ? 1.3f : lane == 1 ? 0.8f : 1.7f)),
                VelocityX = Fixed.FromFloat(0.2f) * s.Facing,
                Life = 70,
                Damage = s.Damage,
                Chip = (short)(s.Damage / 5),
                Hitstun = 16,
                Blockstun = 10,
                Size = FixedVector2.Of(0.4f, 0.4f),
                Signature = (byte)(1 + s.Slot),
            };
            Emit(ref state, MatchEventKind.ProjectileSpawn, s.Owner, (sbyte)i, 0, p.Position);
            break;
        }
        s.Hits++;
    }

    /// <summary>A quake front spreads from the caster in both directions, hitting grounded enemies once.</summary>
    private static void StepQuake(ref MatchState state, MatchContext context, ref Summon s, int index)
    {
        Fixed radius = Fixed.FromFloat(0.18f) * s.Age;
        s.Size = new FixedVector2(radius * 2, Fixed.FromFloat(0.6f));
        if (s.Age == 1) Emit(ref state, MatchEventKind.SignatureImpact, s.Owner, -1, (short)index, s.Position);
        for (int j = 0; j < state.FighterCount; j++)
        {
            if (s.HasHit(j) || !IsEnemy(state, s, j)) continue;
            ref readonly var v = ref state.Fighters[j];
            if (!v.Grounded || Fixed.Abs(v.Position.X - s.Position.X) > radius) continue;
            s.MarkHit(j);
            SummonHit(ref state, context, s, j, s.Damage, Fixed.FromFloat(0.26f), knockDown: true, unblockable: true);
        }
    }
}
