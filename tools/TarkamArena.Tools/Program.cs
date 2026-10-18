using System.Numerics;
using TarkamArena.Core.AI;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Engine.Assets;
using TarkamArena.Engine.Audio;
using TarkamArena.Engine.Rendering;
using ThreeNet;

// Tarkam Arena asset & screenshot baker.
//   dotnet run --project tools/TarkamArena.Tools -- stages            -> assets/generated/stages/<id>.png
//   dotnet run --project tools/TarkamArena.Tools -- portraits [--all] -> assets/generated/portraits/<id>.png (procedural/glTF render)
//   dotnet run --project tools/TarkamArena.Tools -- scene <stage> <p1> <p2> <frames> <out.png> [mode]
//   dotnet run --project tools/TarkamArena.Tools -- music <style> <out.wav>
//   dotnet run --project tools/TarkamArena.Tools -- signatures <outDir> [id,id,...] -> one frame per signature move
ThreeNetRuntime.Initialize();
var command = args.Length > 0 ? args[0] : "help";
switch (command)
{
    case "stages":
        foreach (var stage in Stages.All)
        {
            var path = AssetLocator.PathOf(stage.ThumbnailPath);
            RenderScene(stage, ["pesilat", "preman-pasar"], 150, 1280, 720, path, GameMode.Versus, arenaCamera: true);
            Console.WriteLine($"stage {stage.Id} -> {path}");
        }
        break;

    case "portraits":
        bool all = args.Contains("--all");
        foreach (var c in Roster.All)
        {
            var path = AssetLocator.PathOf(c.PortraitPath);
            if (!all && AssetLocator.Exists($"generated/previews/{c.Id}.png")) continue;
            RenderPortrait(c, path);
            Console.WriteLine($"portrait {c.Id} -> {path}");
        }
        break;

    case "previews":
    {
        // Transparent idle portraits through the game's own glTF path (what players see), not Blender.
        string output = args.Length > 1 ? args[1] : AssetLocator.PathOf("generated/previews");
        var selected = args.Length > 2 && args[2] is not ("" or "-") ? args[2].Split(',').ToHashSet(StringComparer.Ordinal) : null;
        Directory.CreateDirectory(output);
        var options = OffscreenCapture.Options(512, 640, high: true);
        options.Vignette = 0;
        options.Bloom = false;
        using var renderer = Renderer.CreateOffscreen(options);
        using var previewScene = CreatePreviewScene();
        // Optional 4th argument: a folder of alternative glbs (e.g. a staging folder) to compare.
        string? models = args.Length > 3 && args[3] != "-" ? args[3] : null;
        string pose = args.Length > 4 ? args[4] : "idle";
        foreach (var c in Roster.All.Where(c => (selected is null || selected.Contains(c.Id)) && AssetLocator.Exists(c.ModelPath)))
        {
            var path = Path.Combine(output, $"{c.Id}.png");
            RenderPreview(renderer, previewScene, c, path, models is null ? null : Path.Combine(models, $"{c.Id}.glb"), pose);
            Console.WriteLine($"preview {c.Id} -> {path}");
        }
        break;
    }

    case "characters":
    {
        string output = args.Length > 1 ? args[1] : "docs/screenshots/characters";
        var selected = args.Length > 2 ? args[2].Split(',').ToHashSet(StringComparer.Ordinal) : null;
        Directory.CreateDirectory(output);
        var report = new List<object>();
        // Keep scene resource handles unique for the lifetime of the renderer.
        using var scene = CreateCharacterAuditScene();
        using var renderer = Renderer.CreateOffscreen(OffscreenCapture.Options(480, 600, high: false));
        foreach (var character in Roster.All.Where(c => selected is null || selected.Contains(c.Id)))
            report.Add(RenderCharacterAudit(scene, renderer, character, output));
        File.WriteAllText(Path.Combine(output, "manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        break;
    }

    case "scene":
        var mode = args.Length > 6 ? Enum.Parse<GameMode>(args[6]) : GameMode.Versus;
        string[] ids = args[2].Contains(',') ? args[2].Split(',') : [args[2], args[3]];
        RenderScene(Stages.Get(args[1]), ids, int.Parse(args[4]), 1600, 900, args[5], mode, arenaCamera: false);
        Console.WriteLine($"scene -> {args[5]}");
        break;

    case "signatures":
    {
        string outDir = args.Length > 1 ? args[1] : "docs/screenshots/signatures";
        var who = args.Length > 2 ? args[2].Split(',') : [.. Roster.All.Where(c => SignatureCatalog.IsHandAuthored(c.Id)).Select(c => c.Id)];
        Directory.CreateDirectory(outDir);
        for (int n = 0; n < who.Length; n++)
        {
            var stage = Stages.All[n % Stages.All.Count];
            string foe = who[n] == "preman-pasar" ? "pesilat" : "preman-pasar";
            for (int slot = 0; slot < 3; slot++)
            {
                var path = Path.Combine(outDir, $"{who[n]}-{slot}.png");
                // A fresh renderer per scene: the offscreen renderer caches resources by scene handle,
                // and handles are reused once a presenter is disposed.
                using (var renderer = Renderer.CreateOffscreen(OffscreenCapture.Options(960, 540, high: false)))
                    RenderSignature(renderer, stage, who[n], foe, slot, path);
                Console.WriteLine($"signature {who[n]} #{slot} -> {path}");
            }
        }
        break;
    }

    case "music":
        var buffer = MusicGenerator.Compose(Enum.Parse<MusicStyle>(args[1]), 7);
        WriteWav(args[2], buffer.ToInterleaved(), buffer.SampleRate);
        Console.WriteLine($"music -> {args[2]} ({buffer.Frames / (float)buffer.SampleRate:0.0}s)");
        break;

    case "docs":
        foreach (var lang in new[] { TarkamArena.Core.Localization.Language.English, TarkamArena.Core.Localization.Language.Indonesian })
        {
            string dir = lang == TarkamArena.Core.Localization.Language.English ? "en" : "id";
            bool id = dir == "id";
            var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", dir);
            Directory.CreateDirectory(root);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(id ? "# Daftar Petarung" : "# Fighter Roster");
            sb.AppendLine();
            sb.AppendLine(id ? "Dihasilkan otomatis dari `Roster.cs` dengan `dotnet run --project tools/TarkamArena.Tools -- docs`." : "Generated from `Roster.cs` with `dotnet run --project tools/TarkamArena.Tools -- docs`.");
            sb.AppendLine();
            foreach (var group in Roster.All.GroupBy(c => c.Dlc != DlcPack.None ? "DLC" : c.Category.ToString()))
            {
                sb.AppendLine($"## {CategoryName(group.Key, id)}");
                sb.AppendLine();
                foreach (var c in group)
                {
                    sb.AppendLine($"### {c.Name.Get(lang)} — {c.Title.Get(lang)}");
                    sb.AppendLine();
                    var portrait = $"../../assets/generated/previews/{c.Id}.png";
                    if (File.Exists(Path.Combine(root, portrait))) sb.AppendLine($"<img src=\"{portrait}\" width=\"160\" align=\"right\" alt=\"{c.Name.En}\">");
                    sb.AppendLine($"*{(id ? "Asal" : "From")}: {c.Origin} · {(id ? "Gaya" : "Style")}: {c.Archetype} · {(id ? "Tenaga/Kecepatan/Pertahanan/Teknik" : "Power/Speed/Defense/Technique")}: {c.Stats.Power}/{c.Stats.Speed}/{c.Stats.Defense}/{c.Stats.Technique}*");
                    sb.AppendLine();
                    sb.AppendLine(c.Bio.Get(lang));
                    sb.AppendLine();
                    if (c.Catchphrase.En is not null) sb.AppendLine($"> {c.Catchphrase.Get(lang)}");
                    sb.AppendLine();
                    sb.AppendLine($"- **S**: {c.Specials.Neutral.Get(lang)} · **→ S**: {c.Specials.Forward.Get(lang)} · **↓ S**: {c.Specials.Down.Get(lang)}");
                    if (c.IsSecret) sb.AppendLine($"- {(id ? "Cara membuka" : "Unlock")}: {c.UnlockHint.Get(lang)}");
                    sb.AppendLine();
                    sb.AppendLine(id ? "| Input | Jurus pamungkas | Jenis | Meter | Efek |" : "| Input | Signature move | Kind | Meter | Effect |");
                    sb.AppendLine("|---|---|---|---|---|");
                    foreach (var m in TarkamArena.Core.Combat.SignatureCatalog.For(c))
                        sb.AppendLine($"| `{m.Input}` | **{m.Name.Get(lang)}** | {TarkamArena.Core.Combat.SignatureCatalog.KindName(m.Kind).Get(lang)} | {m.Cost} | {m.Description.Get(lang)} |");
                    sb.AppendLine();
                    sb.AppendLine(id ? "| Input | Jenis | Finisher |" : "| Input | Kind | Finisher |");
                    sb.AppendLine("|---|---|---|");
                    foreach (var f in TarkamArena.Core.Combat.FinisherFactory.Build(c))
                        sb.AppendLine($"| `{f.SequenceGlyphs}` | {TarkamArena.Core.Combat.FinisherFactory.KindName(f.Kind).Get(lang)} | {f.Name.Get(lang)} |");
                    sb.AppendLine();
                }
            }
            File.WriteAllText(Path.Combine(root, "characters.md"), sb.ToString());

            var st = new System.Text.StringBuilder();
            st.AppendLine(id ? "# Arena" : "# Arenas");
            st.AppendLine();
            foreach (var s in Stages.All)
            {
                st.AppendLine($"## {s.Name.Get(lang)} — {s.Region}");
                st.AppendLine();
                st.AppendLine($"![{s.Name.En}](../../assets/backdrops/{s.Id}.jpg)");
                st.AppendLine();
                st.AppendLine(s.Description.Get(lang));
                st.AppendLine();
                st.AppendLine($"- {(id ? "Waktu / cuaca" : "Time / weather")}: {s.Time} / {s.Weather} · {(id ? "Musik" : "Music")}: {s.Music}");
                st.AppendLine($"- NPC: {string.Join(", ", s.Npcs)}");
                foreach (var e in s.Events)
                    st.AppendLine($"- **{e.Name.Get(lang)}**{(e.AffectsFight ? (id ? " (memengaruhi pertarungan)" : " (affects the fight)") : "")}: {e.Description.Get(lang)}");
                st.AppendLine();
            }
            File.WriteAllText(Path.Combine(root, "stages.md"), st.ToString());
            Console.WriteLine($"docs/{dir}: characters.md, stages.md");
        }
        break;

    case "online-smoke":
        // Two clients queue for ranked on a running server and must be paired.
        var url = args.Length > 1 ? args[1] : "http://localhost:5080";
        await using (var a = new TarkamArena.Networking.OnlineServices(url))
        await using (var b = new TarkamArena.Networking.OnlineServices(url))
        {
            var found = new TaskCompletionSource<TarkamArena.Networking.MatchFound>();
            var foundB = new TaskCompletionSource<TarkamArena.Networking.MatchFound>();
            a.MatchFound += m => found.TrySetResult(m);
            b.MatchFound += m => foundB.TrySetResult(m);
            await a.ConnectAsync();
            await b.ConnectAsync();
            await a.JoinRankedQueueAsync(new("Ucup", 1000, "pesilat", 7001));
            await b.JoinRankedQueueAsync(new("Siti", 1040, "kuntilanak", 7002));
            var ma = await found.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var mb = await foundB.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Console.WriteLine($"A: host={ma.IsHost} vs {ma.OpponentName} ({ma.OpponentCharacterId}) stage {ma.StageId}");
            Console.WriteLine($"B: host={mb.IsHost} vs {mb.OpponentName} ({mb.OpponentCharacterId}) port {mb.PeerPort}");
            int ra = await a.ReportResultAsync(ma.MatchId, true);
            int rb = await b.ReportResultAsync(mb.MatchId, false);
            Console.WriteLine($"ratings after: {ra} / {rb}");
            var created = await a.CreateClanAsync("Laskar Pasar", "LPS");
            Console.WriteLine($"clan {created.Name} [{created.Tag}]");
            foreach (var row in await a.LeaderboardAsync(5)) Console.WriteLine($"  {row.Name} {row.Rating} {row.Wins}-{row.Losses}");
        }
        break;

    default:
        Console.WriteLine("commands: stages | portraits [--all] | characters <outDir> [id,id,...] | scene <stage> <p1> <p2> <frames> <out.png> [mode] | music <style> <out.wav>");
        break;
}

static string CategoryName(string key, bool id) => key switch
{
    "Profession" => id ? "Profesi" : "Professions",
    "Mystic" => id ? "Makhluk Mistis" : "Mystic Creatures",
    "Wildlife" => id ? "Satwa Nusantara" : "Wildlife",
    "Society" => id ? "Warga & Satire" : "City & Satire",
    "Secret" => id ? "Karakter Rahasia" : "Secret Legends",
    _ => id ? "Paket DLC" : "DLC Packs",
};

static void RenderScene(StageDefinition stage, string[] ids, int frames, int width, int height, string path, GameMode mode, bool arenaCamera)
{
    var rules = MatchRules.For(mode) with { ArenaEventsEnabled = false };
    var setup = new MatchSetup(rules, stage, [.. ids.Select(id => new FighterEntry(Roster.Get(id)))], Seed: 17);
    var context = new MatchContext(setup);
    var state = MatchSimulation.CreateInitialState(context);
    using var presenter = new ArenaPresenter(context, new PresenterOptions(Weather: true, Physics: true));
    var brains = Enumerable.Range(0, ids.Length).Select(i => new CpuBrain(i, CpuDifficulty.Hard, (uint)(i * 7 + 3))).ToArray();
    var inputs = new InputFrame[ids.Length];
    for (int f = 0; f < frames; f++)
    {
        for (int i = 0; i < brains.Length; i++) inputs[i] = brains[i].Think(state, context);
        MatchSimulation.Step(ref state, context, inputs);
        presenter.OnSimulated(state);
        presenter.Render(state, 1f / 60f);
    }
    if (arenaCamera)
    {
        // Thumbnail framing: a wide view of the whole set.
        presenter.Camera.Position = new Vector3(0, 3.2f, 13.5f);
        presenter.Camera.LookAt(new Vector3(0, 2.0f, -4));
    }
    OffscreenCapture.RenderToPng(presenter.Scene, presenter.Camera, width, height, path);
}

// Plays one signature move and captures the frame where its effect is at its most visible.
static void RenderSignature(Renderer renderer, StageDefinition stage, string id, string foe, int slot, string path)
{
    var rules = MatchRules.For(GameMode.Versus) with { ArenaEventsEnabled = false, TimerEnabled = false };
    var setup = new MatchSetup(rules, stage, [new FighterEntry(Roster.Get(id)), new FighterEntry(Roster.Get(foe))], Seed: 5);
    var context = new MatchContext(setup);
    var state = MatchSimulation.CreateInitialState(context);
    using var presenter = new ArenaPresenter(context, new PresenterOptions(Weather: false, Physics: false, NpcCount: 40));
    var inputs = new InputFrame[2];
    void Run(int frames, InputButtons p0 = InputButtons.None)
    {
        inputs[0] = new(p0);
        for (int f = 0; f < frames; f++)
        {
            MatchSimulation.Step(ref state, context, inputs);
            presenter.OnSimulated(state);
            presenter.Render(state, 1f / 60f);
        }
    }
    while (state.Phase != MatchPhase.Fight) Run(1);
    state.Fighters[0].Position = TarkamArena.Core.Mathematics.FixedVector2.Of(-1.6f, 0);
    state.Fighters[1].Position = TarkamArena.Core.Mathematics.FixedVector2.Of(1.6f, 0);
    state.Fighters[0].Jurus = MatchSimulation.MeterMax;
    Run(20);
    var right = InputButtons.Right;
    var left = InputButtons.Left;
    switch (slot)
    {
        case 0: Run(2, InputButtons.Down); Run(2, InputButtons.Down | right); Run(1, right | InputButtons.Special); break;
        case 1: Run(2, InputButtons.Down); Run(2, InputButtons.Down | left); Run(1, left | InputButtons.Special); break;
        default: Run(2, InputButtons.Down); Run(2); Run(1, InputButtons.Down | InputButtons.Special); break;
    }
    var kind = context.Signatures[0][slot].Kind;
    // Frames after the press: wind-up (16) plus the moment each effect peaks.
    int peak = 16 + kind switch
    {
        SignatureKind.Summon => 40,
        SignatureKind.Crowd => 26,
        SignatureKind.Weapon => 9,
        SignatureKind.Storm => 35,
        SignatureKind.Levitate => 40,
        SignatureKind.Tornado => 30,
        SignatureKind.Barrage => 22,
        SignatureKind.Quake => 14,
        _ => 18,
    };
    Run(peak);
    presenter.Scene.FinishStreaming(TimeSpan.FromSeconds(5));
    for (int i = 0; i < 2; i++) renderer.Render(presenter.Scene, presenter.Camera);
    var rgba = renderer.ReadPixels();
    File.WriteAllBytes(path, OffscreenCapture.EncodePng(rgba, 960, 540));
}

// Renders the fighter's idle pose twice, over black and over white, and recovers a straight-alpha
// PNG from the difference, so soft edges (hair, fur, feathers) keep their partial coverage.
// One scene serves every fighter: the offscreen renderer caches resources by scene handle.
static void RenderPreview(Renderer renderer, Scene scene, CharacterDefinition c, string path, string? modelPath = null, string pose = "idle")
{
    var model = new GltfFighterModel(scene, c, modelPath ?? AssetLocator.PathOf(c.ModelPath));
    var context = new MatchContext(new MatchSetup(MatchRules.For(GameMode.Training), Stages.All[0], [new FighterEntry(c), new FighterEntry(c)]));
    var state = MatchSimulation.CreateInitialState(context);
    ref var f = ref state.Fighters[0];
    f.Position = default;
    f.Facing = 1;
    var (status, move, frames) = pose switch
    {
        "jab" => (FighterStatus.Attack, MoveId.Jab, 3),
        "heavy" => (FighterStatus.Attack, MoveId.Heavy, 10),
        "block" => (FighterStatus.Blockstun, MoveId.Jab, 5),
        "victory" => (FighterStatus.Victory, MoveId.Jab, 15),
        _ => (FighterStatus.Idle, MoveId.Jab, 12),
    };
    f.Status = status;
    f.Move = move;
    for (int i = 0; i <= frames; i++)
    {
        f.StatusFrame = i;
        model.Apply(f, context, 1f / 30f, i / 30f);
        scene.UpdateAnimations(1f / 30f);
    }
    model.Root.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0, 0);
    float h = c.Body.Height;
    var camera = scene.AddCamera(Camera.Perspective(30f * MathF.PI / 180f), new Vector3(0, h * 0.53f, h * 2.45f));
    camera.LookAt(new Vector3(0, h * 0.5f, 0));
    float distance = 2.45f;
    scene.FinishStreaming(TimeSpan.FromSeconds(15));

    byte[] Shot(Vector4 background)
    {
        var env = scene.Environment;
        env.Background = background;
        scene.Environment = env;
        for (int i = 0; i < 3; i++) renderer.Render(scene, camera);
        return renderer.ReadPixels();
    }

    byte[] black, white;
    // Pull the camera back until wings, tails and feet clear the frame edges.
    for (int attempt = 0; ; attempt++)
    {
        black = Shot(new Vector4(0, 0, 0, 1));
        white = Shot(new Vector4(1, 1, 1, 1));
        if (attempt == 5 || !TouchesEdge(black, white)) break;
        distance *= 1.15f;
        camera.Position = new Vector3(0, h * 0.53f, h * distance);
        camera.LookAt(new Vector3(0, h * 0.5f, 0));
    }
    model.Root.Visible = false;
    var rgba = new byte[black.Length];
    // Tone mapping lifts the black background and dims the white one, so measure both at a corner
    // and express each pixel's difference relative to that full-transparency span.
    float bgBlack = (black[0] + black[1] + black[2]) / 3f;
    float span = Math.Max(1f, (white[0] + white[1] + white[2]) / 3f - bgBlack);
    for (int i = 0; i < rgba.Length; i += 4)
    {
        // Over black we see colour * alpha; over white colour * alpha + (1 - alpha).
        float diff = (white[i] - black[i] + white[i + 1] - black[i + 1] + white[i + 2] - black[i + 2]) / 3f;
        int alpha = (int)Math.Clamp(255 * (1 - diff / span), 0, 255);
        if (alpha < 8) alpha = 0;
        rgba[i + 3] = (byte)alpha;
        if (alpha == 0) continue;
        float a = alpha / 255f;
        for (int ch = 0; ch < 3; ch++) rgba[i + ch] = (byte)Math.Clamp((black[i + ch] - bgBlack * (1 - a)) / a, 0, 255);
    }
    File.WriteAllBytes(path, OffscreenCapture.EncodePng(rgba, 512, 640));
}

static bool TouchesEdge(byte[] black, byte[] white, int width = 512, int height = 640)
{
    bool Covered(int x, int y)
    {
        int i = (y * width + x) * 4;
        return Math.Abs(white[i] - black[i]) + Math.Abs(white[i + 1] - black[i + 1]) < Math.Abs(white[0] - black[0]) + Math.Abs(white[1] - black[1]) - 60;
    }
    for (int x = 0; x < width; x += 2)
        if (Covered(x, 6) || Covered(x, height - 7)) return true;
    for (int y = 0; y < height; y += 2)
        if (Covered(6, y) || Covered(width - 7, y)) return true;
    return false;
}

static Scene CreatePreviewScene()
{
    var scene = new Scene();
    var env = SceneEnvironment.Default;
    env.Sky = SkyMode.Color;
    env.AmbientIntensity = 0.55f;
    env.FogDensity = 0;
    scene.Environment = env;
    var key = scene.AddLight(Light.Directional(new Vector3(1, 0.95f, 0.88f), 3.2f));
    key.Position = new Vector3(3, 4, 5);
    key.LookAt(Vector3.Zero);
    var rim = scene.AddLight(Light.Directional(new Vector3(0.75f, 0.85f, 1f), 1.6f));
    rim.Position = new Vector3(-3, 2.5f, -3);
    rim.LookAt(Vector3.Zero);
    scene.AddLight(Light.Hemisphere(new Vector3(0.7f, 0.8f, 1f), new Vector3(0.35f, 0.3f, 0.28f), 0.7f));
    return scene;
}

static Scene CreateCharacterAuditScene()
{
    var scene = new Scene();
    var env = SceneEnvironment.Default;
    env.Background = new Vector4(0.045f, 0.055f, 0.075f, 1);
    env.AmbientIntensity = 0.6f;
    scene.Environment = env;
    var key = scene.AddLight(Light.Directional(new Vector3(1, 0.95f, 0.88f), 3f));
    key.Position = new Vector3(3, 4, 4);
    key.LookAt(Vector3.Zero);
    scene.AddLight(Light.Hemisphere(new Vector3(0.7f, 0.8f, 1f), new Vector3(0.3f), 0.8f));
    return scene;
}

static object RenderCharacterAudit(Scene scene, Renderer renderer, CharacterDefinition c, string output)
{
    var cache = new MeshCache(scene);
    bool authored = AssetLocator.Exists(c.ModelPath);
    // Fail loudly on authored assets: a procedural fallback would conceal a broken export.
    FighterModel model = authored
        ? new GltfFighterModel(scene, c, AssetLocator.PathOf(c.ModelPath))
        : new ProceduralFighterModel(scene, cache, c);
    int clips = model is GltfFighterModel gltf ? gltf.ClipCount : 0;
    if (authored && clips != 14) throw new InvalidDataException($"{c.Id}: expected 14 clips, found {clips}");
    var context = new MatchContext(new MatchSetup(MatchRules.For(GameMode.Training), Stages.All[0], [new FighterEntry(c), new FighterEntry(c)]));
    var state = MatchSimulation.CreateInitialState(context);
    ref var fighter = ref state.Fighters[0];
    fighter.Position = default;
    fighter.Facing = 1;
    float h = c.Body.Height;
    var camera = scene.AddCamera(Camera.Perspective(35f * MathF.PI / 180f), new Vector3(0, h * 0.57f, h * 3.3f));
    camera.LookAt(new Vector3(0, h * 0.52f, 0));
    scene.FinishStreaming(TimeSpan.FromSeconds(15));
    var captures = new List<string>();
    foreach (var (name, status, move, frames, yaw) in new[]
    {
        ("idle", FighterStatus.Idle, MoveId.Jab, 12, 0.35f),
        ("jab", FighterStatus.Attack, MoveId.Jab, 3, MathF.PI / 2),
        ("heavy", FighterStatus.Attack, MoveId.Heavy, 10, 0.35f),
        ("block", FighterStatus.Blockstun, MoveId.Jab, 5, 0.35f),
        ("victory", FighterStatus.Victory, MoveId.Jab, 15, 0.35f),
    })
    {
        fighter.Status = status;
        fighter.Move = move;
        fighter.StatusFrame = 0;
        for (int i = 0; i <= frames; i++)
        {
            fighter.StatusFrame = i;
            model.Apply(fighter, context, 1f / 30f, i / 30f);
            scene.UpdateAnimations(1f / 30f);
        }
        model.Root.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
        for (int i = 0; i < 3; i++) renderer.Render(scene, camera);
        string file = $"{c.Id}-{name}.png";
        File.WriteAllBytes(Path.Combine(output, file), OffscreenCapture.EncodePng(renderer.ReadPixels(), 480, 600));
        captures.Add(file);
    }
    Console.WriteLine($"character {c.Id}: {(authored ? "glTF" : "procedural")}, {clips} clips, {captures.Count} captures");
    model.Root.Visible = false;
    return new { id = c.Id, model = authored ? "glTF" : "procedural", clips, captures };
}

static void RenderPortrait(CharacterDefinition c, string path)
{
    using var scene = new Scene();
    var cache = new MeshCache(scene);
    var env = SceneEnvironment.Default;
    env.Background = Palette.Hex(Palette.Terpal);
    env.AmbientIntensity = 0.5f;
    scene.Environment = env;
    var key = scene.AddLight(Light.Directional(new Vector3(1, 0.95f, 0.85f), 3f));
    key.Position = new Vector3(3, 4, 4);
    key.LookAt(Vector3.Zero);
    var rim = scene.AddLight(Light.Directional(Palette.Hex3(Palette.Spanduk), 2f));
    rim.Position = new Vector3(-3, 2, -3);
    rim.LookAt(Vector3.Zero);
    scene.AddLight(Light.Hemisphere(new Vector3(0.6f, 0.7f, 1f), new Vector3(0.3f, 0.25f, 0.2f), 0.6f));

    var model = FighterModel.Create(scene, cache, c);
    var context = new MatchContext(new MatchSetup(MatchRules.For(GameMode.Training), Stages.All[0], [new FighterEntry(c), new FighterEntry(c)]));
    var state = MatchSimulation.CreateInitialState(context);
    ref var f = ref state.Fighters[0];
    f.Position = default;
    f.Facing = 1;
    for (int i = 0; i < 30; i++) model.Apply(f, context, 1f / 30f, i / 30f);
    model.Root.Rotation = Quaternion.CreateFromYawPitchRoll(0.45f, 0, 0);
    scene.UpdateAnimations(0.5f);

    float h = c.Body.Height;
    var cam = scene.AddCamera(Camera.Perspective(30f * MathF.PI / 180f), new Vector3(0.6f, h * 0.62f, h * 2.6f));
    cam.LookAt(new Vector3(0, h * 0.55f, 0));
    OffscreenCapture.RenderToPng(scene, cam, 512, 640, path);
}

static void WriteWav(string path, float[] interleaved, int sampleRate)
{
    using var w = new BinaryWriter(File.Create(path));
    int bytes = interleaved.Length * 2;
    w.Write("RIFF"u8);
    w.Write(36 + bytes);
    w.Write("WAVEfmt "u8);
    w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(sampleRate); w.Write(sampleRate * 4); w.Write((short)4); w.Write((short)16);
    w.Write("data"u8);
    w.Write(bytes);
    foreach (float s in interleaved) w.Write((short)(Math.Clamp(s, -1f, 1f) * 32767));
}
