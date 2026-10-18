using System.Text;
using System.Text.Json;

namespace TarkamArena.Networking;

/// <summary>What each peer tells the other before a direct (LAN) match starts.</summary>
public sealed record HandshakeInfo(string Name, string CharacterId, int Rating, string? StageId = null, uint Seed = 0);

/// <summary>
/// Exchanges <see cref="HandshakeInfo"/> over an <see cref="IPacketTransport"/>. The host decides
/// the stage and seed; the guest adopts them. Poll it every tick until <see cref="Remote"/> is set.
/// </summary>
public sealed class HandshakeExchange
{
    private readonly IPacketTransport _transport;
    private readonly HandshakeInfo _local;
    private int _resendTimer;

    public HandshakeInfo? Remote { get; private set; }
    public bool IsHost { get; }

    public HandshakeExchange(IPacketTransport transport, HandshakeInfo local, bool isHost)
    {
        _transport = transport;
        _local = local;
        IsHost = isHost;
    }

    public bool Poll()
    {
        _transport.Poll(packet =>
        {
            if (packet.Length < 2 || packet[0] != (byte)PacketType.Handshake) return;
            Remote = JsonSerializer.Deserialize<HandshakeInfo>(Encoding.UTF8.GetString(packet[1..]));
        });
        if (_transport.IsConnected && --_resendTimer <= 0)
        {
            _resendTimer = 30;
            var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_local));
            var packet = new byte[json.Length + 1];
            packet[0] = (byte)PacketType.Handshake;
            json.CopyTo(packet, 1);
            _transport.Send(packet, reliable: true);
        }
        return Remote is not null;
    }
}
