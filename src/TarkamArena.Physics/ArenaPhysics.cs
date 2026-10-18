using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace TarkamArena.Physics;

public enum PropShape : byte
{
    Box,
    Sphere,
    Cylinder,
}

/// <summary>A destructible prop or debris piece, readable by the renderer without allocations.</summary>
public struct PropBody
{
    public BodyHandle Handle;
    public PropShape Shape;
    public Vector3 Size;
    public uint Color;
    public Vector3 Position;
    public Quaternion Orientation;
    public float Life;
    public bool Alive;
    public bool Debris;
}

/// <summary>
/// Cosmetic rigid-body world for arena props: market crates, fruit, roof tiles shaken loose by the
/// sound horeg, rubble from finishers. It runs on BepuPhysics and never influences the
/// deterministic fight, so it can use floats and multithreading freely.
/// </summary>
public sealed class ArenaPhysics : IDisposable
{
    public const int MaxProps = 256;

    private readonly BufferPool _pool = new();
    private readonly Simulation _simulation;
    private readonly ThreadDispatcher? _dispatcher;
    private readonly PropBody[] _props = new PropBody[MaxProps];
    private int _count;
    private float _accumulator;

    public const float FixedStep = 1f / 60f;

    public ArenaPhysics(float halfWidth, bool multithreaded = true)
    {
        _simulation = Simulation.Create(
            _pool,
            new NarrowPhaseCallbacks { Friction = 0.8f },
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(8, 1));

        if (multithreaded && Environment.ProcessorCount > 2)
            _dispatcher = new ThreadDispatcher(Math.Max(1, Environment.ProcessorCount / 2));

        // Ground and two invisible walls just outside the play area, plus a back wall for the crowd line.
        AddStatic(new Box(200, 1, 40), new Vector3(0, -0.5f, 0));
        AddStatic(new Box(1, 20, 40), new Vector3(-(halfWidth + 6f), 10, 0));
        AddStatic(new Box(1, 20, 40), new Vector3(halfWidth + 6f, 10, 0));
        AddStatic(new Box(200, 20, 1), new Vector3(0, 10, -10f));
    }

    public ReadOnlySpan<PropBody> Props => _props.AsSpan(0, _count);

    public int AliveCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _count; i++) if (_props[i].Alive) n++;
            return n;
        }
    }

    private void AddStatic<TShape>(TShape shape, Vector3 position) where TShape : unmanaged, IShape
    {
        var index = _simulation.Shapes.Add(shape);
        _simulation.Statics.Add(new StaticDescription(position, index));
    }

    /// <summary>Adds a prop. Returns its index, or -1 when the pool is full of live props.</summary>
    public int AddProp(PropShape shape, Vector3 size, Vector3 position, float mass, uint color, bool debris = false, float life = 0)
    {
        int slot = FindSlot();
        if (slot < 0) return -1;

        BodyInertia inertia;
        TypedIndex shapeIndex;
        switch (shape)
        {
            case PropShape.Sphere:
                var sphere = new Sphere(size.X * 0.5f);
                inertia = sphere.ComputeInertia(mass);
                shapeIndex = _simulation.Shapes.Add(sphere);
                break;
            case PropShape.Cylinder:
                var cylinder = new Cylinder(size.X * 0.5f, size.Y);
                inertia = cylinder.ComputeInertia(mass);
                shapeIndex = _simulation.Shapes.Add(cylinder);
                break;
            default:
                var box = new Box(size.X, size.Y, size.Z);
                inertia = box.ComputeInertia(mass);
                shapeIndex = _simulation.Shapes.Add(box);
                break;
        }

        var handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
            new RigidPose(position), inertia, new CollidableDescription(shapeIndex, 0.05f), new BodyActivityDescription(0.02f)));

        _props[slot] = new PropBody
        {
            Handle = handle,
            Shape = shape,
            Size = size,
            Color = color,
            Position = position,
            Orientation = Quaternion.Identity,
            Life = life,
            Alive = true,
            Debris = debris,
        };
        if (slot == _count) _count++;
        return slot;
    }

    private int FindSlot()
    {
        for (int i = 0; i < _count; i++)
            if (!_props[i].Alive) return i;
        if (_count < MaxProps) return _count;

        // Recycle the oldest debris piece.
        for (int i = 0; i < _count; i++)
        {
            if (!_props[i].Debris) continue;
            Remove(i);
            return i;
        }
        return -1;
    }

    private void Remove(int index)
    {
        ref var p = ref _props[index];
        if (!p.Alive) return;
        var body = _simulation.Bodies[p.Handle];
        var shape = body.Collidable.Shape;
        _simulation.Bodies.Remove(p.Handle);
        _simulation.Shapes.RecursivelyRemoveAndDispose(shape, _pool);
        p.Alive = false;
    }

    /// <summary>Pushes every prop within <paramref name="radius"/> of <paramref name="center"/> outwards.</summary>
    public void Explode(Vector3 center, float radius, float impulse)
    {
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _props[i];
            if (!p.Alive) continue;
            var body = _simulation.Bodies[p.Handle];
            var offset = body.Pose.Position - center;
            float d = offset.Length();
            if (d > radius) continue;
            var dir = d > 1e-3f ? offset / d : Vector3.UnitY;
            dir.Y = MathF.Abs(dir.Y) + 0.6f;
            float strength = impulse * (1f - d / radius);
            body.Awake = true;
            body.ApplyLinearImpulse(Vector3.Normalize(dir) * strength);
            body.ApplyAngularImpulse(new Vector3(dir.Z, 0.3f, -dir.X) * strength * 0.1f);
        }
    }

    /// <summary>Spawns short-lived debris chunks, e.g. when a finisher or hazard smashes something.</summary>
    public void SpawnDebris(Vector3 center, int count, uint color, float size, Random random)
    {
        for (int i = 0; i < count; i++)
        {
            var offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() * 0.5f, (float)random.NextDouble() - 0.5f) * 0.6f;
            float s = size * (0.5f + (float)random.NextDouble());
            int slot = AddProp(PropShape.Box, new Vector3(s, s, s), center + offset, 0.2f, color, debris: true, life: 3f + (float)random.NextDouble() * 2f);
            if (slot < 0) return;
            var body = _simulation.Bodies[_props[slot].Handle];
            body.Velocity.Linear = new Vector3(offset.X * 14f, 4f + (float)random.NextDouble() * 4f, offset.Z * 8f);
            body.Velocity.Angular = new Vector3((float)random.NextDouble() * 10f, (float)random.NextDouble() * 10f, 0);
        }
    }

    /// <summary>Advances physics on a fixed 60 Hz step and copies poses into <see cref="Props"/>.</summary>
    public void Update(float deltaSeconds)
    {
        _accumulator = MathF.Min(_accumulator + deltaSeconds, FixedStep * 4);
        while (_accumulator >= FixedStep)
        {
            _simulation.Timestep(FixedStep, _dispatcher);
            _accumulator -= FixedStep;
        }

        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _props[i];
            if (!p.Alive) continue;
            if (p.Debris)
            {
                p.Life -= deltaSeconds;
                if (p.Life <= 0)
                {
                    Remove(i);
                    continue;
                }
            }
            var pose = _simulation.Bodies[p.Handle].Pose;
            p.Position = pose.Position;
            p.Orientation = pose.Orientation;
        }
    }

    public void Dispose()
    {
        _simulation.Dispose();
        _dispatcher?.Dispose();
        _pool.Clear();
    }

    private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public float Friction;

        public void Initialize(Simulation simulation)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) =>
            a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = Friction;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

        public void Dispose()
        {
        }
    }

    private struct PoseIntegratorCallbacks(Vector3 gravity) : IPoseIntegratorCallbacks
    {
        private Vector3Wide _gravityDt;

        public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public readonly bool AllowSubstepsForUnconstrainedBodies => false;
        public readonly bool IntegrateVelocityForKinematics => false;

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt) => _gravityDt = Vector3Wide.Broadcast(gravity * dt);

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia,
            Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear += _gravityDt;
            // A little damping keeps debris from sliding forever.
            velocity.Linear *= new Vector<float>(0.995f);
            velocity.Angular *= new Vector<float>(0.98f);
        }
    }
}
