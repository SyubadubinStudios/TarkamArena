using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Creator;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Progression;
using TarkamArena.Engine.Rendering;
using ThreeNet;
using ThreeNet.Avalonia;

namespace TarkamArena.App.Screens;

/// <summary>
/// Fighter creator: origin (human, mystic, regional champion), fighting style, a 24-point stat
/// budget, body shape, colours and silhouette details, with a live 3D preview.
/// </summary>
public sealed class CreatorScreen : Screen
{
    private static readonly uint[] s_palette = [0x1F4FB0, 0xE0322B, 0xF6C21B, 0x2E8B57, 0x161616, 0xF3EEE1, 0x9C27B0, 0xF28C1B, 0x8C5A2B, 0x6BD0FF];
    private static readonly uint[] s_skins = [0xE8C8A8, 0xD49A6E, 0xB98055, 0xA86A44, 0x6E4A33, 0x3FA265, 0x8E7A6A];
    private static readonly Archetype[] s_styles = [Archetype.Brawler, Archetype.Striker, Archetype.Grappler, Archetype.Mystic, Archetype.Beast];

    private CustomFighterSpec _spec = new() { Name = "", Power = 6, Speed = 6, Defense = 6, Technique = 6 };
    private readonly TextBox _name = new() { Width = 360, MaxLength = 20, PlaceholderText = "Joko Kendil" };
    private readonly TextBlock _points = Ui.Text("", "h3");
    private readonly TextBlock _error = Ui.Wrap("", "body").With(t => t.Foreground = Ui.Cabai);
    private readonly StackPanel _features = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
    private readonly ThreeNetView _view = new() { Width = 560, Height = 640, MaxFramesPerSecond = 30 };
    private Scene? _scene;
    private FighterModel? _model;
    private Node? _camera;
    private float _time;

    public CreatorScreen()
    {
        _name.TextChanged += (_, _) => _spec = _spec with { Name = _name.Text ?? "" };

        var bases = new OptionRow(Loc.T("creator.base"), 3, 0, i => ((CreatorBase)i) switch
        {
            CreatorBase.Human => Loc.T("creator.base.human"),
            CreatorBase.Mystic => Loc.T("creator.base.mystic"),
            _ => Loc.T("creator.base.jawara"),
        }, i =>
        {
            _spec = _spec with { Base = (CreatorBase)i, Features = _spec.Features & CharacterCreator.AllowedFeatures((CreatorBase)i) };
            RebuildFeatures();
            Changed();
        });
        var style = new OptionRow(Loc.T("creator.style"), s_styles.Length, 0, i => s_styles[i].ToString(), i => { _spec = _spec with { Archetype = s_styles[i] }; Changed(); });
        OptionRow Stat(string key, Func<byte> get, Func<byte, CustomFighterSpec> set) =>
            new(Loc.T(key), CustomFighterSpec.MaxStat, get() - 1, i => $"{i + 1}", i => { _spec = set((byte)(i + 1)); Changed(); });
        var power = Stat("select.stats.power", () => _spec.Power, v => _spec with { Power = v });
        var speed = Stat("select.stats.speed", () => _spec.Speed, v => _spec with { Speed = v });
        var defense = Stat("select.stats.defense", () => _spec.Defense, v => _spec with { Defense = v });
        var technique = Stat("select.stats.technique", () => _spec.Technique, v => _spec with { Technique = v });
        var height = new OptionRow(Loc.T("creator.height"), 9, 4, i => $"{1.5f + i * 0.05f:0.00} m", i => { _spec = _spec with { Height = 1.5f + i * 0.05f }; Changed(); });
        var build = new OptionRow(Loc.T("creator.build"), 6, 2, i => $"{0.85f + i * 0.1f:0.0}", i => { _spec = _spec with { Width = 0.85f + i * 0.1f }; Changed(); });
        var primary = new OptionRow(Loc.T("creator.colors") + " 1", s_palette.Length, 0, i => "■", i => { _spec = _spec with { PrimaryColor = s_palette[i] }; Changed(); });
        var secondary = new OptionRow(Loc.T("creator.colors") + " 2", s_palette.Length, 2, i => "■", i => { _spec = _spec with { SecondaryColor = s_palette[i] }; Changed(); });
        var skin = new OptionRow("Skin", s_skins.Length, 2, i => "■", i => { _spec = _spec with { SkinColor = s_skins[i] }; Changed(); });
        var save = Ui.Banner(Loc.T("creator.saveFighter").ToUpperInvariant(), Save);
        Nav(bases, style, power, speed, defense, technique, height, build, primary, secondary, skin, save);

        var form = Ui.V(8,
            new SignText(Loc.T("creator.title").ToUpperInvariant(), 50),
            Ui.H(12, Ui.Text(Loc.T("creator.name").ToUpperInvariant(), "eyebrow").With(t => t.VerticalAlignment = VerticalAlignment.Center), _name),
            bases, style, _points, power, speed, defense, technique, height, build, primary, secondary, skin,
            Ui.Text(Loc.T("creator.features").ToUpperInvariant(), "eyebrow"), _features, _error, save);
        var scroll = new ScrollViewer { Content = form };
        var root = Ui.Columns("*,Auto", scroll, Ui.Panel(_view, 8).With(p => p.VerticalAlignment = VerticalAlignment.Center));
        root.Margin = new Thickness(80, 40, 80, 40);
        Content = root;
        RebuildFeatures();
        _view.Frame += (_, e) =>
        {
            _time += e.DeltaSeconds;
            if (_model is null) return;
            _model.Root.Rotation = Quaternion.CreateFromYawPitchRoll(_time * 0.6f, 0, 0);
            _scene?.UpdateAnimations(e.DeltaSeconds);
        };
        Changed();
    }

    private void RebuildFeatures()
    {
        _features.Children.Clear();
        var allowed = CharacterCreator.AllowedFeatures(_spec.Base);
        foreach (var f in Enum.GetValues<BodyFeature>())
        {
            if (f == BodyFeature.None || (allowed & f) == 0) continue;
            var feature = f;
            var chip = Ui.Chip(f.ToString(), () =>
            {
                _spec = _spec with { Features = _spec.Features ^ feature };
                RebuildFeatures();
                Changed();
            });
            if (_spec.Features.HasFlag(f)) chip.Classes.Add("active");
            _features.Children.Add(chip);
        }
    }

    private void Changed()
    {
        int left = CustomFighterSpec.StatBudget - _spec.SpentPoints;
        _points.Text = Loc.T("creator.points", left);
        _points.Foreground = left == 0 ? Ui.Daun : Ui.Spanduk;
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        var preview = new CustomFighterSpec
        {
            Name = "Preview", Base = _spec.Base, Archetype = _spec.Archetype, Power = 6, Speed = 6, Defense = 6, Technique = 6,
            Height = _spec.Height, Width = _spec.Width, PrimaryColor = _spec.PrimaryColor, SecondaryColor = _spec.SecondaryColor, SkinColor = _spec.SkinColor, Features = _spec.Features,
        };
        var def = CharacterCreator.ToDefinition(preview);
        var old = _scene;
        _scene = new Scene();
        var env = SceneEnvironment.Default;
        env.Background = Palette.Hex(Palette.TerpalDeep);
        _scene.Environment = env;
        var key = _scene.AddLight(Light.Directional(new Vector3(1, 0.95f, 0.85f), 3f));
        key.Position = new Vector3(3, 4, 4);
        key.LookAt(Vector3.Zero);
        _scene.AddLight(Light.Hemisphere(new Vector3(0.6f, 0.7f, 1f), new Vector3(0.3f, 0.25f, 0.2f), 0.7f));
        _model = new ProceduralFighterModel(_scene, new MeshCache(_scene), def);
        var ctx = new MatchContext(new MatchSetup(MatchRules.For(GameMode.Training), Stages.All[0], [new FighterEntry(def), new FighterEntry(def)]));
        var state = MatchSimulation.CreateInitialState(ctx);
        ref var f = ref state.Fighters[0];
        f.Position = default;
        for (int i = 0; i < 20; i++) _model.Apply(f, ctx, 1f / 30f, i / 30f);
        _camera = _scene.AddCamera(ThreeNet.Camera.Perspective(32f * MathF.PI / 180f), new Vector3(0, def.Body.Height * 0.6f, def.Body.Height * 2.8f));
        _camera.LookAt(new Vector3(0, def.Body.Height * 0.5f, 0));
        _view.Scene = _scene;
        _view.Camera = _camera;
        old?.Dispose();
    }

    private void Save()
    {
        var errors = CharacterCreator.Validate(_spec);
        if (errors.Count > 0)
        {
            _error.Text = string.Join("\n", errors.Distinct().Select(e => Loc.T(e, CustomFighterSpec.StatBudget)));
            return;
        }
        Game.Profile.CustomFighters.Add(_spec with { Name = _spec.Name.Trim() });
        Window.ToastRewards(ProgressionService.Refresh(Game.Profile));
        Game.Save();
        Window.Toast(Loc.T("common.saved"), _spec.Name);
        Window.Back();
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Left or MenuAction.Right && NavItems[NavIndex] is OptionRow row)
        {
            row.Step(action == MenuAction.Right ? 1 : -1);
            return true;
        }
        if (action is MenuAction.Left or MenuAction.Right) return true;
        return base.HandleAction(action);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        _view.IsRendering = true;
    }

    public override void OnLeave()
    {
        _view.IsRendering = false;
        _view.Scene = null!;
        _scene?.Dispose();
        _scene = null;
    }
}
