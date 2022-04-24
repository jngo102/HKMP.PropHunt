using Hkmp.Api.Server;
using Hkmp.Networking.Packet;

namespace PropHunt.HKMP
{
    internal class PropHuntServer : ServerAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;

        public static PropHuntServer Instance { get; private set; }

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
                    sender.BroadcastSingleData(FromServerToClientPackets.SendPropPositionXY, new PropPositionXYFromServerToClientData
                    {
                        PlayerId = id,
                        PositionXY = packetData.PositionXY,
                    });
                }
            );

            receiver.RegisterPacketHandler<PropPositionZFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) =>
                {
                    sender.BroadcastSingleData(FromServerToClientPackets.SendPropPositionZ, new PropPositionZFromServerToClientData
                    {
                        PlayerId = id,
                        PositionZ = packetData.PositionZ,
                    });
                }
            );

            receiver.RegisterPacketHandler<PropRotationFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) =>
                {
                    sender.BroadcastSingleData(FromServerToClientPackets.SendPropRotation, new PropRotationFromServerToClientData
                    {
                        PlayerId = id,
                        Rotation = packetData.Rotation,
                    });
                }
            );

            serverApi.ServerManager.PlayerConnectEvent    += OnPlayerConnect;
            serverApi.ServerManager.PlayerDisconnectEvent += OnPlayerDisconnect;
            serverApi.ServerManager.PlayerEnterSceneEvent += OnPlayerEnterScene;
            serverApi.ServerManager.PlayerLeaveSceneEvent += OnPlayerLeaveScene;
        }

        private void OnPlayerConnect(IServerPlayer player)
        {

        }

        private void OnPlayerDisconnect(IServerPlayer player)
        {

        }

        private void OnPlayerEnterScene(IServerPlayer player)
        {

        }

        private void OnPlayerLeaveScene(IServerPlayer player)
        {

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
                default:
                    return null;
            }
        }
    }
}