using System.Numerics;
using TarkamArena.Core.Content;
using TarkamArena.Engine.Assets;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>A node that bends in the wind about its base (trees, bushes, banners, flags).</summary>
public readonly record struct Swayer(Node Node, Quaternion Base, float Amplitude, float Speed, float Phase);

/// <summary>A node that flickers by scaling (bulbs, neon, embers).</summary>
public readonly record struct Blinker(Node Node, Vector3 Base, float Speed, float Phase, bool Chase);

/// <summary>A node that rattles on the music's beat (speaker cabinets, trucks, loose zinc).</summary>
public readonly record struct Shaker(Node Node, Vector3 Base, float Amplitude);

/// <summary>Instanced ground clutter (grass, ferns, flowers, litter) whose instances sway individually.</summary>
public sealed class ScatterGroup
{
    public required Node Node { get; init; }
    public required Matrix4x4[] Base { get; init; }
    public required Matrix4x4[] Work { get; init; }
    public required float[] Phase { get; init; }
    public float Amplitude { get; init; }
}

/// <summary>
/// Set dressing that makes every arena feel inhabited: Rodin-generated decor (trees, bushes, rocks,
/// boats, carts, statues…) placed around the fighting strip, instanced ground clutter, and the lists
/// of nodes the presenter animates: wind sway, blinking lights and beat-shaken speakers. Decor models
/// live in <c>assets/models/decor/&lt;id&gt;.glb</c>, normalised to 1 m tall with the origin at their base;
/// when one is missing a primitive stand-in is built so the arena never looks empty.
/// </summary>
public sealed partial class StageBuilder
{
    private float Wind => _stage.Weather switch
    {
        Weather.Storm => 2.2f,
        Weather.Rain => 1.5f,
        Weather.Cloudy => 1.1f,
        _ => 0.8f,
    };

    private readonly Dictionary<string, (Geometry Geometry, Material Material)?> _decorMeshes = [];

    /// <summary>Loads the single mesh of a decor model (or null when the asset is missing).</summary>
    private (Geometry Geometry, Material Material)? DecorMesh(string id)
    {
        if (_decorMeshes.TryGetValue(id, out var cached)) return cached;
        (Geometry, Material)? result = null;
        var path = AssetLocator.PathOf($"models/decor/{id}.glb");
        if (File.Exists(path))
        {
            var holder = _scene.CreateNode(name: $"decor-proto-{id}");
            holder.Visible = false;
            try
            {
                var import = _scene.LoadGltf(path, holder);
                if (FindMesh(import.Root) is { } mesh) result = (mesh.Geometry!, mesh.Material!);
            }
            catch (ThreeNetException e)
            {
                Console.Error.WriteLine($"decor {id}: {e.Message}");
                // A broken decor file must not take the arena down; the stand-in is used instead.
            }
        }
        _decorMeshes[id] = result;
        return result;
    }

    private static Node? FindMesh(Node node)
    {
        if (node.Geometry is not null && node.Material is not null) return node;
        foreach (var child in node.Children)
            if (FindMesh(child) is { } found) return found;
        return null;
    }

    /// <summary>
    /// Places one decor piece <paramref name="height"/> metres tall. <paramref name="sway"/> is how far it
    /// bends in the wind (radians at full gust); 0 keeps it still.
    /// </summary>
    private Node? Decor(string id, Vector3 pos, float height, StageRig rig, float sway = 0, float yaw = float.NaN)
    {
        if (_hasBackdrop && pos.Z < -8.5f) return null;
        if (float.IsNaN(yaw)) yaw = R(0, MathF.Tau);
        var node = _scene.CreateNode(name: $"decor-{id}");
        node.Position = pos;
        node.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        if (DecorMesh(id) is { } mesh)
        {
            var child = _scene.CreateNode(node);
            child.AttachMesh(mesh.Geometry, mesh.Material);
            child.Scale = new Vector3(height);
            child.CastShadow = height > 0.8f;
            child.ReceiveShadow = true;
        }
        else
        {
            StandIn(id, node, height);
        }
        if (sway > 0) rig.Swayers.Add(new Swayer(node, node.Rotation, sway * Wind, R(0.8f, 1.4f), R(0, MathF.Tau)));
        return node;
    }

    /// <summary>Primitive stand-ins for decor models that have not been generated yet.</summary>
    private void StandIn(string id, Node parent, float h)
    {
        Vector3 V(float x, float y, float z) => new(x * h, y * h, z * h);
        switch (id)
        {
            case "bush" or "fern":
                _c.Add(_c.LowSphere, _c.Pbr(id == "fern" ? 0x3E7A2Eu : 0x2F5A2Au, 0.85f), V(0, 0.45f, 0), V(1.4f, 0.9f, 1.2f), parent);
                _c.Add(_c.LowSphere, _c.Pbr(0x4A7A3A, 0.85f), V(0.35f, 0.6f, 0.1f), V(0.8f, 0.7f, 0.8f), parent);
                break;
            case "boulder":
                _c.Add(_c.LowSphere, _c.Pbr(0x6A665E, 0.95f), V(0, 0.4f, 0), V(1.5f, 0.9f, 1.2f), parent);
                break;
            case "deadtree":
                _c.Add(_c.Cylinder, _c.Pbr(0x2A1E16, 0.9f), V(0, 0.5f, 0), V(0.08f, 1f, 0.08f), parent);
                _c.Add(_c.Cylinder, _c.Pbr(0x2A1E16, 0.9f), V(0.12f, 0.7f, 0), V(0.04f, 0.4f, 0.04f), parent, new Vector3(0, 0, -0.7f));
                break;
            case "banana" or "palm":
                _c.Add(_c.Cylinder, _c.Pbr(0x6A5A3A, 0.85f), V(0, 0.4f, 0), V(0.06f, 0.8f, 0.06f), parent);
                for (int i = 0; i < 5; i++)
                    _c.Add(_c.Box, _c.Pbr(0x3E7A2E, 0.7f), V(0, 0.85f, 0), V(0.12f, 0.02f, 0.7f), parent, new Vector3(0.6f, i * MathF.Tau / 5, 0));
                break;
            case "bamboo":
                for (int i = 0; i < 5; i++)
                    _c.Add(_c.Cylinder, _c.Pbr(0x7A9A3A, 0.6f), V((i - 2) * 0.08f, 0.5f, (i % 2) * 0.08f), V(0.04f, 1f, 0.04f), parent, new Vector3(0, 0, (i - 2) * 0.05f));
                break;
            default:
                // No primitive reads as a reog mask or a sound truck: without the model, show nothing.
                parent.Visible = false;
                break;
        }
    }

    /// <summary>
    /// Scatters <paramref name="count"/> instances of a decor model over the ground on both sides of the
    /// fighting strip and behind it (never on the strip itself, never in front of the camera).
    /// </summary>
    private void Scatter(string id, int count, float minSize, float maxSize, StageRig rig, float sway, float zMin = -8f, float zMax = -2.6f, bool sides = true)
    {
        if (DecorMesh(id) is not { } mesh)
        {
            // Without the model, fall back to a handful of stand-ins rather than hundreds of primitives.
            for (int i = 0; i < Math.Min(count, 10); i++) Decor(id, ScatterPoint(zMin, zMax, sides), R(minSize, maxSize), rig, sway);
            return;
        }
        var node = _scene.CreateNode(name: $"scatter-{id}");
        node.AttachMesh(mesh.Geometry, mesh.Material);
        node.CastShadow = false;
        node.ReceiveShadow = true;
        var xf = new Matrix4x4[count];
        var phase = new float[count];
        for (int i = 0; i < count; i++)
        {
            var p = ScatterPoint(zMin, zMax, sides);
            xf[i] = Matrix4x4.CreateScale(R(minSize, maxSize)) * Matrix4x4.CreateRotationY(R(0, MathF.Tau)) * Matrix4x4.CreateTranslation(p);
            phase[i] = p.X * 0.35f + R(0, 0.6f);
        }
        node.SetInstances(xf);
        if (sway > 0) rig.Scatters.Add(new ScatterGroup { Node = node, Base = xf, Work = (Matrix4x4[])xf.Clone(), Phase = phase, Amplitude = sway * Wind });
    }

    private Vector3 ScatterPoint(float zMin, float zMax, bool sides)
    {
        float hw = _stage.HalfWidth;
        float z = R(zMin, zMax);
        // In front of the back line only out at the sides, where it frames the shot and never covers the fight.
        if (z > -2.5f || (sides && _random.NextDouble() < 0.3))
        {
            float side = _random.Next(2) == 0 ? -1 : 1;
            return new Vector3(side * R(hw + 1.5f, hw + 9f), 0, z > -2.5f ? z : R(-3f, 4.5f));
        }
        return new Vector3(R(-hw - 10, hw + 10), 0, z);
    }

    /// <summary>Marks bulbs as blinking; <paramref name="chase"/> runs them in a marquee chase.</summary>
    private void Blink(Node node, StageRig rig, float speed = 3f, bool chase = false) =>
        rig.Blinkers.Add(new Blinker(node, node.Scale, speed, R(0, MathF.Tau), chase));

    private void Shake(Node node, StageRig rig, float amplitude = 0.03f) =>
        rig.Shakers.Add(new Shaker(node, node.Position, amplitude));

    /// <summary>A flag or pennant on a pole that waves in the wind.</summary>
    private void Flag(Vector3 pos, float height, uint color, uint stripe, StageRig rig)
    {
        if (_hasBackdrop && pos.Z < -8.5f) return;
        Cyl(0xC0C0C0, pos, 0.04f, height, 0.3f);
        var cloth = _scene.CreateNode(name: "flag");
        cloth.Position = pos + new Vector3(0, height - 0.35f, 0);
        _c.Add(_c.Box, _c.Pbr(color, 0.7f), new Vector3(0.55f, 0.18f, 0), new Vector3(1.1f, 0.36f, 0.02f), cloth);
        _c.Add(_c.Box, _c.Pbr(stripe, 0.7f), new Vector3(0.55f, -0.18f, 0), new Vector3(1.1f, 0.36f, 0.02f), cloth);
        rig.Swayers.Add(new Swayer(cloth, Quaternion.Identity, 0.35f * Wind, 3.2f, R(0, MathF.Tau)));
    }

    /// <summary>A string of pennants (umbul-umbul) zig-zagging across the back of the arena.</summary>
    private void Bunting(float z, float y, float width, StageRig rig)
    {
        if (_hasBackdrop && z < -8.5f) return;
        uint[] colors = [0xE0322B, 0xF3EEE1, 0xF6C21B, 0x1F4FB0, 0x2E8B57];
        int k = 0;
        for (float x = -width / 2; x <= width / 2; x += 0.7f, k++)
        {
            var pennant = _scene.CreateNode(name: "pennant");
            pennant.Position = new Vector3(x, y + MathF.Sin(x * 0.45f) * 0.3f, z);
            _c.Add(_c.Cone, _c.Pbr(colors[k % colors.Length], 0.7f), new Vector3(0, -0.22f, 0), new Vector3(0.36f, 0.44f, 0.03f), pennant, new Vector3(MathF.PI, 0, 0));
            rig.Swayers.Add(new Swayer(pennant, Quaternion.Identity, 0.4f * Wind, 2.6f, x * 0.8f));
        }
    }

    /// <summary>Per-arena dressing on top of the landmark set built by <see cref="BuildSet"/>.</summary>
    private void Dress(StageRig rig)
    {
        float hw = _stage.HalfWidth;
        switch (_stage.Id)
        {
            case "pasar-becek":
                Scatter("crate-stack", 10, 0.7f, 1.1f, rig, 0, -7.5f, -3.2f);
                for (int i = 0; i < 3; i++) Decor("gerobak", new Vector3(i % 2 == 0 ? -hw - 2.5f - i : hw + 2.5f + i, 0, R(-2.5f, 1.5f)), 1.5f, rig, yaw: R(-0.5f, 0.5f) + MathF.PI / 2);
                Decor("banana", new Vector3(-hw - 6, 0, -4), 3.2f, rig, 0.08f);
                Decor("banana", new Vector3(hw + 6, 0, -3.6f), 3.6f, rig, 0.08f);
                Bunting(-3.2f, 4.7f, 22, rig);
                break;

            case "sawah-jawa-tengah":
                Scatter("rice", 140, 0.7f, 1.1f, rig, 0.18f, -8f, -2.8f);
                Scatter("rice", 50, 0.7f, 1.1f, rig, 0.18f, -2.6f, 4.5f, sides: true);
                for (int i = 0; i < 4; i++) Decor("banana", new Vector3(R(-hw - 8, hw + 8), 0, R(-8, -5)), R(2.8f, 3.8f), rig, 0.1f);
                Decor("gubuk", new Vector3(hw + 4, 0, -5), 3f, rig, yaw: -0.4f);
                Decor("scarecrow", new Vector3(-hw - 2, 0, -3.5f), 1.9f, rig, 0.12f, yaw: 0.3f);
                break;

            case "gedung-dpr":
                Scatter("bush", 14, 0.8f, 1.2f, rig, 0.05f, -6.5f, -3f);
                for (int i = 0; i < 6; i++) Flag(new Vector3(-12 + i * 4.8f, 0, -5.6f), 5, 0xE0322B, 0xF3EEE1, rig);
                Decor("mic-podium", new Vector3(hw + 2.5f, 0, -3), 1.4f, rig, yaw: -0.4f);
                break;

            case "jalan-demo":
                Scatter("tire", 12, 0.6f, 0.9f, rig, 0, -6f, -2.8f);
                Scatter("litter", 40, 0.06f, 0.09f, rig, 0, -6f, 3.5f);
                for (int i = 0; i < 4; i++) Flag(new Vector3(-10 + i * 6.5f, 0, -4.4f), 4.2f, i % 2 == 0 ? 0xE0322Bu : 0xF6C21Bu, 0xF3EEE1, rig);
                Decor("water-cannon", new Vector3(-hw - 5, 0, -5), 3.2f, rig, yaw: 0.5f);
                break;

            case "halte-transjakarta":
                Scatter("traffic-cone", 6, 0.6f, 0.8f, rig, 0, -4f, -3.2f);
                Decor("motorbike", new Vector3(hw + 3, 0, -2.5f), 1.2f, rig, yaw: 2.2f);
                Decor("motorbike", new Vector3(-hw - 3.5f, 0, -3f), 1.2f, rig, yaw: 1.2f);
                Decor("gerobak", new Vector3(-hw - 2, 0, 1.5f), 1.5f, rig, yaw: 1.7f);
                break;

            case "terminal-kampung-rambutan":
                Scatter("crate-stack", 8, 0.7f, 1f, rig, 0, -6f, -3f);
                Decor("gerobak", new Vector3(hw + 3, 0, -2.6f), 1.5f, rig, yaw: -1.4f);
                Decor("plastic-chairs", new Vector3(-hw - 3, 0, -2.8f), 0.9f, rig);
                Decor("motorbike", new Vector3(-hw - 4.5f, 0, 1.2f), 1.2f, rig, yaw: 0.8f);
                Bunting(-3.4f, 4.2f, 24, rig);
                break;

            case "kampung-ambon":
                Scatter("bush", 16, 0.7f, 1.2f, rig, 0.06f, -6.5f, -3.4f);
                for (int i = 0; i < 4; i++) Decor("palm", new Vector3(i % 2 == 0 ? -hw - 2 - i * 2 : hw + 2 + i * 2, 0, R(-5.5f, -3.5f)), R(5, 7), rig, 0.07f);
                Decor("plastic-chairs", new Vector3(hw + 2.6f, 0, -2.6f), 0.9f, rig);
                Decor("sasando", new Vector3(-hw - 1.8f, 0, -2.6f), 1.1f, rig, yaw: 0.6f);
                break;

            case "hutan-kalimantan":
                Scatter("fern", 70, 0.6f, 1.3f, rig, 0.12f, -8f, -2.6f);
                Scatter("fern", 26, 0.6f, 1.1f, rig, 0.12f, -2.6f, 4.5f);
                Scatter("boulder", 10, 0.5f, 1.2f, rig, 0, -8f, -3f);
                for (int i = 0; i < 6; i++) Decor("jungle-tree", new Vector3(R(-hw - 10, hw + 10), 0, R(-8.4f, -5.5f)), R(9, 13), rig, 0.03f);
                for (int i = 0; i < 4; i++) Decor("banana", new Vector3(i % 2 == 0 ? -hw - R(1.5f, 6) : hw + R(1.5f, 6), 0, R(-4, 1)), R(2.8f, 3.6f), rig, 0.1f);
                Decor("hornbill-totem", new Vector3(-hw - 1.5f, 0, -3.2f), 2.4f, rig, yaw: 0.4f);
                break;

            case "hutan-gambut-riau":
                Scatter("burnt-stump", 22, 0.6f, 1.4f, rig, 0, -8f, -2.8f);
                Scatter("boulder", 6, 0.4f, 0.9f, rig, 0, -6f, -3f);
                for (int i = 0; i < 7; i++) Decor("deadtree", new Vector3(R(-hw - 10, hw + 10), 0, R(-8.4f, -4.5f)), R(5, 9), rig, 0.02f);
                Decor("fire-truck", new Vector3(hw + 6, 0, -4.5f), 3.2f, rig, yaw: -0.3f);
                Decor("water-hose", new Vector3(-hw - 2, 0, -2.4f), 0.4f, rig);
                break;

            case "pantai-selatan":
                Scatter("seashell", 30, 0.05f, 0.09f, rig, 0, -6f, 4f);
                Scatter("boulder", 6, 0.6f, 1.4f, rig, 0, -7.5f, -4f);
                Decor("jukung", new Vector3(-hw - 3, 0, -4.5f), 1.6f, rig, yaw: 0.4f);
                Decor("jukung", new Vector3(hw + 4, 0, -5.5f), 1.6f, rig, yaw: -0.6f);
                Decor("sesajen", new Vector3(hw + 1.8f, 0, -2.6f), 0.7f, rig);
                for (int i = 0; i < 3; i++) Flag(new Vector3(-6 + i * 6, 0, -6f), 4.5f, 0x0E7A52, 0xF6C21B, rig);
                break;

            case "kota-tua":
                Scatter("bush", 10, 0.7f, 1f, rig, 0.05f, -7f, -4f);
                for (int i = 0; i < 4; i++) Decor("ontel", new Vector3(i % 2 == 0 ? -hw - 1.5f - i : hw + 1.5f + i, 0, R(-3.5f, -2.6f)), 1.1f, rig, yaw: R(0.8f, 2.2f));
                for (int i = 0; i < 4; i++) Decor("colonial-lamp", new Vector3(-12 + i * 8, 0, -4.2f), 3.4f, rig, yaw: 0);
                Decor("ondel-ondel", new Vector3(hw + 2.5f, 0, -3.4f), 2.6f, rig, 0.03f, yaw: -0.3f);
                break;

            case "gunung-bromo":
                Scatter("boulder", 18, 0.5f, 1.6f, rig, 0, -8f, -3f);
                Scatter("dry-grass", 50, 0.4f, 0.8f, rig, 0.2f, -8f, 4f);
                Decor("pony", new Vector3(-hw - 2.5f, 0, -3.2f), 1.6f, rig, yaw: 1.2f);
                Decor("pony", new Vector3(hw + 3.5f, 0, -4.2f), 1.6f, rig, yaw: -1.6f);
                Decor("jeep", new Vector3(hw + 7, 0, -6), 2f, rig, yaw: -0.5f);
                Decor("sesajen", new Vector3(-hw - 1, 0, -2.4f), 0.7f, rig);
                break;

            case "candi-borobudur":
                Scatter("fern", 18, 0.4f, 0.7f, rig, 0.08f, -7f, -3f);
                // Guardian lions flank the fight; Buddhas in their stupas sit along the back terrace.
                foreach (float x in (float[])[-3.6f, 3.6f, -hw - 2, hw + 2]) Decor("stone-lion", new Vector3(x, 0, -4.4f), 1.4f, rig, yaw: x < 0 ? 0.4f : -0.4f);
                foreach (float x in (float[])[-7.5f, -1.2f, 5.5f, -hw - 6, hw + 6]) Decor("buddha", new Vector3(x, 0, -6.6f), 2.4f, rig, yaw: R(-0.2f, 0.2f));
                Decor("frangipani", new Vector3(-hw - 3, 0, -6.5f), 4f, rig, 0.05f);
                Decor("frangipani", new Vector3(hw + 3, 0, -6.8f), 4.4f, rig, 0.05f);
                Decor("candle-lantern", new Vector3(-2, 0, -4.2f), 0.6f, rig);
                Decor("candle-lantern", new Vector3(2, 0, -4.4f), 0.6f, rig);
                break;

            case "karnaval-sound-horeg":
                Decor("sound-truck", new Vector3(hw + 7, 0, -6.5f), 4.2f, rig, yaw: -0.25f);
                Scatter("litter", 30, 0.06f, 0.09f, rig, 0, -6f, 3.5f);
                Bunting(-3.6f, 4.6f, 24, rig);
                break;

            case "alun-alun-ponorogo":
                Scatter("bush", 12, 0.7f, 1.1f, rig, 0.06f, -7f, -4f);
                Decor("reog-mask", new Vector3(-hw - 2.5f, 0, -3.6f), 2.6f, rig, 0.02f, yaw: 0.5f);
                Decor("gerobak", new Vector3(hw + 3, 0, -2.8f), 1.5f, rig, yaw: -1.6f);
                for (int i = 0; i < 5; i++) Flag(new Vector3(-12 + i * 6, 0, -6.4f), 4.5f, 0xB22222, Palette.Spanduk, rig);
                break;

            case "malam-pengerupukan":
                for (int i = 0; i < 6; i++) Decor("penjor", new Vector3(-13 + i * 5.2f, 0, -4.9f), 6f, rig, 0.06f, yaw: 0);
                Decor("bale-kulkul", new Vector3(-hw - 4, 0, -5.5f), 5f, rig, yaw: 0.4f);
                Decor("canang", new Vector3(-hw - 1.2f, 0, -2.4f), 0.5f, rig);
                Decor("canang", new Vector3(hw + 1.4f, 0, -2.6f), 0.5f, rig);
                break;

            case "pangkalan-ojol":
                Decor("gerobak", new Vector3(-hw - 3, 0, -3), 1.5f, rig, yaw: 1.5f);
                Decor("plastic-chairs", new Vector3(hw + 4.5f, 0, -3.2f), 0.9f, rig);
                Scatter("litter", 24, 0.06f, 0.09f, rig, 0, -6f, 3f);
                Decor("banana", new Vector3(hw + 7, 0, -5), 3.4f, rig, 0.1f);
                break;

            case "ruang-sidang":
                for (int i = 0; i < 2; i++) Decor("gallery-bench", new Vector3(i == 0 ? -hw - 2.5f : hw + 2.5f, 0, -3.2f), 1.1f, rig, yaw: MathF.PI / 2);
                // The models are long along their local Z; a quarter turn lays them across the room.
                Decor("judge-bench", new Vector3(0, 0, -5.3f), 2.2f, rig, yaw: MathF.PI / 2);
                Decor("garuda", new Vector3(0, 3.0f, -6.25f), 1.5f, rig, yaw: 0);
                Flag(new Vector3(-6.5f, 0, -5.8f), 3.2f, 0xE0322B, 0xF3EEE1, rig);
                Decor("potted-palm", new Vector3(6.5f, 0, -5.6f), 2.2f, rig, 0.02f);
                Decor("potted-palm", new Vector3(-9f, 0, -5.6f), 2.2f, rig, 0.02f);
                break;
        }
    }
}
