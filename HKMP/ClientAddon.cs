using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game;
using Hkmp.Networking.Packet.Data;
using Modding;
using PropHunt.Behaviors;
using PropHunt.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using HKMPVector2 = Hkmp.Math.Vector2;

namespace PropHunt.HKMP
{
    internal class PropHuntClientAddon : ClientAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;

        public static PropHuntClientAddon Instance { get; private set; }
        public IClientApi PropHuntClientAddonApi { get; private set; }

        public override void Initialize(IClientApi clientApi)
        {
            Instance = this;
            PropHuntClientAddonApi = clientApi;

            var sender = clientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            var receiver = clientApi.NetClient.GetNetworkReceiver<FromServerToClientPackets>(Instance, clientPacket =>
            {
                return clientPacket switch
                {
                    FromServerToClientPackets.SendPropSprite => new PropSpriteFromServerToClientData(),
                    FromServerToClientPackets.SendPropPositionXY => new PropPositionXYFromServerToClientData(),
                    FromServerToClientPackets.SendPropPositionZ => new PropPositionZFromServerToClientData(),
                    FromServerToClientPackets.SendPropRotation => new PropRotationFromServerToClientData(),
                    FromServerToClientPackets.SendPropScale => new PropScaleFromServerToClientData(),
                    FromServerToClientPackets.SetPlayingPropHunt => new SetPlayingPropHuntFromServerToClientData(),
                    FromServerToClientPackets.PlayerDeath => new PlayerDeathFromServerToClientData(),
                    FromServerToClientPackets.UpdateRoundTimer => new UpdateRoundTimerFromServerToClientData(),
                    FromServerToClientPackets.UpdateGraceTimer => new UpdateGraceTimerFromServerToClientData(),
                    FromServerToClientPackets.EndRound => new EndRoundFromServerToClientData(),
                    _ => null
                };
            });

            receiver.RegisterPacketHandler<PropSpriteFromServerToClientData>
            (
                FromServerToClientPackets.SendPropSprite,
                packetData =>
                { 
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);
                    var propSprite = string.IsNullOrEmpty(packetData.SpriteName)
                        ? null
                        : Resources.FindObjectsOfTypeAll<Sprite>()
                            .FirstOrDefault(sprite => sprite.name == packetData.SpriteName);
                    var propManager = player.PlayerObject.GetComponent<RemotePropManager>();
                    propManager ??= player.PlayerObject.AddComponent<RemotePropManager>();
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
                    var hunter = HeroController.instance.GetComponent<Hunter>();
                    var playing = packetData.Playing;
                    if (playing)
                    {
                        ModHooks.BeforePlayerDeadHook += OnPlayerDeath;

                        PlayerData.instance.isInvincible = false;

                        var ui = GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>();

                        if (packetData.PropHuntTeam == (byte)PropHuntTeam.Hunters)
                        {
                            clientApi.ClientManager.ChangeTeam(Team.Grimm);

                            hunter.enabled = true;
                            hunter.BeginGracePeriod(packetData.GracePeriod);
                            propManager.enabled = false;

                            PropHunt.Instance.Log("You are a HUNTER");
                            ui.SetPropHuntMessage("You are a hunter!");

                            HeroController.instance.SetMPCharge(198);
                            GameManager.instance.soulOrb_fsm.SendEvent("MP GAIN");

                            On.Breakable.Break += OnBreakableBreak;
                        }
                        else if (packetData.PropHuntTeam == (byte)PropHuntTeam.Props)
                        {
                            clientApi.ClientManager.ChangeTeam(Team.Moss);

                            hunter.enabled = false;
                            propManager.enabled = true;

                            PropHunt.Instance.Log("You are a PROP");
                            ui.SetPropHuntMessage("You are a prop!");

                            On.Breakable.Break -= OnBreakableBreak;
                        }
                    }
                    else
                    {
                        ModHooks.BeforePlayerDeadHook -= OnPlayerDeath;

                        var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                        var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
                        blankerCtrl.SendEvent("FADE OUT INSTANT");

                        PlayerData.instance.isInvincible = true;

                        clientApi.ClientManager.ChangeTeam(Team.None);

                        hunter.enabled = false;
                        propManager.enabled = false;
                        var ui = GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>();
                        ui.SetTimeRemainingInRound(0);
                        ui.SetGraceTimeRemaining(0);

                        On.Breakable.Break -= OnBreakableBreak;
                    }
                }
            );

            receiver.RegisterPacketHandler<PlayerDeathFromServerToClientData>
            (
                FromServerToClientPackets.PlayerDeath,
                packetData =>
                {
                    var player = clientApi.ClientManager.GetPlayer(packetData.PlayerId);

                    PropHunt.Instance.Log($"Player {player.Username} has died.");
                    
                    string text = $"Player {player.Username} has died!" +
                                  $"\nProps remaining: {packetData.PropsRemaining}/{packetData.PropsTotal}" +
                                  $"\nHunters remaining: {packetData.HuntersRemaining}/{packetData.HuntersTotal}";

                    GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>().SetPropHuntMessage(text);
                }
            );

            receiver.RegisterPacketHandler<UpdateRoundTimerFromServerToClientData>
            (
                FromServerToClientPackets.UpdateRoundTimer,
                packetData =>
                {
                    int timeRemaining = packetData.TimeRemaining;
                    PropHunt.Instance.Log("Seconds remaining in round: " + timeRemaining);
                    GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>().SetTimeRemainingInRound(timeRemaining);
                }
            );

            receiver.RegisterPacketHandler<UpdateGraceTimerFromServerToClientData>
            (
                FromServerToClientPackets.UpdateGraceTimer,
                packetData =>
                {
                    int timeRemaining = packetData.TimeRemaining;
                    PropHunt.Instance.Log("Grace time remaining: " + timeRemaining);
                    GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>().SetGraceTimeRemaining(timeRemaining);
                }
            );

            receiver.RegisterPacketHandler<EndRoundFromServerToClientData>
            (
                FromServerToClientPackets.EndRound,
                packetData =>
                {
                    ModHooks.BeforePlayerDeadHook -= OnPlayerDeath;

                    var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                    var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
                    blankerCtrl.SendEvent("FADE OUT INSTANT");

                    PlayerData.instance.isInvincible = true;

                    clientApi.ClientManager.ChangeTeam(Team.None);

                    InitComponents();

                    var ui = GameCameras.instance.hudCanvas.GetComponent<UIPropHunt>();

                    On.Breakable.Break -= OnBreakableBreak;

                    bool huntersWin = packetData.HuntersWin;
                    if (huntersWin)
                    {
                        PropHunt.Instance.Log("HUNTERS WIN");
                        ui.SetPropHuntMessage("Hunters win!");
                    }
                    else
                    {
                        PropHunt.Instance.Log("PROPS WIN");
                        ui.SetPropHuntMessage("Props win!");
                    }
                }
            );

            clientApi.CommandManager.RegisterCommand(new PropHuntCommand());
            
            clientApi.ClientManager.ConnectEvent += () => InitComponents();
            clientApi.ClientManager.DisconnectEvent += () =>
            {
                sender.SendSingleData(
                    FromClientToServerPackets.PlayerDeath,
                    new ReliableEmptyData());
            };

            clientApi.ClientManager.PlayerConnectEvent += player =>
            {
                var localPropManager = HeroController.instance.GetComponent<LocalPropManager>();
                localPropManager ??= HeroController.instance.gameObject.AddComponent<LocalPropManager>();

                if (!localPropManager.enabled) return;

                var sender = PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);

                string propName = localPropManager.PropSprite?.name;

                sender.SendSingleData(
                    FromClientToServerPackets.BroadcastPropSprite,
                    new PropSpriteFromClientToServerData
                    {
                        SpriteName = propName,
                    }
                );

                var propTransform = localPropManager.Prop.transform;

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
            };
            clientApi.ClientManager.PlayerEnterSceneEvent += player =>
            {
                var heroPropManager = HeroController.instance.GetComponent<LocalPropManager>();
                heroPropManager ??= HeroController.instance.gameObject.AddComponent<LocalPropManager>();

                if (heroPropManager.PropSprite == null) return;
                
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
            };
        }

        private void OnPlayerDeath()
        {
            var sender = PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            sender.SendSingleData(
                FromClientToServerPackets.PlayerDeath,
                new ReliableEmptyData());
        }

        private void OnBreakableBreak(On.Breakable.orig_Break orig, Breakable self, float flingAngleMin, float flingAngleMax, float impactMultiplier)
        {
            HeroController.instance.TakeDamage(null, CollisionSide.top, 1, (int)HazardType.PIT);

            orig(self, flingAngleMin, flingAngleMax, impactMultiplier);
        }

        /// <summary>
        /// Initialize components on the local player object.
        /// </summary>
        private void InitComponents()
        {
            var hunter = HeroController.instance.GetComponent<Hunter>();
            hunter ??= HeroController.instance.gameObject.AddComponent<Hunter>();
            hunter.enabled = false;

            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            propManager ??= HeroController.instance.gameObject.AddComponent<LocalPropManager>();
            propManager.enabled = false;
        }
    }
}