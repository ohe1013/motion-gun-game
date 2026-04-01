using System;

namespace MotionGun.Runtime
{
    public interface IGesturePacketSource
    {
        GesturePacket LatestPacket { get; }
        bool HasPacket { get; }
        float LastPacketReceivedRealtime { get; }
        event Action<GesturePacket> PacketUpdated;
        bool HasFreshPacket(float maxAgeSeconds);
    }
}
