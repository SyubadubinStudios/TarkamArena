using System.Numerics;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Engine.Assets;
using ThreeNet;

namespace TarkamArena.Engine.Audio;

public enum Sfx : byte
{
    HitLight,
    HitHeavy,
    Block,
    Parry,
    Whoosh,
    Jump,
    Land,
    Projectile,
    Clash,
    Ko,
    Bell,
    Tag,
    Rage,
    Grab,
    CrowdOoh,
    Finisher,
    Confetti,
    // Arena event sounds
    BassDrop,
    Siren,
    Motorbikes,
    Helicopter,
    Gavel,
    Camera,
    Fire,
    Splash,
    Chicken,
    Horn,
    Whistle,
}

/// <summary>
/// Every sound effect in the game, synthesised once at startup (about two seconds of audio each)
/// so the build needs no sample files. Recorded clips in <c>assets/audio/sfx/&lt;name&gt;.wav</c> override them.
/// </summary>
public static class SoundBank
{
    public static StereoBuffer Render(Sfx sfx)
    {
        var b = new StereoBuffer(Synth.SampleRate * (sfx is Sfx.Ko or Sfx.Finisher or Sfx.BassDrop or Sfx.Helicopter ? 3 : 1));
        switch (sfx)
        {
            case Sfx.HitLight:
                Synth.NoiseSweep(b, 0, 0.09f, 0.8f, 0.6f, 0.1f, 0.002f);
                Synth.Tone(b, 0, 180, 0.7f, 0.12f, 30f, 0, 90);
                break;
            case Sfx.HitHeavy:
                Synth.NoiseSweep(b, 0, 0.16f, 0.9f, 0.7f, 0.05f, 0.002f);
                Synth.Kick(b, 0, 0.9f, 0.8f);
                Synth.Tone(b, 0, 120, 0.6f, 0.25f, 14f, 0, 50);
                break;
            case Sfx.Block:
                Synth.Metallophone(b, 0, 820, 0.4f, 0.12f, 0, 40);
                Synth.NoiseSweep(b, 0, 0.06f, 0.5f, 0.5f, 0.2f, 0.002f);
                break;
            case Sfx.Parry:
                Synth.Metallophone(b, 0, 1568, 0.6f, 0.5f, 0.2f, 6);
                Synth.Metallophone(b, 0.02f, 2093, 0.4f, 0.4f, -0.2f, 6);
                break;
            case Sfx.Whoosh:
                Synth.NoiseSweep(b, 0, 0.22f, 0.5f, 0.05f, 0.4f, 0.08f);
                break;
            case Sfx.Jump:
                Synth.NoiseSweep(b, 0, 0.15f, 0.3f, 0.1f, 0.3f, 0.03f);
                break;
            case Sfx.Land:
                Synth.Kick(b, 0, 0.5f, 0.3f);
                Synth.NoiseSweep(b, 0, 0.12f, 0.3f, 0.2f, 0.02f, 0.002f);
                break;
            case Sfx.Projectile:
                Synth.Tone(b, 0, 300, 0.4f, 0.3f, 6f, 0, 900);
                Synth.NoiseSweep(b, 0, 0.3f, 0.3f, 0.1f, 0.5f, 0.05f);
                break;
            case Sfx.Clash:
                Synth.Metallophone(b, 0, 600, 0.6f, 0.3f, 0, 20);
                Synth.NoiseSweep(b, 0, 0.3f, 0.6f, 0.7f, 0.05f, 0.002f);
                break;
            case Sfx.Ko:
                Synth.Gong(b, 0, 62, 1f, 3f);
                Synth.Kick(b, 0, 1f, 1.2f);
                Synth.NoiseSweep(b, 0.1f, 2.5f, 0.35f, 0.02f, 0.08f, 0.4f);
                break;
            case Sfx.Bell:
                Synth.Metallophone(b, 0, 880, 0.6f, 0.6f, 0, 2);
                Synth.Metallophone(b, 0.18f, 880, 0.6f, 0.6f, 0, 2);
                break;
            case Sfx.Tag:
                Synth.Tone(b, 0, 660, 0.5f, 0.12f, 20f);
                Synth.Tone(b, 0.08f, 990, 0.5f, 0.15f, 20f);
                break;
            case Sfx.Rage:
                Synth.NoiseSweep(b, 0, 0.9f, 0.7f, 0.02f, 0.3f, 0.1f);
                Synth.Saw(b, 0, 55, 0.5f, 0.9f, 0, 0.03f, 4f);
                break;
            case Sfx.Grab:
                Synth.NoiseSweep(b, 0, 0.1f, 0.5f, 0.3f, 0.05f, 0.002f);
                Synth.Kick(b, 0.12f, 0.8f, 0.5f);
                break;
            case Sfx.CrowdOoh:
                for (int i = 0; i < 20; i++) Synth.Flute(b, i * 0.01f, 200 + i * 9, 0.08f, 0.8f, (i % 5 - 2) * 0.3f);
                Synth.NoiseSweep(b, 0, 0.8f, 0.2f, 0.02f, 0.05f, 0.15f);
                break;
            case Sfx.Finisher:
                Synth.Gong(b, 0, 49, 1f, 3f);
                Synth.Kendang(b, 0, true, 0.8f);
                Synth.Kendang(b, 0.25f, true, 0.8f);
                Synth.NoiseSweep(b, 0.3f, 2.4f, 0.5f, 0.02f, 0.2f, 0.6f);
                break;
            case Sfx.Confetti:
                for (int i = 0; i < 8; i++) Synth.Metallophone(b, i * 0.06f, 1046 + i * 130, 0.25f, 0.3f, (i % 3 - 1) * 0.5f, 5);
                break;
            case Sfx.BassDrop:
                Synth.Sub808(b, 0, 38, 1f, 2.6f);
                Synth.Kick(b, 0, 1f, 1.5f);
                break;
            case Sfx.Siren:
                for (int i = 0; i < 4; i++) Synth.Tone(b, i * 0.25f, 700, 0.4f, 0.25f, 0.5f, 0, 1100);
                break;
            case Sfx.Motorbikes:
                for (int i = 0; i < 5; i++) Synth.Saw(b, i * 0.08f, 70 + i * 9, 0.25f, 0.8f, (i - 2) * 0.3f, 0.06f, 3f);
                break;
            case Sfx.Helicopter:
                // Rotor thumps over a low wash.
                for (int i = 0; i < 18; i++) Synth.Kick(b, i * 0.15f, 0.55f, 0.15f);
                Synth.NoiseSweep(b, 0, 2.7f, 0.5f, 0.08f, 0.05f, 0.3f);
                break;
            case Sfx.Gavel:
                Synth.Kendang(b, 0, false, 0.9f);
                Synth.Kendang(b, 0.22f, false, 0.9f);
                break;
            case Sfx.Camera:
                for (int i = 0; i < 6; i++) Synth.NoiseSweep(b, i * 0.07f, 0.03f, 0.4f, 0.8f, 0.6f, 0.001f, (i % 3 - 1) * 0.6f);
                break;
            case Sfx.Fire:
                Synth.NoiseSweep(b, 0, 0.9f, 0.5f, 0.03f, 0.08f, 0.2f);
                for (int i = 0; i < 12; i++) Synth.NoiseSweep(b, i * 0.07f, 0.02f, 0.3f, 0.9f, 0.9f, 0.001f);
                break;
            case Sfx.Splash:
                Synth.NoiseSweep(b, 0, 0.6f, 0.7f, 0.5f, 0.03f, 0.01f);
                break;
            case Sfx.Chicken:
                for (int i = 0; i < 4; i++) Synth.Flute(b, i * 0.13f, 900 + i * 60, 0.35f, 0.1f, 0, reed: true);
                break;
            case Sfx.Horn:
                Synth.Saw(b, 0, 311, 0.35f, 0.5f, 0, 0.3f, 2f);
                Synth.Saw(b, 0, 392, 0.3f, 0.5f, 0, 0.3f, 2f);
                break;
            case Sfx.Whistle:
                for (int i = 0; i < 2; i++) Synth.Tone(b, i * 0.25f, 2800, 0.35f, 0.2f, 2f, 0, 3000);
                break;
        }
        return b;
    }

    /// <summary>A crowd bed that loops; its volume follows the crowd excitement meter.</summary>
    public static StereoBuffer CrowdLoop()
    {
        var b = new StereoBuffer(Synth.SampleRate * 6) { Wrap = true };
        var rng = new Random(77);
        for (int i = 0; i < 60; i++)
            Synth.NoiseSweep(b, (float)rng.NextDouble() * 6f, 0.6f + (float)rng.NextDouble(), 0.12f, 0.02f, 0.06f, 0.3f, (float)rng.NextDouble() * 2 - 1);
        for (int i = 0; i < 24; i++)
            Synth.Flute(b, (float)rng.NextDouble() * 6f, 180 + (float)rng.NextDouble() * 220, 0.03f, 0.5f + (float)rng.NextDouble(), (float)rng.NextDouble() * 2 - 1);
        return b;
    }
}

/// <summary>Announcer lines. Voice files come from the ElevenLabs step of the asset pipeline.</summary>
public enum VoiceLine : byte
{
    RoundOne,
    RoundTwo,
    FinalRound,
    Fight,
    Ko,
    FinishThem,
    Fatality,
    Brutality,
    Friendship,
    Mercy,
    Legendary,
    TimeUp,
    Perfect,
}

/// <summary>
/// Plays music, effects, the crowd bed and announcer lines for a match. All audio is optional:
/// with no output device the director silently does nothing.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private readonly AudioEngine? _engine;
    private readonly Dictionary<Sfx, AudioClip> _sfx = [];
    private readonly Dictionary<VoiceLine, AudioClip> _voice = [];
    private readonly Random _random = new(3);
    private SoundInstance? _music;
    private SoundInstance? _crowd;
    private float _musicVolume = 0.7f, _sfxVolume = 0.9f;

    public bool Available => _engine is not null;

    public AudioDirector()
    {
        try
        {
            _engine = AudioEngine.Open();
        }
        catch (Exception e) when (e is ThreeNetException or DllNotFoundException or InvalidOperationException)
        {
            _engine = null;
        }
        if (_engine is null) return;

        foreach (var sfx in Enum.GetValues<Sfx>())
        {
            var file = AssetLocator.PathOf($"audio/sfx/{sfx.ToString().ToLowerInvariant()}.wav");
            _sfx[sfx] = File.Exists(file) ? _engine.LoadClip(file) : Create(SoundBank.Render(sfx));
        }
        foreach (var line in Enum.GetValues<VoiceLine>())
        {
            foreach (var ext in new[] { "mp3", "wav", "ogg" })
            {
                var file = AssetLocator.PathOf($"audio/voice/{line.ToString().ToLowerInvariant()}.{ext}");
                if (!File.Exists(file)) continue;
                try { _voice[line] = _engine.LoadClip(file); } catch (ThreeNetException) { }
                break;
            }
        }
    }

    private AudioClip Create(StereoBuffer buffer) => _engine!.CreateClip(buffer.ToInterleaved(), 2, buffer.SampleRate);

    public void SetVolumes(float master, float music, float sfx)
    {
        if (_engine is null) return;
        _engine.MasterGain = master;
        _musicVolume = music;
        _sfxVolume = sfx;
        _music?.Update(o => o with { Gain = music * 0.55f });
    }

    /// <summary>Renders the stage's loop on a worker thread (it takes a moment) and starts it.</summary>
    public Task StartMusicAsync(MusicStyle style, int seed)
    {
        if (_engine is null) return Task.CompletedTask;
        return Task.Run(() =>
        {
            var buffer = MusicGenerator.Compose(style, seed);
            var clip = Create(buffer);
            _music?.Stop();
            var options = SoundOptions.Flat(_musicVolume * 0.55f, loop: true);
            _music = _engine.Play(clip, in options);
            var crowdOptions = SoundOptions.Flat(0.15f, loop: true);
            _crowd ??= _engine.Play(Create(SoundBank.CrowdLoop()), in crowdOptions);
        });
    }

    public void StopMusic()
    {
        _music?.Stop();
        _music = null;
        _crowd?.Stop();
        _crowd = null;
    }

    public void SetCrowd(float excitement01) => _crowd?.Update(o => o with { Gain = (0.08f + excitement01 * 0.55f) * _sfxVolume });

    public void Play(Sfx sfx, float gain = 1f, float pitchJitter = 0.06f, float pan = 0f)
    {
        if (_engine is null || !_sfx.TryGetValue(sfx, out var clip)) return;
        var options = SoundOptions.Flat(gain * _sfxVolume);
        options.Pitch = 1f + ((float)_random.NextDouble() * 2 - 1) * pitchJitter;
        if (pan != 0)
        {
            options.Spatial = true;
            options.Position = new Vector3(pan * 6f, 1, 0);
        }
        _engine.Play(clip, in options);
    }

    public bool Say(VoiceLine line, float gain = 1f)
    {
        if (_engine is null || !_voice.TryGetValue(line, out var clip)) return false;
        var options = SoundOptions.Flat(gain * _sfxVolume);
        _engine.Play(clip, in options);
        return true;
    }

    /// <summary>Turns simulation events into sound.</summary>
    public void OnEvents(in MatchState state, MatchContext context)
    {
        foreach (ref readonly var e in state.CurrentEvents)
        {
            float pan = Math.Clamp(e.Position.X.ToFloat() / 8f, -1, 1);
            switch (e.Kind)
            {
                case MatchEventKind.Hit:
                    Play(e.Value > 60 ? Sfx.HitHeavy : Sfx.HitLight, 0.9f, 0.08f, pan);
                    break;
                case MatchEventKind.CounterHit:
                    Play(Sfx.HitHeavy, 1f, 0.05f, pan);
                    Play(Sfx.Parry, 0.4f);
                    break;
                case MatchEventKind.Block: Play(Sfx.Block, 0.8f, 0.08f, pan); break;
                case MatchEventKind.Parry: Play(Sfx.Parry, 1f, 0.02f, pan); break;
                case MatchEventKind.Whiff: Play(Sfx.Whoosh, 0.4f, 0.15f, pan); break;
                case MatchEventKind.Jump: Play(Sfx.Jump, 0.35f, 0.1f, pan); break;
                case MatchEventKind.Land: Play(Sfx.Land, 0.3f, 0.1f, pan); break;
                case MatchEventKind.Knockdown: Play(Sfx.Land, 0.9f, 0.05f, pan); break;
                case MatchEventKind.ProjectileSpawn: Play(Sfx.Projectile, 0.6f, 0.1f, pan); break;
                case MatchEventKind.ProjectileClash: Play(Sfx.Clash, 0.8f); break;
                case MatchEventKind.Tech: Play(Sfx.Clash, 0.6f); break;
                case MatchEventKind.Launch: Play(Sfx.Whoosh, 0.7f, 0.05f, pan); Play(Sfx.CrowdOoh, 0.4f); break;
                case MatchEventKind.TagSwitch: Play(Sfx.Tag, 0.8f); break;
                case MatchEventKind.TeamCombo: Play(Sfx.Confetti, 0.5f); break;
                case MatchEventKind.RageActivated: Play(Sfx.Rage, 1f, 0.02f, pan); break;
                case MatchEventKind.AdrenalineActivated: Play(Sfx.Tag, 0.6f, 0.2f); break;
                case MatchEventKind.Combo when e.Value >= 5: Play(Sfx.CrowdOoh, 0.6f); break;
                case MatchEventKind.Dodge: Play(Sfx.Whoosh, 0.45f, 0.1f, pan); break;
                case MatchEventKind.KO:
                    Play(Sfx.Ko, 1f, 0);
                    Say(VoiceLine.Ko);
                    break;
                case MatchEventKind.RoundStart:
                    Play(Sfx.Bell, 0.8f, 0);
                    bool final = false;
                    for (int i = 0; i < state.FighterCount; i++)
                        if (context.Rules.RoundsToWin > 1 && state.RoundWins[state.Fighters[i].Team] == context.Rules.RoundsToWin - 1) final = true;
                    Say(final ? VoiceLine.FinalRound : e.Value <= 1 ? VoiceLine.RoundOne : VoiceLine.RoundTwo);
                    break;
                case MatchEventKind.Fight: Say(VoiceLine.Fight); Play(Sfx.Whistle, 0.4f, 0); break;
                case MatchEventKind.TimeUp: Say(VoiceLine.TimeUp); Play(Sfx.Bell, 0.8f, 0); break;
                case MatchEventKind.FinisherPrompt: Say(VoiceLine.FinishThem); Play(Sfx.CrowdOoh, 0.8f); break;
                case MatchEventKind.FinisherStart:
                    var kind = context.Finishers[e.Slot][e.Value].Kind;
                    Play(kind == FinisherKind.Friendship ? Sfx.Confetti : Sfx.Finisher, 1f, 0);
                    Say(kind switch
                    {
                        FinisherKind.Fatality => VoiceLine.Fatality,
                        FinisherKind.Brutality => VoiceLine.Brutality,
                        FinisherKind.Friendship => VoiceLine.Friendship,
                        FinisherKind.Mercy => VoiceLine.Mercy,
                        _ => VoiceLine.Legendary,
                    });
                    break;
                case MatchEventKind.Mercy: Say(VoiceLine.Mercy); Play(Sfx.Confetti, 0.8f); break;
                case MatchEventKind.HazardHit: Play(Sfx.HitHeavy, 0.8f, 0.05f, pan); break;
                case MatchEventKind.ArenaEvent:
                    var ev = context.Stage.Events[e.Value].Kind;
                    Sfx? s = ev switch
                    {
                        ArenaEventKind.BassDrop => Sfx.BassDrop,
                        ArenaEventKind.WaterCannon => Sfx.Siren,
                        ArenaEventKind.TearGas => Sfx.Siren,
                        ArenaEventKind.MotorbikeSwarm => Sfx.Motorbikes,
                        ArenaEventKind.WaterBomb => Sfx.Helicopter,
                        ArenaEventKind.FireSpread or ArenaEventKind.Eruption => Sfx.Fire,
                        ArenaEventKind.BigWave or ArenaEventKind.Downpour => Sfx.Splash,
                        ArenaEventKind.LooseChicken => Sfx.Chicken,
                        ArenaEventKind.Fireworks => Sfx.Camera,
                        ArenaEventKind.BusArrival or ArenaEventKind.PassingCart or ArenaEventKind.TourBus or ArenaEventKind.Procession => Sfx.Horn,
                        ArenaEventKind.Protest => Sfx.Whistle,
                        ArenaEventKind.RagingGoat => Sfx.CrowdOoh,
                        _ => null,
                    };
                    if (context.Stage.Id == "ruang-sidang") s = Sfx.Gavel;
                    if (s is { } sound) Play(sound, 0.9f, 0);
                    break;
            }
        }
    }

    public void Update(Scene scene, Node listener, float dt) => _engine?.Update(scene, listener, dt);

    public void Dispose()
    {
        StopMusic();
        _engine?.Dispose();
    }
}
