using Hkmp.Networking.Packet;

namespace PropHunt.HKMP
{
    #region Server to Client
    /// <summary>
    /// Sent by the server to a client to assign them a team.
    /// </summary>
    internal class AssignTeamFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// Whether the assigned player is a hunter.
        /// </summary>
        public bool IsHunter;
        /// <summary>
        /// Whether the round is still in its grace period.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to end the current round.
    /// </summary>
    internal class EndRoundFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// Whether the hunters team won the round.
        /// </summary>
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

    /// <summary>
    /// Send by the server to a client to indicate that a player has died.
    /// </summary>
    internal class PlayerDeathFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The ID of the player who died.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The username of the player that died.
        /// </summary>
        public string Username;
        /// <summary>
        /// The number of hunters remaining after the player died.
        /// </summary>
        public ushort HuntersRemaining;
        /// <summary>
        /// The number of hunters total after the player died.
        /// </summary>
        public ushort HuntersTotal;
        /// <summary>
        /// The number of props remaining after the player died.
        /// </summary>
        public ushort PropsRemaining;
        /// <summary>
        /// The number of props total after the player died.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to indicate that a player has disconnected from the server.
    /// </summary>
    internal class PlayerLeftGameFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The ID of the player that left the game.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The username of the player that left the game.
        /// </summary>
        public string Username;
        /// <summary>
        /// The number of hunters remaining after the player left.
        /// </summary>
        public ushort HuntersRemaining;
        /// <summary>
        /// The number of hunters total after the player left.
        /// </summary>
        public ushort HuntersTotal;
        /// <summary>
        /// The number of props remaining after the player left.
        /// </summary>
        public ushort PropsRemaining;
        /// <summary>
        /// The number of props total after the player left.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update a remote prop's position along the x- and y-axes.
    /// </summary>
    internal class UpdatePropPositionXYFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The ID of the player whose prop's x and y position are being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The x component of the prop's position.
        /// </summary>
        public float X;
        /// <summary>
        /// The y component of the prop's position.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update a remote prop's position along the z-axis.
    /// </summary>
    internal class UpdatePropPositionZFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The ID of the player whose prop's z position is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The z component of the prop's position.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update a remote prop's rotation.
    /// </summary>
    internal class UpdatePropRotationFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The ID of the player whose prop's rotation is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The rotation of the prop.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update a remote prop's scale.
    /// </summary>
    internal class UpdatePropScaleFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The ID of the player whose prop's scale is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The scale of the prop.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update a remote prop's sprite.
    /// </summary>
    internal class UpdatePropSpriteFromServerToClientData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The ID of the player whose prop is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The name of the sprite to be broadcast.
        /// </summary>
        public string SpriteName;
        /// <summary>
        /// The length of the array of bytes to be sent.
        /// </summary>
        public int NumBytes;
        /// <summary>
        /// A byte array of the prop sprite's texture.
        /// </summary>
        public byte[] SpriteBytes;
        /// <summary>
        /// The x component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionX;
        /// <summary>
        /// The y component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionY;
        /// <summary>
        /// The z component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionZ;
        /// <summary>
        /// The prop's rotation at the time the packet is sent.
        /// </summary>
        public float RotationZ;
        /// <summary>
        /// The prop's scale at the time the packet is sent.
        /// </summary>
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

    /// <summary>
    /// Sent by the server to a client to update the amount of grace time remaining.
    /// </summary>
    internal class UpdateGraceTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The amount of time remaining in the grace period.
        /// </summary>
        public byte TimeRemaining;

        public void ReadData(IPacket packet)
        {
            TimeRemaining = packet.ReadByte();
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(TimeRemaining);
        }
    }

    /// <summary>
    /// Sent by the server to a client to update the amount of time remaining in the round.
    /// </summary>
    internal class UpdateRoundTimerFromServerToClientData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;
        
        /// <summary>
        /// The amount of time remaining in the round in seconds.
        /// </summary>
        public ushort TimeRemaining;

        public void ReadData(IPacket packet)
        {
            TimeRemaining = packet.ReadUShort();
        }

        public void WriteData(IPacket packet)
        {
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
    /// <summary>
    /// Broadcast to all players that the local player has died.
    /// </summary>
    internal class BroadcastPlayerDeathFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        public void ReadData(IPacket packet) { }

        public void WriteData(IPacket packet) { }
    }

    /// <summary>
    /// Broadcast to all players the local prop's position along the x- and y-axes.
    /// </summary>
    internal class BroadcastPropPositionXYFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The x component of the prop's position to be broadcast.
        /// </summary>
        public float X;
        /// <summary>
        /// The y component of the prop's position to be broadcast.
        /// </summary>
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

    /// <summary>
    /// Broadcast to all players the local prop's position along the z-axis.
    /// </summary>
    internal class BroadcastPropPositionZFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The z component of the prop's position to be broadcast.
        /// </summary>
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

    /// <summary>
    /// Broadcast to all players the local prop's rotation.
    /// </summary>
    internal class BroadcastPropRotationFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The prop's rotation to be broadcast.
        /// </summary>
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

    /// <summary>
    /// Broadcast to all players the local prop's scale.
    /// </summary>
    internal class BroadcastPropScaleFromClientToServerData : IPacketData
    {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The prop's scale to be broadcast.
        /// </summary>
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

    /// <summary>
    /// Broadcast to all players the local prop's sprite and its transform.
    /// </summary>
    internal class BroadcastPropSpriteFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The name of the sprite to be broadcasted.
        /// </summary>
        public string SpriteName;
        /// <summary>
        /// The length of the array of bytes to be sent.
        /// </summary>
        public int NumBytes;
        /// <summary>
        /// A byte array of the prop sprite's texture.
        /// </summary>
        public byte[] SpriteBytes;
        /// <summary>
        /// The x component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionX;
        /// <summary>
        /// The y component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionY;
        /// <summary>
        /// The z component of the prop's position at the time the packet is sent.
        /// </summary>
        public float PositionZ;
        /// <summary>
        /// The prop's rotation at the time the packet is sent.
        /// </summary>
        public float RotationZ;
        /// <summary>
        /// The prop's scale at the time the packet is sent.
        /// </summary>
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

    /// <summary>
    /// Sent from a client to the server to end a round.
    /// </summary>
    internal class EndRoundFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// Whether the hunters won at the end of the round.
        /// </summary>
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

    /// <summary>
    /// Sent from a client to the server to start a round.
    /// </summary>
    internal class StartRoundFromClientToServerData : IPacketData
    {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The duration of the initial grace period.
        /// </summary>
        public byte GraceTime;
        /// <summary>
        /// The duration of the round.
        /// </summary>
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
