using Hkmp.Networking.Packet;

namespace PropHunt.HKMP
{
    #region Server to Client
    internal class AssignTeamFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public bool IsHunter;
        public bool InGrace;

        public void ReadData(IPacket packet)
        {
            IsHunter = packet.ReadBool();
            InGrace = packet.ReadBool();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(IsHunter);
            packet.Write(InGrace);
        }
    }

    internal class EndRoundFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public bool HuntersWin;

        public void ReadData(IPacket packet)
        {
            HuntersWin = packet.ReadBool();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(HuntersWin);
        }
    }

    internal class PlayerDeathFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId;
        public ushort HuntersRemaining;
        public ushort HuntersTotal;
        public ushort PropsRemaining;
        public ushort PropsTotal;

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

    internal class PlayerLeftGameFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId;
        public string Username;
        public ushort HuntersRemaining;
        public ushort HuntersTotal;
        public ushort PropsRemaining;
        public ushort PropsTotal;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Username = packet.ReadString();
            HuntersRemaining = packet.ReadUShort();
            HuntersTotal = packet.ReadUShort();
            PropsRemaining = packet.ReadUShort();
            PropsTotal = packet.ReadUShort();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(Username);
            packet.Write(HuntersRemaining);
            packet.Write(HuntersTotal);
            packet.Write(PropsRemaining);
            packet.Write(PropsTotal);
        }
    }

    internal class UpdatePropPositionXYFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public float X;
        public float Y;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            X = packet.ReadFloat();
            Y = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(X);
            packet.Write(Y);
        }
    }

    internal class UpdatePropPositionZFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public float Z;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Z = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(Z);
        }
    }

    internal class UpdatePropRotationFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public float Rotation;

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

    internal class UpdatePropScaleFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public float Scale;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Scale = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(Scale);
        }
    }

    internal class UpdatePropSpriteFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public ushort PlayerId;
        public string SpriteName;
        public int NumBytes;
        public byte[] SpriteBytes;
        public float PositionX;
        public float PositionY;
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

            PositionX = packet.ReadFloat();
            PositionY = packet.ReadFloat();
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
            packet.Write(PositionX);
            packet.Write(PositionY);
            packet.Write(PositionZ);
            packet.Write(RotationZ);
            packet.Write(Scale);
        }
    }

    internal class UpdateGraceTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public byte TimeRemaining;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            TimeRemaining = packet.ReadByte();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(TimeRemaining);
        }
    }

    internal class UpdateRoundTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public ushort PlayerId;
        public ushort TimeRemaining;

        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            TimeRemaining = packet.ReadUShort();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(TimeRemaining);
        }
    }

    public enum FromServerToClientPackets
    {
        AssignTeam,
        EndRound,
        PlayerDeath,
        PlayerLeftGame,
        UpdateGraceTimer,
        UpdateRoundTimer,
        UpdatePropPositionXY,
        UpdatePropPositionZ,
        UpdatePropRotation,
        UpdatePropScale,
        UpdatePropSprite,
    }
    #endregion

    #region Client to Server
    internal class BroadcastPlayerDeathFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public void ReadData(IPacket packet) { }

        public void WriteData(IPacket packet) { }
    }

    internal class BroadcastPropPositionXYFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float X;
        public float Y;

        public void ReadData(IPacket packet)
        {
            X = packet.ReadFloat();
            Y = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(X);
            packet.Write(Y);
        }
    }

    internal class BroadcastPropPositionZFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float Z;

        public void ReadData(IPacket packet)
        {
            Z = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Z);
        }
    }

    internal class BroadcastPropRotationFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float Rotation;

        public void ReadData(IPacket packet)
        {
            Rotation = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Rotation);
        }
    }

    internal class BroadcastPropScaleFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        public float Scale;

        public void ReadData(IPacket packet)
        {
            Scale = packet.ReadFloat();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Scale);
        }
    }

    internal class BroadcastPropSpriteFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public string SpriteName;
        public int NumBytes;
        public byte[] SpriteBytes;
        public float PositionX;
        public float PositionY;
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
            PositionX = packet.ReadFloat();
            PositionY = packet.ReadFloat();
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
            packet.Write(PositionX);
            packet.Write(PositionY);
            packet.Write(PositionZ);
            packet.Write(RotationZ);
            packet.Write(Scale);
        }
    }

    internal class EndRoundFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public bool HuntersWin;

        public void ReadData(IPacket packet)
        {
            HuntersWin = packet.ReadBool();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(HuntersWin);
        }
    }

    internal class StartRoundFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public byte GraceTime;
        public ushort RoundTime;

        public void ReadData(IPacket packet)
        {
            GraceTime = packet.ReadByte();
            RoundTime = packet.ReadUShort();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(GraceTime);
            packet.Write(RoundTime);
        }
    }

    public enum FromClientToServerPackets
    {
        BroadcastPlayerDeath,
        BroadcastPropPositionXY,
        BroadcastPropPositionZ,
        BroadcastPropRotation,
        BroadcastPropScale,
        BroadcastPropSprite,
        EndRound,
        StartRound,
    }
    #endregion
}
