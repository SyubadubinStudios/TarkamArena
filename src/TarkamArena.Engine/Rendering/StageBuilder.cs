using System.Numerics;
using TarkamArena.Core.Content;
using TarkamArena.Engine.Assets;
using TarkamArena.Physics;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>Lights and nodes of a built stage that the presenter animates later.</summary>
public sealed class StageRig
{
    public required Node Sun { get; init; }
    public List<Node> Lamps { get; } = [];
    public List<(Node Node, float Speed)> Spinners { get; } = [];
    public List<Node> Pulsers { get; } = [];
    public List<Swayer> Swayers { get; } = [];
    public List<Blinker> Blinkers { get; } = [];
    public List<Shaker> Shakers { get; } = [];
    public List<ScatterGroup> Scatters { get; } = [];
    public Vector3 SunDirection { get; init; }
    public float BaseSunIntensity { get; init; }
    public bool Night { get; init; }
}

/// <summary>
/// Builds an arena: ground, sky, fog and lighting from the stage palette, then a hand-placed set of
/// landmarks for each location. An authored <c>models/stages/&lt;id&gt;.glb</c> (Blender MCP) replaces
/// the procedural set when present. Physics props are registered with <see cref="ArenaPhysics"/>.
/// </summary>
public sealed partial class StageBuilder
{
    private readonly Scene _scene;
    private readonly MeshCache _c;
    private readonly ArenaPhysics? _physics;
    private readonly Random _random;
    private readonly StageDefinition _stage;

    private StageBuilder(Scene scene, MeshCache cache, StageDefinition stage, ArenaPhysics? physics)
    {
        _scene = scene;
        _c = cache;
        _stage = stage;
        _physics = physics;
        _random = new Random((int)Core.Mathematics.DeterministicRandom.Hash(stage.Id));
    }

    public static StageRig Build(Scene scene, MeshCache cache, StageDefinition stage, ArenaPhysics? physics, bool dynamicWeather = true) =>
        new StageBuilder(scene, cache, stage, physics).Build(dynamicWeather);

    private StageRig Build(bool dynamicWeather)
    {
        var p = _stage.Palette;
        bool night = _stage.Time == TimeOfDay.Night;
        bool dusk = _stage.Time is TimeOfDay.Dusk or TimeOfDay.Dawn;

        Vector3 sunDir = _stage.Time switch
        {
            TimeOfDay.Dawn => Vector3.Normalize(new Vector3(0.8f, 0.25f, -0.4f)),
            TimeOfDay.Dusk => Vector3.Normalize(new Vector3(-0.8f, 0.22f, -0.5f)),
            TimeOfDay.Night => Vector3.Normalize(new Vector3(0.3f, 0.8f, 0.4f)),
            _ => Vector3.Normalize(new Vector3(0.35f, 0.85f, 0.4f)),
        };

        var env = SceneEnvironment.Default;
        env.Background = Palette.Hex(p.Sky);
        env.Sky = night ? SkyMode.Color : SkyMode.Procedural;
        env.SunDirection = sunDir;
        env.SkyIntensity = dusk ? 0.8f : 1f;
        env.SkyHaze = _stage.Weather is Weather.Fog or Weather.Ash ? 0.8f : 0.35f;
        env.SkyClouds = _stage.Weather is Weather.Cloudy or Weather.Rain or Weather.Storm ? 0.8f : 0.3f;
        env.AmbientColor = Palette.Hex3(Palette.Mix(p.Sky, 0xFFFFFF, 0.5f));
        env.AmbientIntensity = night ? 0.5f : 0.45f;
        env.FogColor = Palette.Hex3(p.Fog);
        env.FogDensity = !dynamicWeather ? 0.008f : _stage.Weather switch
        {
            // Thick enough to read as haze in the distance without washing out the textured floor.
            Weather.Fog => 0.024f,
            Weather.Ash => 0.02f,
            Weather.Rain or Weather.Storm => 0.025f,
            Weather.Night => 0.02f,
            _ => 0.01f,
        };
        _scene.Environment = env;

        var sunLight = Light.Directional(night ? new Vector3(0.75f, 0.82f, 1f) : Palette.Hex3(p.Light), night ? 1.8f : dusk ? 2.4f : 3.2f);
        sunLight.CastShadow = true;
        sunLight.ShadowStrength = night ? 0.5f : 0.85f;
        var sun = _scene.AddLight(sunLight, name: "sun");
        sun.Position = sunDir * 30f;
        sun.LookAt(Vector3.Zero);
        _scene.AddLight(Light.Hemisphere(night ? Palette.Hex3(Palette.Lighten(p.Sky, 0.45f)) : Palette.Hex3(p.Sky), Palette.Hex3(Palette.Lighten(p.Ground, 0.2f)), night ? 0.9f : 0.6f), name: "sky");
        if (night)
        {
            // A warm key over the fighting strip, like the floodlights of a kampung tournament.
            var flood = Light.Spot(new Vector3(1f, 0.9f, 0.75f), 60f, 30f, 0.5f, 0.9f);
            var floodNode = _scene.AddLight(flood, name: "floodlight");
            floodNode.Position = new Vector3(0, 9, 7);
            floodNode.LookAt(new Vector3(0, 0, -1));
        }

        var rig = new StageRig { Sun = sun, SunDirection = sunDir, BaseSunIntensity = sunLight.Intensity, Night = night };

        // Ground: a tiling hand-painted texture (Rodin MCP) tinted with the stage palette.
        // Generic textures (concrete, earth, sand, stone) take the stage colour; the location-specific ones
        // (jungle floor, burnt peat, volcanic sand, beach, temple slabs, parquet) already carry their own.
        string groundKind = GroundKind(_stage.Id);
        bool painted = groundKind is not ("concrete" or "earth" or "sand" or "stone");
        uint groundTint = painted ? (night ? 0xC8C8D0u : 0xFFFFFFu) : Palette.Lighten(p.Ground, night ? 0.6f : 0.3f);
        var groundOptions = MaterialOptions.Pbr(Palette.Hex(groundTint), 0f, _stage.Weather is Weather.Rain or Weather.Storm ? 0.3f : groundKind == "parquet" ? 0.45f : 0.92f);
        var groundTexture = AssetLocator.PathOf($"textures/ground-{groundKind}.jpg");
        if (File.Exists(groundTexture))
        {
            var tex = _scene.LoadTextureCached(groundTexture);
            tex.SetSampler(WrapMode.Repeat, WrapMode.Repeat);
            groundOptions.BaseColorMap = tex;
            groundOptions.UvScale = new Vector2(30, 15);
        }
        else
        {
            groundOptions.BaseColor = Palette.Hex(p.Ground);
        }
        var ground = _c.Add(_c.Plane, _scene.CreateMaterial(groundOptions), new Vector3(0, 0, -5), new Vector3(120, 60, 1), euler: new Vector3(-MathF.PI / 2, 0, 0));
        ground.CastShadow = false;

        // Painted backdrop (Rodin MCP) far behind the set, like the background plates of classic fighting games.
        var backdropPath = AssetLocator.PathOf($"backdrops/{_stage.Id}.jpg");
        _hasBackdrop = File.Exists(backdropPath);
        if (_hasBackdrop)
        {
            var texture = _scene.LoadTextureCached(backdropPath);
            var options = MaterialOptions.Basic(Vector4.One);
            options.BaseColorMap = texture;
            options.CullMode = CullMode.None;
            var material = _scene.CreateMaterial(options);
            // 21:9 plate, placed so its painted ground line meets the 3D floor.
            var plate = _c.Add(_c.Plane, material, new Vector3(0, 7.2f, -22f), new Vector3(44f, 44f / 21f * 9f, 1));
            plate.CastShadow = false;
            plate.ReceiveShadow = false;
            var env2 = _scene.Environment;
            env2.FogDensity *= 0.55f;
            _scene.Environment = env2;
        }

        var authored = AssetLocator.PathOf(_stage.ModelPath);
        if (File.Exists(authored))
        {
            var imported = _scene.LoadModelCached(authored);
            imported.Root.SetShadowsRecursive(true, true);
        }
        else
        {
            BuildSet(rig);
        }
        Dress(rig);
        return rig;
    }

    // ───────────────────────── set pieces ─────────────────────────

    private static string GroundKind(string stageId) => stageId switch
    {
        "hutan-kalimantan" => "jungle",
        "hutan-gambut-riau" => "peat",
        "gunung-bromo" => "volcanic",
        "pantai-selatan" => "beach",
        "candi-borobudur" => "temple",
        "ruang-sidang" => "parquet",
        "sawah-jawa-tengah" or "alun-alun-ponorogo" or "karnaval-sound-horeg" => "earth",
        "kota-tua" or "malam-pengerupukan" => "stone",
        _ => "concrete",
    };

    private Material? _spanduk;

    /// <summary>The tournament's own banner, painted in the signage lettering of the UI.</summary>
    private void TournamentBanner(Vector3 pos, float width)
    {
        if (_spanduk is null)
        {
            var path = AssetLocator.PathOf("textures/spanduk-tarkam.png");
            var options = MaterialOptions.Pbr(Vector4.One, 0, 0.8f);
            if (File.Exists(path)) options.BaseColorMap = _scene.LoadTextureCached(path);
            else options.BaseColor = Palette.Hex(Palette.Spanduk);
            _spanduk = _scene.CreateMaterial(options);
        }
        float height = width * 360f / 1600f;
        _c.Add(_c.Plane, _spanduk, pos, new Vector3(width, height, 1));
        Box(0x6B4A2B, pos + new Vector3(-width / 2, -height / 2 - 0.6f, -0.02f), new Vector3(0.07f, height + 1.6f, 0.07f));
        Box(0x6B4A2B, pos + new Vector3(width / 2, -height / 2 - 0.6f, -0.02f), new Vector3(0.07f, height + 1.6f, 0.07f));
    }

    private float R(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    /// <summary>The painted backdrop already shows the far scenery; procedural pieces behind this depth are hidden.</summary>
    private Node Keep(Node node, Vector3 pos)
    {
        if (_hasBackdrop && pos.Z < -8.5f) node.Visible = false;
        return node;
    }

    private Node Box(uint color, Vector3 pos, Vector3 size, float yaw = 0, float rough = 0.8f, float emissive = 0) =>
        Keep(_c.Add(_c.Box, _c.Pbr(color, rough, 0, emissive), pos, size, euler: new Vector3(0, yaw, 0)), pos);

    private Node Cyl(uint color, Vector3 pos, float radius, float height, float rough = 0.8f) =>
        Keep(_c.Add(_c.Cylinder, _c.Pbr(color, rough), pos + new Vector3(0, height / 2, 0), new Vector3(radius * 2, height, radius * 2)), pos);

    private Node Ball(uint color, Vector3 pos, float size, float rough = 0.8f, float emissive = 0) =>
        Keep(_c.Add(_c.Sphere, _c.Pbr(color, rough, 0, emissive), pos, new Vector3(size)), pos);

    private void Banner(Vector3 pos, float width, uint color, uint stripe)
    {
        Box(color, pos, new Vector3(width, 0.9f, 0.04f));
        Box(stripe, pos + new Vector3(0, -0.3f, 0.025f), new Vector3(width * 0.9f, 0.12f, 0.02f));
        Box(0x6B4A2B, pos + new Vector3(-width / 2, -0.8f, 0), new Vector3(0.07f, 2.4f, 0.07f));
        Box(0x6B4A2B, pos + new Vector3(width / 2, -0.8f, 0), new Vector3(0.07f, 2.4f, 0.07f));
    }

    private void Stall(Vector3 pos, uint tarp)
    {
        foreach (var (dx, dz) in new[] { (-1.1f, -0.7f), (1.1f, -0.7f), (-1.1f, 0.7f), (1.1f, 0.7f) })
            Box(0x5A4632, pos + new Vector3(dx, 1.1f, dz), new Vector3(0.08f, 2.2f, 0.08f));
        _c.Add(_c.Box, _c.Pbr(tarp, 0.6f), pos + new Vector3(0, 2.3f, 0), new Vector3(2.6f, 0.05f, 1.9f), euler: new Vector3(0.15f, 0, 0));
        Box(0x8C6A45, pos + new Vector3(0, 0.75f, 0.2f), new Vector3(2.2f, 0.1f, 1.0f));
        for (int i = 0; i < 4; i++)
            Box(new uint[] { 0x8C6A45, 0x6B4A2B, 0x2E8B57, 0xA0522D }[i % 4], pos + new Vector3(-0.8f + i * 0.52f, 0.95f, 0.2f), new Vector3(0.42f, 0.3f, 0.42f));
    }

    private bool _hasBackdrop;

    private void House(Vector3 pos, float width, float height, uint wall, uint roof)
    {
        // The painted backdrop already shows the skyline; only keep buildings close to the fight.
        if (_hasBackdrop && pos.Z < -8.5f) return;
        Box(wall, pos + new Vector3(0, height / 2, 0), new Vector3(width, height, 3));
        _c.Add(_c.Box, _c.Pbr(roof, 0.7f), pos + new Vector3(0, height + 0.4f, 0), new Vector3(width + 0.4f, 0.15f, 3.4f), euler: new Vector3(0, 0, 0.12f));
        for (float x = -width / 2 + 0.8f; x < width / 2 - 0.4f; x += 1.4f)
            Box(0x2A2A35, pos + new Vector3(x, height * 0.6f, 1.52f), new Vector3(0.6f, 0.7f, 0.04f), emissive: _stage.Time == TimeOfDay.Night ? 0.6f : 0);
    }

    private void Tree(Vector3 pos, float height, uint leaves, bool palm = false)
    {
        // Prefer the Rodin tree models; the primitive version is the fallback.
        if (DecorMesh(palm ? "palm" : "jungle-tree") is not null)
        {
            Decor(palm ? "palm" : "jungle-tree", pos, palm ? height * 1.15f : height * 1.1f, _rigForTrees!, palm ? 0.06f : 0.03f);
            return;
        }
        if (_hasBackdrop && pos.Z < -8.5f) return;
        var tree = _scene.CreateNode(name: "tree");
        tree.Position = pos;
        _c.Add(_c.Cylinder, _c.Pbr(0x5A3A1E, 0.8f), new Vector3(0, height / 2, 0), new Vector3(palm ? 0.24f : 0.44f, height, palm ? 0.24f : 0.44f), tree);
        if (palm)
        {
            for (int i = 0; i < 6; i++)
                _c.Add(_c.Box, _c.Pbr(leaves, 0.7f), new Vector3(0, height, 0), new Vector3(0.25f, 0.05f, 2.2f), tree, new Vector3(0.5f, i * MathF.PI / 3, 0));
        }
        else
        {
            _c.Add(_c.Sphere, _c.Pbr(leaves, 0.8f), new Vector3(0, height, 0), new Vector3(height * 0.7f), tree);
            _c.Add(_c.Sphere, _c.Pbr(Palette.Darken(leaves, 0.15f), 0.8f), new Vector3(height * 0.25f, height * 0.85f, 0.3f), new Vector3(height * 0.5f), tree);
        }
        _rigForTrees!.Swayers.Add(new Swayer(tree, Quaternion.Identity, (palm ? 0.05f : 0.025f) * Wind, R(0.7f, 1.1f), R(0, MathF.Tau)));
    }

    private Node Bus(Vector3 pos, uint body, float yaw = 0)
    {
        var root = _scene.CreateNode(name: "bus");
        root.Position = pos;
        root.EulerAngles = new Vector3(0, yaw, 0);
        _c.Add(_c.Box, _c.Pbr(body, 0.4f, 0.3f), new Vector3(0, 1.5f, 0), new Vector3(10, 2.6f, 2.5f), root);
        _c.Add(_c.Box, _c.Pbr(0x1A2A3A, 0.1f, 0.5f), new Vector3(0, 2.0f, 1.26f), new Vector3(9, 0.9f, 0.02f), root);
        foreach (float x in new[] { -3.5f, 3.5f })
            _c.Add(_c.Cylinder, _c.Pbr(0x111111, 0.9f), new Vector3(x, 0.45f, 1.1f), new Vector3(0.9f, 0.3f, 0.9f), root, new Vector3(MathF.PI / 2, 0, 0));
        return root;
    }

    private void SpeakerWall(Vector3 pos, int columns, int rows, StageRig rig)
    {
        Box(0x222228, pos + new Vector3(0, 0.6f, 0), new Vector3(columns * 1.1f + 0.6f, 1.2f, 2.6f), rough: 0.5f);
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
        {
            var cab = new Vector3(pos.X + (x - (columns - 1) / 2f) * 1.1f, 1.2f + 0.55f + y * 1.1f, pos.Z);
            Shake(Box(0x2A2A32, cab, new Vector3(1.05f, 1.05f, 1.0f), rough: 0.4f), rig, 0.025f);
            var cone = _c.Add(_c.Cylinder, _c.Pbr(0x2A2A30, 0.3f, 0.5f), cab + new Vector3(0, 0, 0.51f), new Vector3(0.8f, 0.04f, 0.8f), euler: new Vector3(MathF.PI / 2, 0, 0));
            rig.Pulsers.Add(cone);
        }
        // LED strips that flash to the beat.
        for (int y = 0; y <= rows; y++)
            rig.Pulsers.Add(Box(y % 2 == 0 ? 0xFF3EA5u : 0x7CFC00u, new Vector3(pos.X, 1.2f + y * 1.1f, pos.Z + 0.52f), new Vector3(columns * 1.1f, 0.06f, 0.02f), emissive: 4f));
    }

    private void Stupa(Vector3 pos, float size)
    {
        Ball(0x8A8478, pos + new Vector3(0, size * 0.5f, 0), size, 0.95f);
        Cyl(0x8A8478, pos + new Vector3(0, size * 0.9f, 0), size * 0.12f, size * 0.5f, 0.95f);
    }

    private void Lamp(Vector3 pos, uint color, StageRig rig, float intensity = 6f)
    {
        Cyl(0x333333, pos, 0.05f, 3.2f, 0.4f);
        Blink(Ball(color, pos + new Vector3(0, 3.25f, 0), 0.3f, 0.2f, 5f), rig, 9f);
        if (rig.Lamps.Count < 4)
        {
            var light = _scene.AddLight(Light.Point(Palette.Hex3(color), intensity, 9f), name: "lamp");
            light.Position = pos + new Vector3(0, 3.1f, 0.4f);
            rig.Lamps.Add(light);
        }
    }

    private void StringLights(float z, float y, float width, StageRig rig)
    {
        for (float x = -width / 2; x <= width / 2; x += 0.6f)
            Blink(Ball(new uint[] { 0xF6C21B, 0xE0322B, 0x6BD0FF, 0x7CFC00 }[(int)((x + width) / 0.6f) % 4], new Vector3(x, y + MathF.Sin(x * 0.7f) * 0.25f, z), 0.12f, 0.2f, 6f), rig, 2.5f, chase: true);
    }

    private void Props(int count, uint color, PropShape shape, float size, float mass = 2f)
    {
        if (_physics is null) return;
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            var pos = new Vector3(side * R(_stage.HalfWidth - 0.5f, _stage.HalfWidth + 3f), size * 0.5f + 0.01f + (i / 4) * size, R(-2.2f, -1.2f));
            var dims = shape == PropShape.Box ? new Vector3(size, size * 0.8f, size) : new Vector3(size, size, size);
            _physics.AddProp(shape, dims, pos, mass, color);
        }
    }

    private void Crowd2D(float z, float width, uint color)
    {
        if (_hasBackdrop) return;
        // Distant crowd silhouettes as instanced capsules: cheap and it reads as a packed audience.
        var positions = new Vector3[(int)(width / 0.55f)];
        for (int i = 0; i < positions.Length; i++) positions[i] = new Vector3(-width / 2 + i * 0.55f + R(-0.1f, 0.1f), 0.8f, z + R(-0.4f, 0.4f));
        var node = _scene.AddMesh(_c.Capsule, _c.Pbr(color, 0.9f), name: "far-crowd");
        node.Scale = new Vector3(0.45f, 0.6f, 0.4f);
        node.SetInstances(positions, 1f);
        node.CastShadow = false;
    }

    private StageRig? _rigForTrees;

    private void BuildSet(StageRig rig)
    {
        _rigForTrees = rig;
        var p = _stage.Palette;
        float hw = _stage.HalfWidth;
        switch (_stage.Id)
        {
            case "pasar-becek":
                for (int i = 0; i < 5; i++) Stall(new Vector3(-9 + i * 4.5f, 0, -5.5f + (i % 2) * 0.6f), i % 2 == 0 ? Palette.Terpal : 0xE0322Bu);
                House(new Vector3(-6, 0, -11), 10, 5, 0xC9B78F, 0x8C3A20);
                House(new Vector3(7, 0, -11.5f), 12, 6, 0xB8C4C9, 0x5A6A7A);
                TournamentBanner(new Vector3(0, 3.4f, -8.2f), 6.5f);

                Props(8, 0x8C6A45, PropShape.Box, 0.5f);
                Props(6, 0x2E8B57, PropShape.Sphere, 0.3f, 0.6f);
                break;

            case "sawah-jawa-tengah":
                for (int row = 0; row < 6; row++)
                    Box(Palette.Mix(0x7A8A3A, 0xC9B03A, row * 0.15f), new Vector3(0, -0.02f + row * 0.15f, -4 - row * 3.5f), new Vector3(70, 0.3f, 3.2f), rough: 0.9f);
                _c.Add(_c.Cone, _c.Pbr(0x4A5A6A, 0.95f), new Vector3(8, 9, -60), new Vector3(60, 22, 40));
                for (int i = 0; i < 8; i++) Tree(new Vector3(R(-25, 25), 0, R(-26, -14)), R(4, 7), 0x3A6A2A, palm: i % 2 == 0);
                Box(0x8C6A45, new Vector3(-6, 1.2f, -6), new Vector3(3, 0.15f, 2.2f));
                Props(4, 0xC9B03A, PropShape.Cylinder, 0.5f, 1f);
                break;

            case "gedung-dpr":
            case "jalan-demo":
                for (int i = 0; i < 5; i++) Box(0xD8D2C5, new Vector3(0, i * 0.3f, -7 - i * 0.6f), new Vector3(30, 0.3f, 2), rough: 0.5f);
                for (int i = -4; i <= 4; i++) Cyl(0xEDE8DC, new Vector3(i * 3f, 1.5f, -10.5f), 0.45f, 9, 0.5f);
                Box(0xE8E2D5, new Vector3(0, 11, -12), new Vector3(30, 2.5f, 4), rough: 0.5f);
                var dome = Keep(_c.Add(_c.Sphere, _c.Pbr(0x2E8B57, 0.35f, 0.3f), new Vector3(0, 12.2f, -18), new Vector3(18, 12, 18)), new Vector3(0, 0, -18));
                dome.CastShadow = false;
                if (_stage.Id == "jalan-demo")
                {
                    for (int i = 0; i < 6; i++) Box(0xC0C0C0, new Vector3(-10 + i * 4, 0.6f, -4.5f), new Vector3(2.8f, 1.2f, 0.4f), rough: 0.3f); // barricades
                    var truck = Bus(new Vector3(13, 0, -6), 0x3A4A5A, -0.3f);
                    _c.Add(_c.Cylinder, _c.Pbr(0x222222, 0.4f), new Vector3(-2, 3.4f, 0), new Vector3(0.3f, 2.5f, 0.3f), truck, new Vector3(0, 0, 1.2f));
                    Banner(new Vector3(-6, 2.6f, -3.8f), 4, 0xF3EEE1, Palette.Cabai);
                    Banner(new Vector3(5, 2.8f, -3.9f), 5, Palette.Spanduk, 0x1A1A1A);
                    Crowd2D(-8.5f, 34, 0xF6C21B);
                }
                else
                {
                    Banner(new Vector3(-8, 2.6f, -5.5f), 4, 0xF3EEE1, Palette.Cabai);
                    Props(4, 0x666666, PropShape.Cylinder, 0.45f, 3f);
                }
                break;

            case "halte-transjakarta":
                var glass = _scene.CreateMaterial(MaterialOptions.Pbr(Palette.Hex(0xA8C8E8, 0.35f), 0.1f, 0.05f) with { AlphaMode = AlphaMode.Blend, DepthWrite = false });
                _c.Add(_c.Box, glass, new Vector3(0, 1.6f, -4.5f), new Vector3(16, 3.2f, 0.08f));
                Box(0xE0322B, new Vector3(0, 3.35f, -4.2f), new Vector3(17, 0.3f, 2.4f), rough: 0.4f);
                Box(0x5A6470, new Vector3(0, 0.35f, -5.5f), new Vector3(18, 0.7f, 2.4f));
                Bus(new Vector3(-2, 0, -8.5f), 0x1F4FB0);
                for (int i = 0; i < 4; i++) Lamp(new Vector3(-12 + i * 8, 0, -3.2f), 0xFFE2B8, rig);
                for (int i = 0; i < 10; i++) House(new Vector3(-30 + i * 7, 0, -22), 6, R(10, 26), Palette.Mix(0x2B3A55, 0x4A5A75, R(0, 1)), 0x1A2233);
                break;

            case "terminal-kampung-rambutan":
                Box(0x8A8478, new Vector3(0, 4.5f, -6), new Vector3(30, 0.3f, 8), rough: 0.6f);
                for (int i = -3; i <= 3; i++) Cyl(0x9A9488, new Vector3(i * 4.5f, 0, -3), 0.2f, 4.5f);
                Bus(new Vector3(-6, 0, -8), 0xF28C1B);
                Bus(new Vector3(7, 0, -8.5f), 0x2E8B57, 0.05f);
                Banner(new Vector3(0, 3.6f, -2.9f), 7, Palette.Spanduk, 0x1F4FB0);
                Props(6, 0x8C6A45, PropShape.Box, 0.55f);
                break;

            case "kampung-ambon":
                // Colourful kampung houses (Blender MCP) with porches, shutters and laundry; boxes as a fallback.
                for (int i = 0; i < 6; i++)
                {
                    var hp = new Vector3(-15 + i * 6, 0, i % 2 == 0 ? -6.2f : -7.2f);
                    string variant = $"house-ambon-{(char)('a' + i % 3)}";
                    if (DecorMesh(variant) is not null) Decor(variant, hp, R(4.2f, 4.8f), rig, yaw: R(-0.06f, 0.06f));
                    else House(hp, 5.2f, R(3.5f, 5.5f), Palette.Mix(0xC9A27E, 0x8EB3C9, R(0, 1)), 0x6A3A2A);
                }
                StringLights(-3.6f, 3.6f, 24, rig);
                StringLights(-4.8f, 4.2f, 24, rig);
                for (int i = 0; i < 3; i++) Lamp(new Vector3(-9 + i * 9, 0, -3.4f), 0xFFB86B, rig);
                Props(4, 0x2E5C8A, PropShape.Cylinder, 0.45f, 2f);
                break;

            case "hutan-kalimantan":
            case "hutan-gambut-riau":
                bool burning = _stage.Id == "hutan-gambut-riau";
                for (int i = 0; i < 18; i++)
                {
                    var pos = new Vector3(R(-26, 26), 0, R(-18, -4.5f));
                    if (burning) Cyl(0x2A1E16, pos, R(0.15f, 0.35f), R(4, 10));
                    else Tree(pos, R(6, 12), Palette.Mix(0x2F5A2A, 0x4A7A3A, R(0, 1)));
                }
                for (int i = 0; i < 6; i++)
                    _c.Add(_c.Cylinder, _c.Pbr(0x3A5A2A, 0.8f), new Vector3(R(-12, 12), 6, R(-6, -4)), new Vector3(0.06f, 9, 0.06f), euler: new Vector3(R(-0.2f, 0.2f), 0, R(-0.3f, 0.3f)));
                if (burning)
                {
                    for (int i = 0; i < 5; i++) Ball(0xFF6A1A, new Vector3(R(-14, 14), 0.1f, R(-9, -5)), R(0.8f, 1.6f), 0.4f, 5f);
                    Box(0xF28C1B, new Vector3(-10, 0.6f, -4), new Vector3(1.2f, 1.2f, 0.6f)); // water pump
                }
                Props(5, burning ? 0x3A2A1Eu : 0x6E8B3Au, PropShape.Sphere, 0.35f, 0.8f);
                break;

            case "pantai-selatan":
                Box(0x2E4A6A, new Vector3(0, -0.15f, -26), new Vector3(140, 0.2f, 40), rough: 0.05f);
                for (int i = 0; i < 5; i++)
                    _c.Add(_c.Box, _c.Pbr(0xDCE8EE, 0.3f), new Vector3(R(-30, 30), 0.1f, -7 - i * 1.6f), new Vector3(R(6, 14), 0.08f, 0.4f));
                for (int i = 0; i < 6; i++) Tree(new Vector3(i % 2 == 0 ? -14 - i : 14 + i, 0, R(-6, -3)), R(5, 7), 0x2E5A3A, palm: true);
                Box(0x0E7A52, new Vector3(0, 3, -12), new Vector3(0.1f, 6, 0.1f));
                Box(0x0E7A52, new Vector3(0.6f, 5.6f, -12), new Vector3(1.2f, 0.8f, 0.04f), emissive: 0.5f);
                break;

            case "kota-tua":
                House(new Vector3(0, 0, -10), 26, 9, 0xE8D8B8, 0x8C3A20);
                for (int i = -5; i <= 5; i++) Box(0xF3EEE1, new Vector3(i * 2.4f, 4.5f, -8.4f), new Vector3(0.5f, 9, 0.3f));
                Box(0xD4AF37, new Vector3(0, 10.3f, -9), new Vector3(2.5f, 2.5f, 1), rough: 0.3f);
                for (int i = 0; i < 4; i++) Tree(new Vector3(-12 + i * 8, 0, -5.5f), 4.5f, 0x4A6A3A);
                Props(4, 0x8C1C13, PropShape.Box, 0.5f);
                break;

            case "gunung-bromo":
                _c.Add(_c.Cone, _c.Pbr(0x6A5A4E, 0.95f), new Vector3(-6, 8, -40), new Vector3(36, 18, 30));
                _c.Add(_c.Cylinder, _c.Pbr(0x3A2E26, 0.95f), new Vector3(-6, 17, -40), new Vector3(8, 0.5f, 8));
                _c.Add(_c.Cone, _c.Pbr(0x7A6A5E, 0.95f), new Vector3(18, 6, -52), new Vector3(30, 13, 26));
                for (int i = 0; i < 10; i++) Ball(0x8A7A6E, new Vector3(R(-20, 20), -0.3f, R(-14, -5)), R(0.8f, 2.5f), 0.95f);
                break;

            case "candi-borobudur":
                for (int i = 0; i < 6; i++)
                    Box(0x7A7468, new Vector3(0, i * 1.1f + 0.55f, -10 - i * 1.2f), new Vector3(38 - i * 4.5f, 1.1f, 6 - i * 0.3f), rough: 0.95f);
                for (int i = 0; i < 9; i++) Stupa(new Vector3(-12 + i * 3, 3.3f, -11.5f), 1.4f);
                for (int i = 0; i < 6; i++) Stupa(new Vector3(-7.5f + i * 3, 5.5f, -13.8f), 1.6f);
                Stupa(new Vector3(0, 6.6f, -16.5f), 3.2f);
                for (int i = 0; i < 5; i++) Tree(new Vector3(R(-25, 25), 0, R(-25, -18)), R(4, 6), 0x3A5A3A, palm: true);
                break;

            case "karnaval-sound-horeg":
                SpeakerWall(new Vector3(-10f, 0, -6f), 3, 3, rig);
                SpeakerWall(new Vector3(10.5f, 0, -6.5f), 3, 3, rig);
                for (int i = 0; i < 4; i++) House(new Vector3(-18 + i * 12, 0, -14), 8, 4, Palette.Mix(0xC9B78F, 0xB8C4C9, R(0, 1)), 0x8C3A20);
                StringLights(-4.2f, 5.6f, 26, rig);
                for (int i = 0; i < 3; i++) Lamp(new Vector3(-10 + i * 10, 0, -4f), i % 2 == 0 ? 0xFF3EA5u : 0x7CFC00u, rig, 8f);
                Crowd2D(-10.5f, 30, 0x7CFC00);
                Props(5, 0x8C3A20, PropShape.Box, 0.35f, 0.5f); // loose roof tiles
                break;

            case "alun-alun-ponorogo":
                Box(0x3A3A2A, new Vector3(0, 0.02f, -9), new Vector3(40, 0.04f, 10));
                Tree(new Vector3(-6, 0, -12), 9, 0x2F4A2A);
                Tree(new Vector3(6, 0, -12), 9, 0x2F4A2A);
                Box(0xE8D8B8, new Vector3(0, 3, -15), new Vector3(12, 6, 3));
                Box(0x2E8B57, new Vector3(0, 6.4f, -15), new Vector3(13, 0.8f, 3.4f));
                for (int i = 0; i < 4; i++) Lamp(new Vector3(-12 + i * 8, 0, -4f), 0xFFD39A, rig);
                Crowd2D(-7.5f, 30, 0xB22222);
                Banner(new Vector3(0, 3.5f, -6), 6, 0xB22222, Palette.Spanduk);
                break;

            case "malam-pengerupukan":
                // The crossroads with a giant ogoh-ogoh effigy being spun by its bearers.
                var effigy = _scene.CreateNode(name: "ogoh-ogoh");
                effigy.Position = new Vector3(4, 0, -8);
                _c.Add(_c.Box, _c.Pbr(0x6B4A2B, 0.8f), new Vector3(0, 0.6f, 0), new Vector3(4, 0.3f, 4), effigy);
                _c.Add(_c.Capsule, _c.Pbr(0xC0392B, 0.6f), new Vector3(0, 3.5f, 0), new Vector3(1.8f, 2.2f, 1.4f), effigy);
                _c.Add(_c.Sphere, _c.Pbr(0x8E44AD, 0.6f), new Vector3(0, 6.6f, 0), new Vector3(2.2f), effigy);
                _c.Add(_c.Sphere, _c.Pbr(0xFFFFFF, 0.2f, 0, 3f), new Vector3(0.45f, 6.9f, 1.0f), new Vector3(0.5f), effigy);
                _c.Add(_c.Sphere, _c.Pbr(0xFFFFFF, 0.2f, 0, 3f), new Vector3(-0.45f, 6.9f, 1.0f), new Vector3(0.5f), effigy);
                foreach (int s in (int[])[1, -1])
                    _c.Add(_c.Cone, _c.Pbr(0xF3EEE1, 0.4f), new Vector3(s * 0.4f, 6.0f, 1.0f), new Vector3(0.25f, 0.8f, 0.25f), effigy, new Vector3(MathF.PI, 0, 0));
                rig.Spinners.Add((effigy, 0.6f));
                for (int i = 0; i < 6; i++) Lamp(new Vector3(-14 + i * 5.6f, 0, -4.5f), 0xFF8A3A, rig, 7f);
                for (int i = 0; i < 4; i++) House(new Vector3(-16 + i * 11, 0, -14), 8, 4, 0x8A6A4A, 0x3A2A1E);
                Crowd2D(-10.5f, 28, 0x3A2A3A);
                break;

            case "pangkalan-ojol":
                Box(0x6A6E75, new Vector3(0, 7, -6), new Vector3(60, 1.2f, 6), rough: 0.7f);
                for (int i = -3; i <= 3; i++) Box(0x7A7E85, new Vector3(i * 7f, 3.2f, -6), new Vector3(1.4f, 6.4f, 1.4f));
                for (int i = 0; i < 9; i++)
                {
                    // The parked ojol fleet: the authored scooter when it exists, a primitive one otherwise.
                    if (DecorMesh("motorbike") is not null)
                    {
                        Decor("motorbike", new Vector3(-12 + i * 3, 0, -4.2f - (i % 3) * 0.6f), 1.2f, rig, yaw: 0.4f + MathF.PI / 2);
                        continue;
                    }
                    var bike = _scene.CreateNode(name: "motor");
                    bike.Position = new Vector3(-12 + i * 3, 0, -4.2f - (i % 3) * 0.6f);
                    bike.EulerAngles = new Vector3(0, 0.4f, 0);
                    _c.Add(_c.Box, _c.Pbr(i % 2 == 0 ? 0x00A651u : 0x1A1A1Au, 0.4f, 0.3f), new Vector3(0, 0.6f, 0), new Vector3(0.35f, 0.4f, 1.6f), bike);
                    foreach (float z in new[] { -0.6f, 0.6f })
                        _c.Add(_c.Cylinder, _c.Pbr(0x111111, 0.9f), new Vector3(0, 0.3f, z), new Vector3(0.55f, 0.12f, 0.55f), bike, new Vector3(0, 0, MathF.PI / 2));
                }
                Box(0x8C5A2B, new Vector3(9, 1, -7), new Vector3(3, 2, 1.6f)); // coffee stall
                for (int i = 0; i < 3; i++) Lamp(new Vector3(-8 + i * 8, 0, -3.5f), 0xFFE2B8, rig);
                break;

            case "ruang-sidang":
                Box(0x5A3A24, new Vector3(0, 4, -6.5f), new Vector3(24, 8, 0.3f), rough: 0.5f);
                Box(0xE0322B, new Vector3(-5, 5.5f, -6.3f), new Vector3(2.4f, 0.8f, 0.05f));
                Box(0xF3EEE1, new Vector3(-5, 4.7f, -6.3f), new Vector3(2.4f, 0.8f, 0.05f));
                Props(4, 0xF3EEE1, PropShape.Box, 0.35f, 0.5f); // case files
                break;

            default:
                for (int i = 0; i < 6; i++) House(new Vector3(-15 + i * 6, 0, -8), 5, 4, 0xC9B78F, 0x8C3A20);
                break;
        }

        // Every arena gets the tournament's own spanduk on the back line.
        if (_stage.Id is not ("ruang-sidang" or "pantai-selatan" or "gunung-bromo" or "pasar-becek"))
            TournamentBanner(new Vector3(-hw + 2f, 2.5f, -3.6f), 3.6f);
        _ = p;
    }
}
