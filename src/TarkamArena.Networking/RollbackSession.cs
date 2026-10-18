using TarkamArena.Core.Combat;
using TarkamArena.Core.Match;

namespace TarkamArena.Networking;

/// <summary>Packet types exchanged between the two peers of an online match.</summary>
public enum PacketType : byte
{
    Inputs = 1,
    Checksum = 2,
    Handshake = 3,
    Quit = 4,
}

/// <summary>Moves bytes between peers. Implemented over LiteNetLib UDP and an in-memory loopback for tests.</summary>
public interface IPacketTransport
{
    void Send(ReadOnlySpan<byte> packet, bool reliable);

    /// <summary>Delivers every packet that arrived since the last call.</summary>
    void Poll(PacketHandler handler);

    bool IsConnected { get; }
}

public delegate void PacketHandler(ReadOnlySpan<byte> packet);

public readonly record struct RollbackStats(int Frame, int ConfirmedFrame, int Rollbacks, int MaxRollbackDepth, int Stalls, bool Desynced);

/// <summary>
/// GGPO-style rollback for a two-player match. The local player's input is applied with a small
/// delay; the remote player's input is predicted (repeat the last known input) and, when the real
/// input arrives and differs, the simulation rewinds to that frame and replays. Snapshots are
/// plain struct copies of <see cref="MatchState"/>, so a rollback costs no allocations.
/// </summary>
public sealed class RollbackSession
{
    public const int MaxRollback = 8;
    public const int InputDelay = 2;
    private const int Ring = 64;
    private const int RedundantInputs = 8;

    private readonly MatchContext _context;
    private readonly IPacketTransport _transport;
    private readonly int _localSlot;
    private readonly int _remoteSlot;

    private readonly MatchState[] _snapshots = new MatchState[Ring];
    private readonly ushort[] _localInputs = new ushort[Ring];
    private readonly ushort[] _remoteInputs = new ushort[Ring];
    private readonly bool[] _remoteConfirmed = new bool[Ring];
    private readonly ushort[] _remoteUsed = new ushort[Ring];
    private readonly uint[] _localChecksums = new uint[Ring];
    private readonly InputFrame[] _frameInputs = new InputFrame[2];
    private readonly byte[] _sendBuffer = new byte[1 + 4 + 1 + RedundantInputs * 2 + 4];

    private MatchState _state;
    private int _frame;
    private int _lastRemoteConfirmed = -1;
    private int _remoteAck = -1;
    private int _rollbacks;
    private int _maxDepth;
    private int _stalls;
    private bool _desynced;
    private int _earliestMismatch = int.MaxValue;
    private readonly PacketHandler _onPacket;

    public RollbackSession(MatchContext context, IPacketTransport transport, int localSlot)
    {
        if (context.Characters.Length != 2) throw new ArgumentException("Rollback sessions support two-player matches.", nameof(context));
        _context = context;
        _transport = transport;
        _localSlot = localSlot;
        _remoteSlot = 1 - localSlot;
        _state = MatchSimulation.CreateInitialState(context);
        _snapshots[0] = _state;
        _onPacket = OnPacket;

        // The first InputDelay frames have neutral input on both sides by definition.
        for (int f = 0; f < InputDelay; f++)
        {
            _remoteConfirmed[f] = true;
            _lastRemoteConfirmed = f;
        }
    }

    public ref readonly MatchState State => ref _state;
    public int Frame => _frame;
    public int LocalSlot => _localSlot;
    /// <summary>Checksum of the state after <paramref name="frame"/>, once both inputs for it are confirmed (0 if unknown).</summary>
    public uint ConfirmedChecksum(int frame) =>
        frame <= _lastRemoteConfirmed && _frame - frame < Ring / 2 ? _localChecksums[frame % Ring] : 0;

    public RollbackStats Stats => new(_frame, _lastRemoteConfirmed, _rollbacks, _maxDepth, _stalls, _desynced);

    /// <summary>
    /// Advances one 60 Hz tick. Returns false when the session is waiting for the remote peer
    /// (too far ahead of the last confirmed input), in which case the caller should simply try
    /// again next tick.
    /// </summary>
    public bool Tick(InputFrame localInput)
    {
        _transport.Poll(_onPacket);
        ApplyPendingRollback();

        if (_frame - _lastRemoteConfirmed >= MaxRollback)
        {
            _stalls++;
            SendInputs();
            return false;
        }

        int inputFrame = _frame + InputDelay;
        _localInputs[inputFrame % Ring] = localInput.Pack();
        SendInputs();

        SimulateFrame(_frame);
        _frame++;
        _snapshots[_frame % Ring] = _state;
        return true;
    }

    private void SimulateFrame(int frame)
    {
        int idx = frame % Ring;
        ushort remote = _remoteConfirmed[idx] ? _remoteInputs[idx] : PredictRemote(frame);
        _remoteUsed[idx] = remote;
        _frameInputs[_localSlot] = InputFrame.Unpack(_localInputs[idx]);
        _frameInputs[_remoteSlot] = InputFrame.Unpack(remote);
        MatchSimulation.Step(ref _state, _context, _frameInputs);

        // Record checksums of frames whose inputs are fully confirmed.
        if (_remoteConfirmed[idx]) _localChecksums[idx] = _state.Checksum();
    }

    private ushort PredictRemote(int frame) =>
        _lastRemoteConfirmed >= 0 ? _remoteInputs[_lastRemoteConfirmed % Ring] : (ushort)0;

    private void ApplyPendingRollback()
    {
        if (_earliestMismatch == int.MaxValue) return;
        int target = _earliestMismatch;
        _earliestMismatch = int.MaxValue;
        if (target >= _frame) return;

        int depth = _frame - target;
        _rollbacks++;
        _maxDepth = Math.Max(_maxDepth, depth);
        _state = _snapshots[target % Ring];
        for (int f = target; f < _frame; f++)
        {
            SimulateFrame(f);
            _snapshots[(f + 1) % Ring] = _state;
        }
    }

    private void SendInputs()
    {
        // Send the last few local inputs every tick so a lost packet is covered by the next one.
        int newest = _frame + InputDelay;
        int oldest = Math.Max(0, Math.Max(_remoteAck + 1, newest - RedundantInputs + 1));
        int count = newest - oldest + 1;
        var span = _sendBuffer.AsSpan();
        span[0] = (byte)PacketType.Inputs;
        BitConverter.TryWriteBytes(span[1..], oldest);
        span[5] = (byte)count;
        for (int i = 0; i < count; i++)
            BitConverter.TryWriteBytes(span[(6 + i * 2)..], _localInputs[(oldest + i) % Ring]);
        BitConverter.TryWriteBytes(span[(6 + count * 2)..], _lastRemoteConfirmed);
        _transport.Send(span[..(10 + count * 2)], reliable: false);

        if (_lastRemoteConfirmed > 0 && _lastRemoteConfirmed % 60 == 0 && _lastRemoteConfirmed < _frame)
        {
            Span<byte> check = stackalloc byte[9];
            check[0] = (byte)PacketType.Checksum;
            BitConverter.TryWriteBytes(check[1..], _lastRemoteConfirmed);
            BitConverter.TryWriteBytes(check[5..], _localChecksums[_lastRemoteConfirmed % Ring]);
            _transport.Send(check, reliable: false);
        }
    }

    private void OnPacket(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 1) return;
        switch ((PacketType)packet[0])
        {
            case PacketType.Inputs when packet.Length >= 6:
            {
                int start = BitConverter.ToInt32(packet[1..]);
                int count = packet[5];
                if (packet.Length < 10 + count * 2) return;
                for (int i = 0; i < count; i++)
                {
                    int f = start + i;
                    if (f <= _lastRemoteConfirmed || f > _frame + Ring / 2) continue;
                    if (f != _lastRemoteConfirmed + 1) break; // keep confirmed inputs contiguous
                    ushort value = BitConverter.ToUInt16(packet[(6 + i * 2)..]);
                    int idx = f % Ring;
                    _remoteInputs[idx] = value;
                    _remoteConfirmed[idx] = true;
                    _lastRemoteConfirmed = f;
                    // A frame that was already simulated with a wrong guess must be replayed.
                    if (f < _frame && _remoteUsed[idx] != value) _earliestMismatch = Math.Min(_earliestMismatch, f);
                    else if (f < _frame && _earliestMismatch > f) _localChecksums[idx] = _snapshots[(f + 1) % Ring].Checksum();
                }
                _remoteAck = Math.Max(_remoteAck, BitConverter.ToInt32(packet[(6 + count * 2)..]));

                // Clear ring slots far enough ahead for reuse.
                int clear = (_lastRemoteConfirmed + Ring / 2) % Ring;
                _remoteConfirmed[clear] = false;
                break;
            }
            case PacketType.Checksum when packet.Length >= 9:
            {
                int f = BitConverter.ToInt32(packet[1..]);
                uint theirs = BitConverter.ToUInt32(packet[5..]);
                if (f <= _lastRemoteConfirmed && _frame - f < Ring / 2 && _localChecksums[f % Ring] != 0 && _localChecksums[f % Ring] != theirs)
                    _desynced = true;
                break;
            }
        }
    }
}
