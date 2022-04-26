using System;
using Hkmp.Api.Server;
using Hkmp.Networking.Packet;
using System.Linq;
using Hkmp.Game;
using PropHunt.Behaviors;
using Random = UnityEngine.Random;

namespace PropHunt.HKMP
{
    internal class PropHuntServerAddon : ServerAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;

        public static PropHuntServerAddon Instance { get; private set; }

        public override void Initialize(IServerApi serverApi)
        {
            Instance = this;

            var sender = serverApi.NetServer.GetNetworkSender<FromServerToClientPackets>(Instance);
            var receiver = serverApi.NetServer.GetNetworkReceiver<FromClientToServerPackets>(Instance, InstantiatePacket);

            receiver.RegisterPacketHandler<PropSpriteFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var otherPlayers = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(FromServerToClientPackets.SendPropSprite, new PropSpriteFromServerToClientData
                    {
                        PlayerId = id,
                        SpriteName = packetData.SpriteName,
                    }, otherPlayers);
                }
            );

            receiver.RegisterPacketHandler<PropPositionXYFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionXY,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(FromServerToClientPackets.SendPropPositionXY, new PropPositionXYFromServerToClientData
                    {
                        PlayerId = id,
                        PositionXY = packetData.PositionXY,
                    }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropPositionZFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(FromServerToClientPackets.SendPropPositionZ, new PropPositionZFromServerToClientData
                    {
                        PlayerId = id,
                        PositionZ = packetData.PositionZ,
                    }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropRotationFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(FromServerToClientPackets.SendPropRotation, new PropRotationFromServerToClientData
                    {
                        PlayerId = id,
                        Rotation = packetData.Rotation,
                    }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropScaleFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropScale,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(FromServerToClientPackets.SendPropScale, new PropScaleFromServerToClientData
                    {
                        PlayerId = id,
                        ScaleFactor = packetData.ScaleFactor,
                    }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<SetPlayingPropHuntFromClientToServerData>
            (
                FromClientToServerPackets.SetPlayingPropHunt,
                (id, packetData) =>
                {
                    var players = serverApi.ServerManager.Players.ToList();
                    players = players.OrderBy(_ => Guid.NewGuid()).ToList();
                    int halfCount = players.Count / 2;
                    var hunters = players.GetRange(0, halfCount);
                    var props = players.GetRange(halfCount, players.Count - halfCount);

                    sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromServerToClientData
                    {
                        PropHuntTeam = (byte)PropHuntTeam.Hunters,
                        PlayerId = id,
                        Playing = packetData.Playing,
                        GracePeriod = packetData.GracePeriod,
                    }, hunters.Select(hunter => hunter.Id).ToArray());

                    sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromServerToClientData
                    {
                        PropHuntTeam = (byte)PropHuntTeam.Props,
                        PlayerId = id,
                        Playing = packetData.Playing,
                        GracePeriod = packetData.GracePeriod,
                    }, props.Select(prop => prop.Id).ToArray());
                }
            );
        }
        
        private static IPacketData InstantiatePacket(FromClientToServerPackets serverPacket)
        {
            switch (serverPacket)
            {
                case FromClientToServerPackets.BroadcastPropSprite:
                    return new PropSpriteFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionXY:
                    return new PropPositionXYFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionZ:
                    return new PropPositionZFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropRotation:
                    return new PropRotationFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropScale:
                    return new PropScaleFromClientToServerData();
                case FromClientToServerPackets.SetPlayingPropHunt:
                    return new SetPlayingPropHuntFromClientToServerData();
                default:
                    return null;
            }
        }
    }
}