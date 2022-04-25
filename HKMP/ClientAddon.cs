using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Networking.Packet;
using PropHunt.Behaviors;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using HKMPVector2 = Hkmp.Math.Vector2;

namespace PropHunt.HKMP
{
    internal class PropHuntClientAddon : ClientAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;

        
        private Dictionary<ushort, Sprite> _cachedPropSprites = new();
        
        private GameObject _damager;

        public static PropHuntClientAddon Instance { get; private set; }
        public IClientApi PropHuntClientAddonApi { get; private set; }

        public override void Initialize(IClientApi clientApi)
        {
            Instance = this;
            PropHuntClientAddonApi = clientApi;
            
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
                    propTransform.rotation = Quaternion.Euler(0, 0, packetData.Rotation);
                }
            );

            receiver.RegisterPacketHandler<PropScaleFromServerToClientData>
            (
                FromServerToClientPackets.SendPropScale,
                packetData =>
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propTransform = player.PlayerObject.transform.Find("Prop");
                    propTransform.localScale = Vector3.one * packetData.ScaleFactor;
                }
            );

            receiver.RegisterPacketHandler<SetPlayingPropHuntFromServerToClientData>
            (
                FromServerToClientPackets.SetPlayingPropHunt,
                packetData =>
                {
                    var propManager = HeroController.instance.GetComponent<LocalPropManager>();
                    if (packetData.Playing)
                    {
                        On.Breakable.Break += OnBreakableBreak;
                    }
                    else
                    {
                        propManager.ClearProp();
                        On.Breakable.Break -= OnBreakableBreak;
                    }

                    propManager.enabled = packetData.Playing;
                }
            );

            clientApi.CommandManager.RegisterCommand(new PropHuntCommand());

            _damager = new GameObject("Damager");
            _damager.layer = (int)PhysLayers.ENEMIES;
            _damager.AddComponent<DamageHero>().damageDealt = 1;
            var col = _damager.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1;
            Object.DontDestroyOnLoad(_damager);
            _damager.SetActive(false);

            clientApi.ClientManager.ConnectEvent          += OnConnect;
            clientApi.ClientManager.PlayerConnectEvent    += OnPlayerConnect;
            clientApi.ClientManager.PlayerEnterSceneEvent += OnPlayerEnterScene;
        }

        private void OnConnect()
        {
            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            propManager ??= HeroController.instance.gameObject.AddComponent<LocalPropManager>();
            propManager.enabled = false;
        }

        private void OnPlayerConnect(IClientPlayer player)
        {
            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            propManager ??= HeroController.instance.gameObject.AddComponent<LocalPropManager>();

            if (!propManager.enabled) return;

            var sender = PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);

            string propName = propManager.PropSprite?.name;

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = propName,
                }
            );

            var propTransform = propManager.Prop.transform;

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionXY,
                new PropPositionXYFromClientToServerData
                {
                    PositionXY = new HKMPVector2(
                        propTransform.localPosition.x,
                        propTransform.localPosition.y),
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropPositionZ,
                new PropPositionZFromClientToServerData
                {
                    PositionZ = propTransform.localPosition.z,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropRotation,
                new PropRotationFromClientToServerData
                {
                    Rotation = propTransform.rotation.eulerAngles.z,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropScale,
                new PropScaleFromClientToServerData
                {
                    ScaleFactor = propTransform.localScale.x,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.SetPlayingPropHunt,
                new SetPlayingPropHuntFromClientToServerData
                {
                    Playing = propManager.enabled,
                }
            );
        }

        private void OnPlayerEnterScene(IClientPlayer player)
        {
            var propManager = player.PlayerObject.GetComponent<RemotePropManager>();
            propManager ??= player.PlayerObject.AddComponent<RemotePropManager>();

            if (!propManager.enabled) return;

            if (_cachedPropSprites.ContainsKey(player.Id))
            {
                propManager.SetPropSprite(_cachedPropSprites[player.Id]);
            }

            var heroPropManager = HeroController.instance.GetComponent<LocalPropManager>();

            var sender = PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
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
                    PositionXY = new HKMPVector2(
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
                    Rotation = heroPropManager.Prop.transform.rotation.eulerAngles.z,
                }
            );

            sender.SendSingleData(
                FromClientToServerPackets.BroadcastPropScale,
                new PropScaleFromClientToServerData
                {
                    ScaleFactor = heroPropManager.Prop.transform.localScale.x,
                }
            );
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
                case FromServerToClientPackets.SendPropScale:
                    return new PropScaleFromServerToClientData();
                case FromServerToClientPackets.SetPlayingPropHunt:
                    return new SetPlayingPropHuntFromServerToClientData();
                default:
                    return null;
            }
        }

        private void OnBreakableBreak(On.Breakable.orig_Break orig, Breakable self, float flingAngleMin, float flingAngleMax, float impactMultiplier)
        {
            orig(self, flingAngleMin, flingAngleMax, impactMultiplier);

            _damager.SetActive(true);
            _damager.transform.SetPosition2D(HeroController.instance.transform.position);
            GameManager.instance.StartCoroutine(DisableDamagerDelayed());
        }

        private IEnumerator DisableDamagerDelayed()
        {
            yield return new WaitForSeconds(0.1f);

            _damager.SetActive(false);
        }
    }
}