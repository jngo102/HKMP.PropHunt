using Hkmp.Api.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using PropHunt.Behaviors;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PropHunt.HKMP
{
    internal class PropHuntClient : ClientAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;

        private Dictionary<ushort, Sprite> _cachedPropSprites = new();

        public static PropHuntClient Instance { get; private set; }
        public IClientApi PropHuntClientApi { get; private set; }

        public override void Initialize(IClientApi clientApi)
        {
            Instance = this;
            PropHuntClientApi = clientApi;

            var sender = clientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            var receiver = clientApi.NetClient.GetNetworkReceiver<FromServerToClientPackets>(Instance, InstantiatePacket);

            receiver.RegisterPacketHandler<PropSpriteFromServerToClientData>
            (
                FromServerToClientPackets.SendPropSprite,
                packetData => 
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(sprite => sprite.name == packetData.SpriteName);
                    PropHunt.Instance.Log("Caching sprite of player: " + packetData.PlayerId);
                    _cachedPropSprites[packetData.PlayerId] = propSprite;
                    var propManager = player.PlayerObject.GetComponent<RemotePropManager>();
                    propManager.SetPropSprite(propSprite);
                }
            );

            receiver.RegisterPacketHandler<PropPositionXYFromServerToClientData>
            (
                FromServerToClientPackets.SendPropPositionXY,
                packetData => 
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propTransform = player.PlayerObject.transform.Find("Prop");
                    var newPos = new Vector3(packetData.PositionXY.X, packetData.PositionXY.Y, propTransform.localPosition.z);
                    PropHunt.Instance.Log("Setting prop position XY to " + packetData.PositionXY);
                    propTransform.localPosition = newPos;
                }
            );

            receiver.RegisterPacketHandler<PropPositionZFromServerToClientData>
            (
                FromServerToClientPackets.SendPropPositionZ,
                packetData =>
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propTransform = player.PlayerObject.transform.Find("Prop");
                    var newPos = new Vector3(propTransform.localPosition.x, propTransform.localPosition.y, packetData.PositionZ);
                    PropHunt.Instance.Log("Setting prop position Z to " + packetData.PositionZ);
                    propTransform.localPosition = newPos;
                }
            );

            receiver.RegisterPacketHandler<PropRotationFromServerToClientData>
            (
                FromServerToClientPackets.SendPropRotation,
                packetData =>
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propTransform = player.PlayerObject.transform.Find("Prop");
                    PropHunt.Instance.Log("Setting prop rotation to " + packetData.Rotation);
                    propTransform.localRotation = Quaternion.Euler(0, 0, packetData.Rotation);
                }
            );

            clientApi.ClientManager.ConnectEvent          += OnConnect;
            clientApi.ClientManager.PlayerConnectEvent    += OnPlayerConnect;
            clientApi.ClientManager.PlayerDisconnectEvent += OnPlayerDisconnect;
            clientApi.ClientManager.PlayerEnterSceneEvent += OnPlayerEnterScene;
            clientApi.ClientManager.PlayerLeaveSceneEvent += OnPlayerLeaveScene;
        }

        private void OnConnect()
        {
            if (!HeroController.instance.GetComponent<LocalPropManager>())
            {
                HeroController.instance.gameObject.AddComponent<LocalPropManager>().enabled = true;
            }
        }

        private void OnPlayerConnect(IClientPlayer player)
        {
            var sender = PropHuntClientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = propManager.PropSprite.name,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionXY,
                new PropPositionXYFromClientToServerData
                {
                    PositionXY = new Hkmp.Math.Vector2(
                        propManager.Prop.transform.localPosition.x, 
                        propManager.Prop.transform.localPosition.y),
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionZ,
                new PropPositionZFromClientToServerData
                {
                    PositionZ = propManager.Prop.transform.localPosition.z,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropRotation,
                new PropRotationFromClientToServerData
                {
                    Rotation = propManager.Prop.transform.localRotation.eulerAngles.z,
                }
            );
        }

        private void OnPlayerDisconnect(IClientPlayer player)
        {

        }

        private void OnPlayerEnterScene(IClientPlayer player)
        {
            var propManager = player.PlayerObject.GetComponent<RemotePropManager>();
            propManager ??= player.PlayerObject.AddComponent<RemotePropManager>();

            if (_cachedPropSprites.ContainsKey(player.Id))
            {
                propManager.SetPropSprite(_cachedPropSprites[player.Id]);
            }

            var heroPropManager = HeroController.instance.GetComponent<LocalPropManager>();

            var sender = PropHuntClientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = heroPropManager.PropSprite.name,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionXY,
                new PropPositionXYFromClientToServerData
                {
                    PositionXY = new Hkmp.Math.Vector2(
                        heroPropManager.Prop.transform.localPosition.x, 
                        heroPropManager.Prop.transform.localPosition.y),
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionZ,
                new PropPositionZFromClientToServerData
                {
                    PositionZ = heroPropManager.Prop.transform.localPosition.z,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropRotation,
                new PropRotationFromClientToServerData
                {
                    Rotation = heroPropManager.Prop.transform.localRotation.eulerAngles.z,
                }
            );
        }

        private void OnPlayerLeaveScene(IClientPlayer player)
        {

        }

        private static IPacketData InstantiatePacket(FromServerToClientPackets clientPacket)
        {
            switch (clientPacket)
            {
                case FromServerToClientPackets.SendPropSprite:
                    return new PropSpriteFromServerToClientData();
                case FromServerToClientPackets.SendPropPositionXY:
                    return new PropPositionXYFromServerToClientData();
                case FromServerToClientPackets.SendPropPositionZ:
                    return new PropPositionZFromServerToClientData();
                case FromServerToClientPackets.SendPropRotation:
                    return new PropRotationFromServerToClientData();
                default:
                    return null;
            }
        }
    }
}