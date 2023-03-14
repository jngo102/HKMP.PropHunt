using Hkmp.Networking.Packet;
using Vector2 = Hkmp.Math.Vector2;

namespace PropHunt.HKMP
{
    #region Server to Client
    internal class PropSpriteFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId;
        public string SpriteName;
        public int NumBytes;
        public byte[] SpriteBytes;
        public Vector2 PositionXY;
        public float PositionZ;
        public float RotationZ;
        public float Scale;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            SpriteName = packet.ReadString();
            NumBytes = packet.ReadInt();
            if (NumBytes > 0)
            {
                SpriteBytes = new byte[NumBytes];
                for (int i = 0; i < SpriteBytes.Length; i++)
                {
                    SpriteBytes[i] = packet.ReadByte();
                }
            }
            else
            {
                SpriteBytes = null;
            }
            PositionXY = packet.ReadVector2();
            PositionZ = packet.ReadFloat();
            RotationZ = packet.ReadFloat();
            Scale = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(SpriteName);
            packet.Write(NumBytes);
            if (NumBytes > 0)
            {
                foreach (var b in SpriteBytes)
                {
                    packet.Write(b);
                }
            }
            packet.Write(PositionXY);
            packet.Write(PositionZ);
            packet.Write(RotationZ);
            packet.Write(Scale);
        }
    }
    public enum FromServerToClientPackets
    {
        UpdatePropSprite,
    }
    #endregion

    #region Client to Server
    internal class PropSpriteFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public string SpriteName;
        public int NumBytes;
        public byte[] SpriteBytes;
        public Vector2 PositionXY;
        public float PositionZ;
        public float RotationZ;
        public float Scale;

        public void ReadData(IPacket packet)
        {
            SpriteName = packet.ReadString();
            NumBytes = packet.ReadInt();
            if (NumBytes > 0)
            {
                SpriteBytes = new byte[NumBytes];
                for (int i = 0; i < SpriteBytes.Length; i++)
                {
                    SpriteBytes[i] = packet.ReadByte();
                }
            }
            else
            {
                SpriteBytes = null;
            }
            PositionXY = packet.ReadVector2();
            PositionZ = packet.ReadFloat();
            RotationZ = packet.ReadFloat();
            Scale = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(SpriteName);
            packet.Write(NumBytes);
            if (NumBytes > 0)
            {
                foreach (var b in SpriteBytes)
                {
                    packet.Write(b);
                }
            }
            packet.Write(PositionXY);
            packet.Write(PositionZ);
            packet.Write(RotationZ);
            packet.Write(Scale);
        }
    }

    public enum FromClientToServerPackets
    {
        BroadcastPropSprite,
    }
    #endregion
}
