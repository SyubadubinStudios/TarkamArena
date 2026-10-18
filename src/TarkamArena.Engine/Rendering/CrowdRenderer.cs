using System.Numerics;
using TarkamArena.Core.Arena;
using TarkamArena.Core.Content;
using TarkamArena.Physics;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>
/// Draws the Living Arena NPCs with GPU instancing: one body and one head draw call per NPC role,
/// whatever the crowd size. Transform buffers are reused every frame, so it does not allocate.
/// </summary>
public sealed class CrowdRenderer
{
    private sealed class Group
    {
        public required NpcKind Kind;
        public required Node Body;
        public Node? Head;
        public bool Authored;
        public required Node Prop;
        public Matrix4x4[] BodyXf = [];
        public Matrix4x4[] HeadXf = [];
        public Matrix4x4[] PropXf = [];
        public int Count;
    }

    private readonly Group[] _groups;
    private readonly Dictionary<NpcKind, int> _groupIndex = [];

    public CrowdRenderer(Scene scene, MeshCache cache, StageDefinition stage, int capacity)
    {
        var kinds = stage.Npcs.Distinct().ToArray();
        _groups = new Group[kinds.Length];
        for (int i = 0; i < kinds.Length; i++)
        {
            var (bodyColor, headColor, propColor) = Colors(kinds[i]);
            bool animal = IsAnimal(kinds[i]);
            var prop = scene.AddMesh(cache.Box, cache.Pbr(propColor, 0.5f, 0, kinds[i] is NpcKind.Photographer or NpcKind.Journalist ? 1.5f : 0f));
            Group g;
            if (!animal && CrowdModel(scene, Variant(kinds[i])) is { } model)
            {
                // Authored crowd extra (Rodin -> Blender, 2.5k triangles), tinted per role and drawn instanced.
                var options = model.Material.Options;
                options.BaseColor *= Palette.Hex(Palette.Lighten(bodyColor, 0.55f));
                var body = scene.CreateNode(name: $"npc-{kinds[i]}");
                body.AttachMesh(model.Geometry, scene.CreateMaterial(options));
                g = new Group { Kind = kinds[i], Body = body, Head = null, Prop = prop, Authored = true };
            }
            else
            {
                g = new Group
                {
                    Kind = kinds[i],
                    Body = scene.AddMesh(animal ? cache.LowSphere : cache.Capsule, cache.Pbr(bodyColor, 0.85f), name: $"npc-{kinds[i]}"),
                    Head = scene.AddMesh(cache.LowSphere, cache.Pbr(headColor, 0.7f)),
                    Prop = prop,
                };
                g.Head.CastShadow = false;
            }
            int cap = capacity / kinds.Length + 2;
            g.BodyXf = new Matrix4x4[cap];
            g.HeadXf = new Matrix4x4[cap];
            g.PropXf = new Matrix4x4[cap];
            g.Body.CastShadow = true;
            g.Prop.CastShadow = false;
            _groups[i] = g;
            _groupIndex[kinds[i]] = i;
        }
    }

    private static string Variant(NpcKind kind) => kind switch
    {
        NpcKind.Kid => "kid",
        NpcKind.Trader or NpcKind.VegetableSeller or NpcKind.Hawker or NpcKind.Farmer or NpcKind.Fisherman or NpcKind.Villager => "vendor",
        NpcKind.Shopper or NpcKind.Student or NpcKind.Photographer or NpcKind.Tourist or NpcKind.Dancer or NpcKind.Commuter or NpcKind.Lawyer => "woman",
        _ => "man",
    };

    private static readonly Dictionary<(Scene, string), (Geometry Geometry, Material Material)?> s_models = [];

    /// <summary>Loads a crowd extra once per scene and returns its mesh, or null when the asset is missing.</summary>
    private static (Geometry Geometry, Material Material)? CrowdModel(Scene scene, string variant)
    {
        if (s_models.TryGetValue((scene, variant), out var cached)) return cached;
        (Geometry, Material)? result = null;
        var path = Assets.AssetLocator.PathOf($"models/crowd/crowd-{variant}.glb");
        if (File.Exists(path))
        {
            var holder = scene.CreateNode(name: $"crowd-proto-{variant}");
            holder.Visible = false;
            var import = scene.LoadGltf(path, holder);
            var mesh = FindMesh(import.Root);
            if (mesh is not null) result = (mesh.Geometry!, mesh.Material!);
        }
        s_models[(scene, variant)] = result;
        return result;
    }

    private static Node? FindMesh(Node node)
    {
        if (node.Geometry is not null && node.Material is not null) return node;
        foreach (var child in node.Children)
            if (FindMesh(child) is { } found) return found;
        return null;
    }

    private static bool IsAnimal(NpcKind k) => k is NpcKind.Chicken or NpcKind.Goat or NpcKind.Buffalo or NpcKind.OrangUtan;

    private static (uint Body, uint Head, uint Prop) Colors(NpcKind kind) => kind switch
    {
        NpcKind.Trader or NpcKind.VegetableSeller => (0x8C5A2B, 0xB98055, 0x2E8B57),
        NpcKind.Shopper => (0x9C27B0, 0xC48D62, 0xF3EEE1),
        NpcKind.Porter => (0x5A5A5A, 0xA86A44, 0x8C6A45),
        NpcKind.Driver or NpcKind.Conductor => (0x2C3E66, 0xA86A44, 0x1A1A1A),
        NpcKind.Passenger or NpcKind.Commuter => (0x6E7680, 0xC48D62, 0x2A2A2A),
        NpcKind.Hawker => (0xF28C1B, 0xB07A50, 0xF3EEE1),
        NpcKind.Ranger => (0x3E4A3D, 0xA86A44, 0x2E8B57),
        NpcKind.Researcher => (0xF3EEE1, 0xD49A6E, 0x1F4FB0),
        NpcKind.OrangUtan => (0xB4521E, 0x8A3A12, 0xB4521E),
        NpcKind.Farmer => (0x5A4632, 0xA86A44, 0xC9B03A),
        NpcKind.Buffalo => (0x3A3430, 0x2A2420, 0x3A3430),
        NpcKind.Politician => (0x1A1A1A, 0xC48D62, 0x8C5A2B),
        NpcKind.Journalist or NpcKind.Photographer => (0x3A4A5A, 0xD49A6E, 0x111111),
        NpcKind.Officer => (0x5A4A3A, 0xA86A44, 0x1A1A1A),
        NpcKind.Tourist => (0xF6C21B, 0xE8C8A8, 0x111111),
        NpcKind.Fisherman => (0x2E5C8A, 0xA86A44, 0x8C6A45),
        NpcKind.Monk => (0xF28C1B, 0xC48D62, 0xF6C21B),
        NpcKind.Kid => (0xE0322B, 0xC48D62, 0xF6C21B),
        NpcKind.Chicken => (0xF3EEE1, 0xE0322B, 0xF6C21B),
        NpcKind.Goat => (0xDCD6C8, 0xA8A090, 0x3A3A3A),
        NpcKind.Musician => (0x161616, 0xB57B52, 0xB8862B),
        NpcKind.Dancer => (0x7CFC00, 0xD49A6E, 0xFF3EA5),
        NpcKind.SoundCrew => (0x1A1A1A, 0xA86A44, 0x333333),
        NpcKind.Firefighter => (0xF28C1B, 0xA86A44, 0xF6C21B),
        NpcKind.Villager => (0x6B5B45, 0xB07A50, 0x8C6A45),
        NpcKind.Warok => (0x111111, 0xA86A44, 0xE0322B),
        NpcKind.Pecalang => (0x111111, 0xB07A50, 0xF3EEE1),
        NpcKind.Student => (0xF6C21B, 0xC48D62, 0xF3EEE1),
        NpcKind.Judge => (0x2A1A1A, 0xC48D62, 0xE0322B),
        NpcKind.OjolDriver => (0x00A651, 0xB07A50, 0x00A651),
        NpcKind.Lawyer => (0x1A1A2A, 0xD49A6E, 0x8C5A2B),
        _ => (0x6B5B45, 0xB07A50, 0x8C6A45),
    };

    public void Update(ReadOnlySpan<NpcAgent> agents)
    {
        foreach (var g in _groups) g.Count = 0;
        foreach (ref readonly var a in agents)
        {
            if (!_groupIndex.TryGetValue(a.Kind, out int gi)) continue;
            var g = _groups[gi];
            if (g.Count >= g.BodyXf.Length) continue;
            bool animal = IsAnimal(a.Kind);
            float scale = a.Kind switch { NpcKind.Kid => 0.65f, NpcKind.Chicken => 0.3f, NpcKind.Goat => 0.6f, NpcKind.Buffalo => 1.3f, _ => 1f };
            var rot = Matrix4x4.CreateRotationY(a.Yaw);
            var basePos = a.Position + new Vector3(0, a.Bounce, 0);
            if (g.Authored)
            {
                float s = (0.92f + (a.Seed & 15) / 100f) * (a.Kind == NpcKind.Kid ? 1f : 1f);
                g.BodyXf[g.Count] = Matrix4x4.CreateScale(s) * rot * Matrix4x4.CreateTranslation(basePos);
            }
            else
            {
                var bodySize = animal ? new Vector3(0.5f, 0.45f, 0.8f) * scale : new Vector3(0.42f, 0.62f, 0.32f) * scale;
                g.BodyXf[g.Count] = Matrix4x4.CreateScale(bodySize) * rot * Matrix4x4.CreateTranslation(basePos + new Vector3(0, animal ? 0.3f * scale : 0.62f * scale, 0));
                var headPos = animal ? new Vector3(0, 0.45f, 0.4f) * scale : new Vector3(0, 1.4f, 0) * scale;
                g.HeadXf[g.Count] = Matrix4x4.CreateScale(0.3f * scale) * Matrix4x4.CreateTranslation(Vector3.Transform(headPos, rot) + basePos);
            }

            // A held prop: phone held up when filming, arms up when cheering, goods when trading.
            Vector3 propOffset = a.Activity switch
            {
                NpcActivity.Record => new Vector3(0, 1.5f, 0.32f),
                NpcActivity.Cheer => new Vector3(0.25f, 1.7f, 0),
                NpcActivity.Trade => new Vector3(0, 1.0f, 0.35f),
                _ => new Vector3(0.28f, 0.9f, 0.05f),
            } * scale;
            var propSize = a.Activity == NpcActivity.Record ? new Vector3(0.12f, 0.2f, 0.03f) : new Vector3(0.12f, 0.12f, 0.12f);
            g.PropXf[g.Count] = Matrix4x4.CreateScale(propSize * scale) * rot * Matrix4x4.CreateTranslation(Vector3.Transform(propOffset, rot) + basePos);
            g.Count++;
        }

        foreach (var g in _groups)
        {
            g.Body.SetInstances(g.BodyXf.AsSpan(0, g.Count));
            g.Head?.SetInstances(g.HeadXf.AsSpan(0, g.Count));
            g.Prop.SetInstances(g.PropXf.AsSpan(0, g.Count));
        }
    }
}

/// <summary>Mirrors <see cref="ArenaPhysics"/> props onto scene nodes.</summary>
public sealed class PropRenderer
{
    private readonly Scene _scene;
    private readonly MeshCache _cache;
    private readonly List<Node?> _nodes = [];
    private readonly List<uint> _colors = [];

    public PropRenderer(Scene scene, MeshCache cache)
    {
        _scene = scene;
        _cache = cache;
    }

    public void Update(ArenaPhysics physics)
    {
        var props = physics.Props;
        for (int i = 0; i < props.Length; i++)
        {
            ref readonly var p = ref props[i];
            while (_nodes.Count <= i)
            {
                _nodes.Add(null);
                _colors.Add(0);
            }
            var node = _nodes[i];
            if (!p.Alive)
            {
                if (node is not null) node.Visible = false;
                continue;
            }
            if (node is null || _colors[i] != p.Color)
            {
                node?.Remove();
                var geometry = p.Shape switch { PropShape.Sphere => _cache.Sphere, PropShape.Cylinder => _cache.Cylinder, _ => _cache.Box };
                node = _scene.AddMesh(geometry, _cache.Pbr(p.Color, 0.8f), name: "prop");
                _nodes[i] = node;
                _colors[i] = p.Color;
            }
            node.Visible = true;
            node.SetTransform(p.Position, p.Orientation, p.Size);
        }
    }
}
