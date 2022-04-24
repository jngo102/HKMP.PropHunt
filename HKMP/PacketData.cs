using Hkmp.Networking.Packet;
using Hkmp.Math;
using System.Collections.Generic;

namespace PropHunt.HKMP
{
    #region Server to Client
    internal class PropSpriteFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId { get; set; }

        public string SpriteName { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();

            SpriteName = packet.ReadString();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);

            packet.Write(SpriteName);
        }
    }

    internal class PropPositionXYFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }

        public Vector2 PositionXY { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();

            PositionXY = packet.ReadVector2();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);

            packet.Write(PositionXY);
        }
    }

    internal class PropPositionZFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }

        public float PositionZ { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();

            PositionZ = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);

            packet.Write(PositionZ);
        }
    }

    internal class PropRotationFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }

        public float Rotation { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();

            Rotation = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);

            packet.Write(Rotation);
        }
    }

    public enum FromServerToClientPackets
    {
        SendPropSprite,
        SendPropPositionXY,
        SendPropPositionZ,
        SendPropRotation,
    }

    #endregion

    #region Client to Server
    internal class PropSpriteFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public string SpriteName { get; set; }

        public void ReadData(IPacket packet)
        {
            SpriteName = packet.ReadString();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(SpriteName);
        }
    }

    internal class PropPositionXYFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public Vector2 PositionXY { get; set; }

        public void ReadData(IPacket packet)
        {
            PositionXY = packet.ReadVector2();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PositionXY);
        }
    }

    internal class PropPositionZFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float PositionZ { get; set; }

        public void ReadData(IPacket packet)
        {
            PositionZ = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PositionZ);
        }
    }

    internal class PropRotationFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;
        
        public float Rotation { get; set; }

        public void ReadData(IPacket packet)
        {
            Rotation = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Rotation);
        }
    }

    public enum FromClientToServerPackets
    {
        BroadcastPropSprite,
        BroadcastPropPositionXY,
        BroadcastPropPositionZ,
        BroadcastPropRotation,
    }

    #endregion
}