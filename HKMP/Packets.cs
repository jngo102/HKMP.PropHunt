using Hkmp.Networking.Packet;

namespace PropHunt.HKMP
{
    #region Client to Server
    /// <summary>
    /// Broadcast to all players that the local player died and was a prop.
    /// </summary>
    internal class BroadcastPropDeathFromClientToServerData : IPacketData
    {
        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet) { }

        /// <inheritdoc />
        public void WriteData(IPacket packet) { }
    }

    /// <summary>
    /// Broadcast to all players that the local player died and was a hunter.
    /// </summary>
    internal class BroadcastHunterDeathFromClientToServerData : IPacketData
    {
        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet) { }

        /// <inheritdoc />
        public void WriteData(IPacket packet) { }
    }

    /// <summary>
    /// Broadcast to all players the local prop's position along the x- and y-axes.
    /// </summary>
    internal class BroadcastPropPositionXyFromClientToServerData : IPacketData
    {
        /// <summary>
        /// The x component of the prop's position to be broadcast.
        /// </summary>
        public float X;
        /// <summary>
        /// The y component of the prop's position to be broadcast.
        /// </summary>
        public float Y;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

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
        /// <summary>
        /// The z component of the prop's position to be broadcast.
        /// </summary>
        public float Z;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            Z = packet.ReadFloat();
        }

        /// <inheritdoc />
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
        /// <summary>
        /// The prop's rotation to be broadcast.
        /// </summary>
        public float Rotation;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

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
        /// <summary>
        /// The prop's scale to be broadcast.
        /// </summary>
        public float Scale;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            Scale = packet.ReadFloat();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet) { }

        /// <inheritdoc />
        public void WriteData(IPacket packet) { }
    }

    /// <summary>
    /// Sent from a client to the server to start a round.
    /// </summary>
    internal class StartRoundFromClientToServerData : IPacketData
    {
        /// <summary>
        /// The duration of the initial grace period.
        /// </summary>
        public byte GraceTime;

        /// <summary>
        /// The duration of the round.
        /// </summary>
        public ushort RoundTime;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            GraceTime = packet.ReadByte();
            RoundTime = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(GraceTime);
            packet.Write(RoundTime);
        }
    }

    /// <summary>
    /// Sent from a client to the server to toggle automated rounds.
    /// </summary>
    internal class ToggleAutomationFromClientToServerData : IPacketData
    {
        /// <summary>
        /// The amount of grace time in seconds to automatically start a new round with.
        /// </summary>
        public byte GraceTime;

        /// <summary>
        /// The amount of round time in seconds to automatically start a new round with.
        /// </summary>
        public ushort RoundTime;

        /// <summary>
        /// The amount of time in seconds to wait between automatically starting new rounds.
        /// </summary>
        public ushort SecondsBetweenRounds;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            GraceTime = packet.ReadByte();
            RoundTime = packet.ReadUShort();
            SecondsBetweenRounds = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(GraceTime);
            packet.Write(RoundTime);
            packet.Write(SecondsBetweenRounds);
        }
    }
    #endregion

    #region Server to Client
    /// <summary>
    /// Sent by the server to a client to assign them a team.
    /// </summary>
    internal class AssignTeamFromServerToClientData : IPacketData
    {
        /// <summary>
        /// Whether the assigned player is a hunter.
        /// </summary>
        public bool IsHunter;

        /// <summary>
        /// Whether the round is still in its grace period.
        /// </summary>
        public bool InGrace;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            IsHunter = packet.ReadBool();
            InGrace = packet.ReadBool();
        }

        /// <inheritdoc />
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
        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <summary>
        /// Whether the hunters team won the round.
        /// </summary>
        public bool HuntersWin;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            HuntersWin = packet.ReadBool();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(HuntersWin);
        }
    }

    /// <summary>
    /// Send by the server to a client to indicate that a player has died and was a hunter.
    /// </summary>
    internal class HunterDeathFromServerToClientData : IPacketData
    {
        /// <summary>
        /// The ID of the player who died.
        /// </summary>
        public ushort PlayerId;

        /// <summary>
        /// The convo number to be displayed on all clients.
        /// </summary>
        public byte ConvoNum;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;
        
        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            ConvoNum = packet.ReadByte();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(ConvoNum);
        }
    }

    /// <summary>
    /// Send by the server to a client to indicate that a player has died and was a prop.
    /// </summary>
    internal class PropDeathFromServerToClientData : IPacketData
    {
        /// <summary>
        /// The ID of the player who died.
        /// </summary>
        public ushort PlayerId;

        /// <summary>
        /// The number of props remaining after the player died.
        /// </summary>
        public ushort PropsRemaining;

        /// <summary>
        /// The number of props total after the player died.
        /// </summary>
        public ushort PropsTotal;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;
        
        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            PropsRemaining = packet.ReadUShort();
            PropsTotal = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(PropsRemaining);
            packet.Write(PropsTotal);
        }
    }

    /// <summary>
    /// Sent by the server to a client to indicate that a player has disconnected from the server.
    /// </summary>
    internal class PlayerLeftRoundFromServerToClientData : IPacketData
    {
        /// <summary>
        /// The ID of the player that left the game.
        /// </summary>
        public ushort PlayerId;

        /// <summary>
        /// The number of props remaining after the player left.
        /// </summary>
        public ushort PropsRemaining;

        /// <summary>
        /// The number of props total after the player left.
        /// </summary>
        public ushort PropsTotal;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            PropsRemaining = packet.ReadUShort();
            PropsTotal = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(PlayerId);
            packet.Write(PropsRemaining);
            packet.Write(PropsTotal);
        }
    }

    /// <summary>
    /// Sent by the server to a client to update a remote prop's position along the x- and y-axes.
    /// </summary>
    internal class UpdatePropPositionXyFromServerToClientData : IPacketData
    {
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

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            X = packet.ReadFloat();
            Y = packet.ReadFloat();
        }

        /// <inheritdoc />
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
        /// <summary>
        /// The ID of the player whose prop's z position is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The z component of the prop's position.
        /// </summary>
        public float Z;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;
        
        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Z = packet.ReadFloat();
        }

        /// <inheritdoc />
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
        /// <summary>
        /// The ID of the player whose prop's rotation is being broadcast.
        /// </summary>
        public ushort PlayerId;

        /// <summary>
        /// The rotation of the prop.
        /// </summary>
        public float Rotation;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Rotation = packet.ReadFloat();
        }

        /// <inheritdoc />
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
        /// <summary>
        /// The ID of the player whose prop's scale is being broadcast.
        /// </summary>
        public ushort PlayerId;
        /// <summary>
        /// The scale of the prop.
        /// </summary>
        public float Scale;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            PlayerId = packet.ReadUShort();
            Scale = packet.ReadFloat();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        /// <summary>
        /// The amount of time remaining in the grace period.
        /// </summary>
        public byte TimeRemaining;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;
        
        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            TimeRemaining = packet.ReadByte();
        }

        /// <inheritdoc />
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
        /// <summary>
        /// The amount of time remaining in the round in seconds.
        /// </summary>
        public ushort TimeRemaining;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            TimeRemaining = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(TimeRemaining);
        }
    }

    /// <summary>
    /// Sent by the server to a client to update the amount of time remaining between rounds.
    /// </summary>
    internal class UpdateRoundOverTimerFromServerToClientData : IPacketData
    {
        /// <summary>
        /// The amount of time remaining in seconds before a new round starts.
        /// </summary>
        public ushort TimeRemaining;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public void ReadData(IPacket packet)
        {
            TimeRemaining = packet.ReadUShort();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet)
        {
            packet.Write(TimeRemaining);
        }
    }
    #endregion
}
