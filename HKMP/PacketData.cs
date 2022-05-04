using Hkmp.Math;
using Hkmp.Networking.Packet;

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

    internal class PropScaleFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }
        public float ScaleFactor { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            ScaleFactor = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(ScaleFactor);
        }
    }

    internal class SetPlayingPropHuntFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;
        
        public ushort PlayerId { get; set; }
        public bool Playing { get; set; }
        public byte PropHuntTeam { get; set; }
        public float GracePeriod { get; set; }
        public float RoundTime { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Playing = packet.ReadBool();
            if (Playing)
            {
                PropHuntTeam = packet.ReadByte();
                GracePeriod = packet.ReadFloat();
                RoundTime = packet.ReadFloat();
            }
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(Playing);
            if (Playing)
            {
                packet.Write(PropHuntTeam);
                packet.Write(GracePeriod);
                packet.Write(RoundTime);
            }
        }
    }

    internal class UpdateRoundTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }
        public int TimeRemaining { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            TimeRemaining = packet.ReadInt();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(TimeRemaining);
        }
    }

    internal class UpdateGraceTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId { get; set; }
        public int TimeRemaining { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            TimeRemaining = packet.ReadInt();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(TimeRemaining);
        }
    }

    internal class EndRoundFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId { get; set; }
        public bool HuntersWin { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            HuntersWin = packet.ReadBool();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(HuntersWin);
        }
    }

    internal class PlayerDeathFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId { get; set; }
        public ushort HuntersRemaining { get; set; }
        public ushort HuntersTotal { get; set; }
        public ushort PropsRemaining { get; set; }
        public ushort PropsTotal { get; set; }

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            HuntersRemaining = packet.ReadUShort();
            HuntersTotal = packet.ReadUShort();
            PropsRemaining = packet.ReadUShort();
            PropsTotal = packet.ReadUShort();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(HuntersRemaining);
            packet.Write(HuntersTotal);
            packet.Write(PropsRemaining);
            packet.Write(PropsTotal);
        }
    }

    public enum FromServerToClientPackets
    {
        SendPropSprite,
        SendPropPositionXY,
        SendPropPositionZ,
        SendPropRotation,
        SendPropScale,
        SetPlayingPropHunt,
        UpdateRoundTimer,
        UpdateGraceTimer,
        EndRound,
        PlayerDeath,
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

    internal class PropScaleFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float ScaleFactor { get; set; }

        public void ReadData(IPacket packet)
        {
            ScaleFactor = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(ScaleFactor);
        }
    }

    internal class SetPlayingPropHuntFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public bool Playing { get; set; }
        public float GracePeriod { get; set; }
        public float RoundTime { get; set; }

        public void ReadData(IPacket packet)
        {
            Playing = packet.ReadBool();
            if (Playing)
            {
                GracePeriod = packet.ReadFloat();
                RoundTime = packet.ReadFloat();
            }
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Playing);
            if (Playing)
            {
                packet.Write(GracePeriod);
                packet.Write(RoundTime);
            }
        }
    }
    
    public enum FromClientToServerPackets
    {
        BroadcastPropSprite,
        BroadcastPropPositionXY,
        BroadcastPropPositionZ,
        BroadcastPropRotation,
        BroadcastPropScale,
        SetPlayingPropHunt,
        PlayerDeath,
    }

    #endregion
}