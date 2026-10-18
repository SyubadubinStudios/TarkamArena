using System.Collections.Concurrent;
using LiteNetLib;

namespace TarkamArena.Networking;

/// <summary>In-memory transport pair for tests and local replays, with optional simulated latency and loss.</summary>
public sealed class LoopbackTransport : IPacketTransport
{
    private readonly Queue<(int DeliverAt, byte[] Data)> _inbox = new();
    private readonly Random _random;
    private LoopbackTransport? _peer;
    private int _tick;

    public int LatencyTicks { get; set; }
    public int LossPercent { get; set; }

    private LoopbackTransport(int seed) => _random = new Random(seed);

    public static (LoopbackTransport A, LoopbackTransport B) CreatePair(int latencyTicks = 0, int lossPercent = 0)
    {
        var a = new LoopbackTransport(1) { LatencyTicks = latencyTicks, LossPercent = lossPercent };
        var b = new LoopbackTransport(2) { LatencyTicks = latencyTicks, LossPercent = lossPercent };
        a._peer = b;
        b._peer = a;
        return (a, b);
    }

    public bool IsConnected => _peer is not null;

    public void Send(ReadOnlySpan<byte> packet, bool reliable)
    {
        if (!reliable && LossPercent > 0 && _random.Next(100) < LossPercent) return;
        _peer!._inbox.Enqueue((_peer._tick + LatencyTicks, packet.ToArray()));
    }

    public void Poll(PacketHandler handler)
    {
        _tick++;
        int n = _inbox.Count;
        for (int i = 0; i < n; i++)
        {
            var (at, data) = _inbox.Dequeue();
            if (at <= _tick) handler(data);
            else _inbox.Enqueue((at, data));
        }
    }
}

/// <summary>
/// UDP transport on LiteNetLib. One side hosts (<see cref="Host"/>), the other connects
/// (<see cref="Join"/>). Inputs go unreliable-sequenced-free (plain unreliable, the session handles
/// redundancy); the handshake uses reliable ordered delivery.
/// </summary>
public sealed class LiteNetTransport : IPacketTransport, IDisposable
{
    public const string ConnectionKey = "tarkam-arena-v1";
    public const int DefaultPort = 7777;

    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _manager;
    private readonly ConcurrentQueue<byte[]> _received = new();
    private NetPeer? _peer;

    public event Action? Connected;
    public event Action<string>? Disconnected;

    public int LatencyMs { get; private set; }

    private LiteNetTransport()
    {
        _manager = new NetManager(_listener) { AutoRecycle = true, DisconnectTimeout = 8000, UpdateTime = 5 };
        _listener.ConnectionRequestEvent += request =>
        {
            if (_peer is null) request.AcceptIfKey(ConnectionKey);
            else request.Reject();
        };
        _listener.PeerConnectedEvent += peer =>
        {
            _peer = peer;
            Connected?.Invoke();
        };
        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (peer == _peer) _peer = null;
            Disconnected?.Invoke(info.Reason.ToString());
        };
        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            var data = new byte[reader.AvailableBytes];
            reader.GetBytes(data, data.Length);
            _received.Enqueue(data);
        };
        _listener.NetworkLatencyUpdateEvent += (peer, latency) => LatencyMs = latency;
    }

    public static LiteNetTransport Host(int port = DefaultPort)
    {
        var t = new LiteNetTransport();
        if (!t._manager.Start(port)) throw new InvalidOperationException($"Could not listen on UDP port {port}.");
        return t;
    }

    public static LiteNetTransport Join(string address, int port = DefaultPort)
    {
        var t = new LiteNetTransport();
        t._manager.Start();
        t._manager.Connect(address, port, ConnectionKey);
        return t;
    }

    public bool IsConnected => _peer is not null;

    public int LocalPort => _manager.LocalPort;

    public void Send(ReadOnlySpan<byte> packet, bool reliable) =>
        _peer?.Send(packet, 0, reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);

    public void Poll(PacketHandler handler)
    {
        _manager.PollEvents();
        while (_received.TryDequeue(out var data)) handler(data);
    }

    public void Dispose()
    {
        _peer?.Send([(byte)PacketType.Quit], 0, DeliveryMethod.ReliableOrdered);
        _manager.Stop();
    }
}
