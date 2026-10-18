using System.Numerics;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>Colour helpers. Content stores sRGB hex; the renderer wants linear RGBA.</summary>
public static class Palette
{
    public static Vector4 Hex(uint rgb, float alpha = 1f) => MathHelpers.FromHex(rgb, alpha);

    public static Vector3 Hex3(uint rgb)
    {
        var v = MathHelpers.FromHex(rgb, 1f);
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static uint Mix(uint a, uint b, float t)
    {
        static float C(uint c, int shift) => (c >> shift) & 0xFF;
        uint r = (uint)(C(a, 16) + (C(b, 16) - C(a, 16)) * t);
        uint g = (uint)(C(a, 8) + (C(b, 8) - C(a, 8)) * t);
        uint bl = (uint)(C(a, 0) + (C(b, 0) - C(a, 0)) * t);
        return (r << 16) | (g << 8) | bl;
    }

    public static uint Darken(uint c, float amount) => Mix(c, 0x000000, amount);

    public static uint Lighten(uint c, float amount) => Mix(c, 0xFFFFFF, amount);

    // Brand tokens shared with the Avalonia UI.
    public const uint Terpal = 0x1F3F9E;
    public const uint TerpalDeep = 0x112459;
    public const uint Spanduk = 0xF6C21B;
    public const uint Cabai = 0xE0322B;
    public const uint Kapur = 0xF3EEE1;
    public const uint Arang = 0x14141A;
    public const uint DaunPisang = 0x2E8B57;
}

/// <summary>
/// Caches geometries and materials so building a stage or a fighter does not create the same
/// primitive twice. Everything belongs to one <see cref="Scene"/> and dies with it.
/// </summary>
public sealed class MeshCache(Scene scene)
{
    private readonly Dictionary<string, Geometry> _geometries = new(StringComparer.Ordinal);
    private readonly Dictionary<(uint, float, float, float), Material> _materials = [];

    public Scene Scene { get; } = scene;

    public Geometry Box => Get("box", () => Scene.CreateBoxGeometry());
    public Geometry Sphere => Get("sphere", () => Scene.CreateSphereGeometry(0.5f, 20, 12));
    public Geometry LowSphere => Get("sphere-low", () => Scene.CreateSphereGeometry(0.5f, 10, 6));
    public Geometry Cylinder => Get("cylinder", () => Scene.CreateCylinderGeometry(0.5f, 0.5f, 1f, 16));
    public Geometry Cone => Get("cone", () => Scene.CreateConeGeometry(0.5f, 1f, 18));
    /// <summary>Radius 0.5 with a straight part of 1 (2 units tall). Built here because the 0.12 built-in capsule has inverted shading.</summary>
    public Geometry Capsule => Get("capsule", () => BuildCapsule(0.5f, 1f, 14, 5));
    public Geometry Plane => Get("plane", () => Scene.CreatePlaneGeometry(1, 1));
    public Geometry Torus => Get("torus", () => Scene.CreateTorusGeometry(0.5f, 0.12f, 10, 24));

    private Geometry BuildCapsule(float radius, float straight, int segments, int rings)
    {
        int rows = rings * 2 + 2;
        var vertices = new ThreeNet.Interop.Vertex[rows * (segments + 1)];
        var indices = new uint[(rows - 1) * segments * 6];
        int v = 0;
        for (int r = 0; r < rows; r++)
        {
            float ny, ring, y;
            if (r <= rings)
            {
                float a = MathF.PI / 2 * (1 - r / (float)rings);
                ny = MathF.Sin(a);
                ring = MathF.Cos(a);
                y = straight / 2 + radius * ny;
            }
            else
            {
                float a = MathF.PI / 2 * ((r - rings - 1) / (float)rings);
                ny = -MathF.Sin(a);
                ring = MathF.Cos(a);
                y = -straight / 2 + radius * ny;
            }
            for (int s = 0; s <= segments; s++)
            {
                float t = s / (float)segments * MathF.PI * 2;
                var n = new Vector3(MathF.Cos(t) * ring, ny, MathF.Sin(t) * ring);
                vertices[v++] = new ThreeNet.Interop.Vertex(new Vector3(n.X * radius, y, n.Z * radius), Vector3.Normalize(n + new Vector3(0, 1e-6f, 0)), new Vector2(s / (float)segments, r / (float)(rows - 1)));
            }
        }
        int k = 0;
        for (int r = 0; r < rows - 1; r++)
        for (int s = 0; s < segments; s++)
        {
            uint a = (uint)(r * (segments + 1) + s), b = a + 1, c = (uint)((r + 1) * (segments + 1) + s), d = c + 1;
            indices[k++] = a; indices[k++] = b; indices[k++] = c;
            indices[k++] = b; indices[k++] = d; indices[k++] = c;
        }
        return Scene.CreateGeometry(vertices, indices);
    }

    public Geometry Get(string key, Func<Geometry> create)
    {
        if (!_geometries.TryGetValue(key, out var g))
        {
            g = create();
            _geometries[key] = g;
        }
        return g;
    }

    public Material Pbr(uint color, float roughness = 0.7f, float metallic = 0f, float emissive = 0f)
    {
        var key = (color, roughness, metallic, emissive);
        if (_materials.TryGetValue(key, out var m)) return m;
        var options = MaterialOptions.Pbr(Palette.Hex(color), metallic, roughness);
        if (emissive > 0)
        {
            options.Emissive = Palette.Hex3(color);
            options.EmissiveIntensity = emissive;
        }
        m = Scene.CreateMaterial(options);
        _materials[key] = m;
        return m;
    }

    public Material Unlit(uint color, float alpha = 1f)
    {
        var key = (color, -1f, alpha, 0f);
        if (_materials.TryGetValue(key, out var m)) return m;
        var options = MaterialOptions.Basic(Palette.Hex(color, alpha));
        if (alpha < 1f)
        {
            options.AlphaMode = AlphaMode.Blend;
            options.DepthWrite = false;
        }
        m = Scene.CreateMaterial(options);
        _materials[key] = m;
        return m;
    }

    /// <summary>Adds a mesh node with a transform in one call.</summary>
    public Node Add(Geometry geometry, Material material, Vector3 position, Vector3 scale, Node? parent = null, Vector3? euler = null)
    {
        var node = Scene.AddMesh(geometry, material, parent);
        node.Position = position;
        node.Scale = scale;
        if (euler is { } e) node.EulerAngles = e;
        return node;
    }
}
