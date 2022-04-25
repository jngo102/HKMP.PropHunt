using Hkmp.Api.Server;
using Hkmp.Networking.Packet;
using System.Linq;

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
                    sender.BroadcastSingleData(FromServerToClientPackets.SendPropSprite, new PropSpriteFromServerToClientData
                    {
                        PlayerId = id,
                        SpriteName = packetData.SpriteName,
                    });
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
                    
                    sender.BroadcastSingleData(FromServerToClientPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromServerToClientData()
                    {
                        PlayerId = id,
                        Playing = packetData.Playing,
                    });
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