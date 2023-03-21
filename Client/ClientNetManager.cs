using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Networking.Packet;
using PropHunt.HKMP;
using System;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages client side network events.
    /// </summary>
    internal class ClientNetManager
    {
        /// <summary>
        /// Event that is invoked when the local player is assigned a team.
        /// </summary>
        public event Action<AssignTeamFromServerToClientData> AssignTeamEvent;

        /// <summary>
        /// Event that is invoked when the round is over.
        /// </summary>
        public event Action<EndRoundFromServerToClientData> EndRoundEvent;

        /// <summary>
        /// Event that is invoked when a hunter dies.
        /// </summary>
        public event Action<HunterDeathFromServerToClientData> HunterDeathEvent;

        /// <summary>
        /// Event that is invoked when a prop dies.
        /// </summary>
        public event Action<PropDeathFromServerToClientData> PropDeathEvent;

        /// <summary>
        /// Event that is invoked when a player leaves the current round.
        /// </summary>
        public event Action<PlayerLeftRoundFromServerToClientData> PlayerLeftRoundEvent;

        /// <summary>
        /// Event that is invoked when a player updates their prop's position along the x- and y-axes.
        /// </summary>
        public event Action<UpdatePropPositionXyFromServerToClientData> UpdatePropPositionXyEvent;

        /// <summary>
        /// Event that is invoked when a player updates their prop's position along the z-axis.
        /// </summary>
        public event Action<UpdatePropPositionZFromServerToClientData> UpdatePropPositionZEvent;

        /// <summary>
        /// Event that is invoked when a player updates their prop's rotation.
        /// </summary>
        public event Action<UpdatePropRotationFromServerToClientData> UpdatePropRotationEvent;

        /// <summary>
        /// Event that is invoked when a player updates their prop's scale.
        /// </summary>
        public event Action<UpdatePropScaleFromServerToClientData> UpdatePropScaleEvent;

        /// <summary>
        /// Event that is invoked when a player updates their prop's sprite.
        /// </summary>
        public event Action<UpdatePropSpriteFromServerToClientData> UpdatePropSpriteEvent;

        /// <summary>
        /// Event that is invoked when the grace timer is updated.
        /// </summary>
        public event Action<UpdateGraceTimerFromServerToClientData> UpdateGraceTimerEvent;

        /// <summary>
        /// Event that is invoked when the round timer is updated.
        /// </summary>
        public event Action<UpdateRoundTimerFromServerToClientData> UpdateRoundTimerEvent;

        /// <summary>
        /// Event that is invoked when the round over timer is updated.
        /// </summary>
        public event Action<UpdateRoundOverTimerFromServerToClientData> UpdateRoundOverTimerEvent;

        /// <summary>
        /// Network sender of packets from the local client to the server.
        /// </summary>
        private static IClientAddonNetworkSender<FromClientToServerPackets> _sender;

        /// <summary>
        /// Constructor for the client net manager.
        /// </summary>
        /// <param name="addon">The client add-on instance.</param>
        /// <param name="netClient">The net client instance.</param>
        public ClientNetManager(ClientAddon addon, INetClient netClient)
        {
            _sender = netClient.GetNetworkSender<FromClientToServerPackets>(addon);

            var receiver = netClient.GetNetworkReceiver<FromServerToClientPackets>(addon, InstantiatePacket);

            receiver.RegisterPacketHandler<AssignTeamFromServerToClientData>(
                FromServerToClientPackets.AssignTeam,
                packetData => AssignTeamEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<EndRoundFromServerToClientData>(
                FromServerToClientPackets.EndRound,
                packetData => EndRoundEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<HunterDeathFromServerToClientData>(
                FromServerToClientPackets.HunterDeath,
                packetData => HunterDeathEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<PropDeathFromServerToClientData>(
                FromServerToClientPackets.PropDeath,
                packetData => PropDeathEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<PlayerLeftRoundFromServerToClientData>(
                FromServerToClientPackets.PlayerLeftRound,
                packetData => PlayerLeftRoundEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdatePropPositionXyFromServerToClientData>(
                FromServerToClientPackets.UpdatePropPositionXy,
                packetData => UpdatePropPositionXyEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdatePropPositionZFromServerToClientData>(
                FromServerToClientPackets.UpdatePropPositionZ,
                packetData => UpdatePropPositionZEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdatePropRotationFromServerToClientData>(
                FromServerToClientPackets.UpdatePropRotation,
                packetData => UpdatePropRotationEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdatePropScaleFromServerToClientData>(
                FromServerToClientPackets.UpdatePropScale,
                packetData => UpdatePropScaleEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdatePropSpriteFromServerToClientData>(
                FromServerToClientPackets.UpdatePropSprite,
                packetData => UpdatePropSpriteEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdateGraceTimerFromServerToClientData>(
                FromServerToClientPackets.UpdateGraceTimer,
                packetData => UpdateGraceTimerEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdateRoundTimerFromServerToClientData>(
                FromServerToClientPackets.UpdateRoundTimer,
                packetData => UpdateRoundTimerEvent?.Invoke(packetData));

            receiver.RegisterPacketHandler<UpdateRoundOverTimerFromServerToClientData>(
                FromServerToClientPackets.UpdateRoundOverTimer,
                packetData => UpdateRoundOverTimerEvent?.Invoke(packetData));
        }

        /// <summary>
        /// Send a packet from the local client to the server.
        /// </summary>
        /// <param name="packetId">The ID of the packet to be sent.</param>
        /// <param name="packetData">The data of the packet to be sent.</param>
        public static void SendPacket(FromClientToServerPackets packetId, IPacketData packetData)
        {
            _sender.SendSingleData(packetId, packetData);
        }

        /// <summary>
        /// Instantiates a packet from a given packet ID.
        /// </summary>
        /// <param name="packetId">The packet ID.</param>
        /// <returns>The appropriately mapped packet.</returns>
        private IPacketData InstantiatePacket(FromServerToClientPackets packetId)
        {
            switch (packetId)
            {
                case FromServerToClientPackets.AssignTeam:
                    return new AssignTeamFromServerToClientData();
                case FromServerToClientPackets.EndRound:
                    return new EndRoundFromServerToClientData();
                case FromServerToClientPackets.HunterDeath:
                    return new HunterDeathFromServerToClientData();
                case FromServerToClientPackets.PropDeath:
                    return new PropDeathFromServerToClientData();
                case FromServerToClientPackets.PlayerLeftRound:
                    return new PlayerLeftRoundFromServerToClientData();
                case FromServerToClientPackets.UpdatePropPositionXy:
                    return new UpdatePropPositionXyFromServerToClientData();
                case FromServerToClientPackets.UpdatePropPositionZ:
                    return new UpdatePropPositionZFromServerToClientData();
                case FromServerToClientPackets.UpdatePropRotation:
                    return new UpdatePropRotationFromServerToClientData();
                case FromServerToClientPackets.UpdatePropScale:
                    return new UpdatePropScaleFromServerToClientData();
                case FromServerToClientPackets.UpdatePropSprite:
                    return new UpdatePropSpriteFromServerToClientData();
                case FromServerToClientPackets.UpdateGraceTimer:
                    return new UpdateGraceTimerFromServerToClientData();
                case FromServerToClientPackets.UpdateRoundTimer:
                    return new UpdateRoundTimerFromServerToClientData();
                case FromServerToClientPackets.UpdateRoundOverTimer:
                    return new UpdateRoundOverTimerFromServerToClientData();
            }

            return null;
        }
    }
}
