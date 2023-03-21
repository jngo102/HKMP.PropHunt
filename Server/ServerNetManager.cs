using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Networking.Packet;
using PropHunt.HKMP;
using System;

namespace PropHunt.Server
{
    /// <summary>
    /// Manages server side network events.
    /// </summary>
    internal class ServerNetManager
    {
        /// <summary>
        /// Event that is invoked when a player dies as a hunter.
        /// </summary>
        public event Action<ushort> HunterDeathEvent;

        /// <summary>
        /// Event that is invoked when a player dies as a prop.
        /// </summary>
        public event Action<ushort> PropDeathEvent;

        /// <summary>
        /// Event that is invoked when a player moves their prop along the x- or y-axes.
        /// </summary>
        public event Action<ushort, BroadcastPropPositionXyFromClientToServerData> UpdatePropPositionXyEvent;

        /// <summary>
        /// Event that is invoked when a player moves their prop along the z-axis.
        /// </summary>
        public event Action<ushort, BroadcastPropPositionZFromClientToServerData> UpdatePropPositionZEvent;

        /// <summary>
        /// Event that is invoked when a player rotates their prop.
        /// </summary>
        public event Action<ushort, BroadcastPropRotationFromClientToServerData> UpdatePropRotationEvent;

        /// <summary>
        /// Event that is invoked when a player scales their prop.
        /// </summary>
        public event Action<ushort, BroadcastPropScaleFromClientToServerData> UpdatePropScaleEvent;

        /// <summary>
        /// Event that is invoked when a player changes their prop.
        /// </summary>
        public event Action<ushort, BroadcastPropSpriteFromClientToServerData> UpdatePropSpriteEvent;

        /// <summary>
        /// Event that is invoked when a player requests to end a round.
        /// </summary>
        public event Action<EndRoundFromClientToServerData> EndRoundEvent;

        /// <summary>
        /// Event that is invoked when a player requests to start a new round.
        /// </summary>
        public event Action<StartRoundFromClientToServerData> StartRoundEvent;

        
        /// <summary>
        /// Event that is invoked when a player requests to toggle automated rounds.
        /// </summary>
        public event Action<ToggleAutomationFromClientToServerData> ToggleAutomationEvent;

        /// <summary>
        /// Network sender of packets from the server to a client.
        /// </summary>
        private static IServerAddonNetworkSender<FromServerToClientPackets> _sender;

        /// <summary>
        /// Constructor for the server net manager.
        /// </summary>
        /// <param name="addon">The server add-on instance.</param>
        /// <param name="netServer">The net server instance.</param>
        public ServerNetManager(ServerAddon addon, INetServer netServer)
        {
            _sender = netServer.GetNetworkSender<FromServerToClientPackets>(addon);

            var receiver = netServer.GetNetworkReceiver<FromClientToServerPackets>(addon, InstantiatePacket);

            receiver.RegisterPacketHandler<BroadcastHunterDeathFromClientToServerData>(
                FromClientToServerPackets.BroadcastHunterDeath,
                (id, _) => HunterDeathEvent?.Invoke(id));

            receiver.RegisterPacketHandler<BroadcastPropDeathFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropDeath,
                (id, _) => PropDeathEvent?.Invoke(id));

            receiver.RegisterPacketHandler<BroadcastPropPositionXyFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropPositionXy,
                (id, packetData) => UpdatePropPositionXyEvent?.Invoke(id, packetData));

            receiver.RegisterPacketHandler<BroadcastPropPositionZFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) => UpdatePropPositionZEvent?.Invoke(id, packetData));

            receiver.RegisterPacketHandler<BroadcastPropRotationFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) => UpdatePropRotationEvent?.Invoke(id, packetData));

            receiver.RegisterPacketHandler<BroadcastPropScaleFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropScale,
                (id, packetData) => UpdatePropScaleEvent?.Invoke(id, packetData));

            receiver.RegisterPacketHandler<BroadcastPropSpriteFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) => UpdatePropSpriteEvent?.Invoke(id, packetData));

            receiver.RegisterPacketHandler<EndRoundFromClientToServerData>(
                FromClientToServerPackets.EndRound,
                (_, packetData) => EndRoundEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<StartRoundFromClientToServerData>(
                FromClientToServerPackets.StartRound,
                (_, packetData) => StartRoundEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<ToggleAutomationFromClientToServerData>(
                FromClientToServerPackets.ToggleAutomation,
                (_, packetData) => ToggleAutomationEvent?.Invoke(packetData));
        }

        /// <summary>
        /// Send a packet from the server to a client.
        /// </summary>
        /// <param name="packetId">The ID of the packet to be sent.</param>
        /// <param name="packetData">The data of the packet to be sent.</param>
        /// <param name="playerIds">A list of player IDs to send the packet to.</param>
        public void SendPacket(FromServerToClientPackets packetId, IPacketData packetData, params ushort[] playerIds)
        {
            _sender.SendSingleData(packetId, packetData, playerIds);
        }

        /// <summary>
        /// Broadcast a packet from the server to all clients.
        /// </summary>
        /// <param name="packetId"></param>
        /// <param name="packetData"></param>
        public void BroadcastPacket(FromServerToClientPackets packetId, IPacketData packetData)
        {
            _sender.BroadcastSingleData(packetId, packetData);
        }

        /// <summary>
        /// Instantiates a packet from a given packet ID.
        /// </summary>
        /// <param name="packetId">The packet ID.</param>
        /// <returns>The appropriately mapped packet.</returns>
        private IPacketData InstantiatePacket(FromClientToServerPackets packetId)
        {
            switch (packetId)
            {
                case FromClientToServerPackets.BroadcastHunterDeath:
                    return new BroadcastHunterDeathFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropDeath:
                    return new BroadcastPropDeathFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionXy:
                    return new BroadcastPropPositionXyFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionZ:
                    return new BroadcastPropPositionZFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropRotation:
                    return new BroadcastPropRotationFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropScale:
                    return new BroadcastPropScaleFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropSprite:
                    return new BroadcastPropSpriteFromClientToServerData();
                case FromClientToServerPackets.EndRound:
                    return new EndRoundFromClientToServerData();
                case FromClientToServerPackets.StartRound:
                    return new StartRoundFromClientToServerData();
                case FromClientToServerPackets.ToggleAutomation:
                    return new ToggleAutomationFromClientToServerData();
            }

            return null;
        }
    }
}
