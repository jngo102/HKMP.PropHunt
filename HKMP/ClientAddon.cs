using System.Collections;
using System.Linq;
using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Game;
using Modding;
using Modding.Utils;
using PropHunt.Behaviors;
using PropHunt.UI;
using Satchel;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropHunt.HKMP
{
    internal class PropHuntClientAddon : ClientAddon
    {
        protected override string Name => Constants.NAME;
        protected override string Version => PropHunt.Instance.GetVersion();
        public override bool NeedsNetwork => true;
        public static PropHuntClientAddon Instance { get; private set; }

        public static IClientApi Api { get; private set; }
        private static IClientAddonNetworkSender<FromClientToServerPackets> _sender;
        private static IClientAddonNetworkReceiver<FromServerToClientPackets> _receiver;

        public override void Initialize(IClientApi clientApi)
        {
            Instance = this;

            Api = clientApi;
            
            _sender = clientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            _receiver = clientApi.NetClient.GetNetworkReceiver<FromServerToClientPackets>(Instance, clientPacket =>
            {
                return clientPacket switch
                {
                    FromServerToClientPackets.AssignTeam => new AssignTeamFromServerToClientData(),
                    FromServerToClientPackets.EndRound => new EndRoundFromServerToClientData(),
                    FromServerToClientPackets.PlayerDeath => new PlayerDeathFromServerToClientData(),
                    FromServerToClientPackets.PlayerLeftGame => new PlayerDeathFromServerToClientData(),
                    FromServerToClientPackets.UpdateGraceTimer => new UpdateGraceTimerFromServerToClientData(),
                    FromServerToClientPackets.UpdateRoundTimer => new UpdateRoundTimerFromServerToClientData(),
                    FromServerToClientPackets.UpdatePropPositionXY => new UpdatePropPositionXYFromServerToClientData(),
                    FromServerToClientPackets.UpdatePropPositionZ => new UpdatePropPositionZFromServerToClientData(),
                    FromServerToClientPackets.UpdatePropRotation => new UpdatePropRotationFromServerToClientData(),
                    FromServerToClientPackets.UpdatePropScale => new UpdatePropScaleFromServerToClientData(),
                    FromServerToClientPackets.UpdatePropSprite => new UpdatePropSpriteFromServerToClientData(),
                    _ => null,
                };
            });

            _receiver.RegisterPacketHandler<AssignTeamFromServerToClientData>(FromServerToClientPackets.AssignTeam,
                packetData =>
                {
                    ModHooks.BeforePlayerDeadHook += BroadcastPlayerDeath;

                    var propManager = HeroController.instance.GetComponent<LocalPropManager>();
                    var hunter = HeroController.instance.GetComponent<Hunter>();
                    var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                    PlayerData.instance.isInvincible = false;

                    if (packetData.IsHunter)
                    {
                        clientApi.ClientManager.ChangeTeam(Team.Grimm);

                        propManager.enabled = false;
                        hunter.enabled = true;

                        if (packetData.InGrace)
                        {
                            hunter.BeginGracePeriod();
                        }

                        PropHunt.Instance.Log("You are a HUNTER");
                        ui.SetPropHuntMessage("You are a hunter!");

                        HeroController.instance.SetMPCharge(198);
                        GameManager.instance.soulOrb_fsm.SendEvent("MP GAIN");

                        On.Breakable.Break += OnBreakableBreak;
                    }
                    else
                    {
                        clientApi.ClientManager.ChangeTeam(Team.Moss);

                        hunter.enabled = false;
                        propManager.enabled = true;

                        PropHunt.Instance.Log("You are a PROP");
                        ui.SetPropHuntMessage("You are a prop!");
                    }

                    USceneManager.LoadScene(USceneManager.GetActiveScene().name, LoadSceneMode.Single);
                });

            _receiver.RegisterPacketHandler<EndRoundFromServerToClientData>(FromServerToClientPackets.EndRound,
                packetData =>
                {
                    var propManager = HeroController.instance.GetComponent<LocalPropManager>();
                    var hunter = HeroController.instance.GetComponent<Hunter>();

                    ModHooks.BeforePlayerDeadHook -= BroadcastPlayerDeath;

                    var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                    var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
                    blankerCtrl.SendEvent("FADE OUT INSTANT");

                    PlayerData.instance.isInvincible = true;

                    clientApi.ClientManager.ChangeTeam(Team.None);

                    hunter.enabled = false;
                    propManager.enabled = true;
                    var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                    ui.SetTimeRemainingInRound(0);
                    ui.SetGraceTimeRemaining(0);

                    On.Breakable.Break -= OnBreakableBreak;

                    InitComponents();

                    if (packetData.HuntersWin)
                    {
                        PropHunt.Instance.Log("HUNTERS WIN");
                        ui.SetPropHuntMessage("Hunters win!");
                    }
                    else
                    {
                        PropHunt.Instance.Log("PROPS WIN");
                        ui.SetPropHuntMessage("Props win!");
                    }
                });

            _receiver.RegisterPacketHandler<PlayerDeathFromServerToClientData>(FromServerToClientPackets.PlayerDeath,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        string text = $"Player {player.Username} has died!" +
                                      $"\nProps remaining: {packetData.PropsRemaining}/{packetData.PropsTotal}" +
                                      $"\nHunters remaining: {packetData.HuntersRemaining}/{packetData.HuntersTotal}";

                        PropHunt.Instance.Log(text);
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        ui.SetPropHuntMessage(text);
                    }
                });

            _receiver.RegisterPacketHandler<PlayerLeftGameFromServerToClientData>(FromServerToClientPackets.PlayerLeftGame,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        string text = $"Player {player.Username} has left the server!" +
                                      $"\nProps remaining: {packetData.PropsRemaining}/{packetData.PropsTotal}" +
                                      $"\nHunters remaining: {packetData.HuntersRemaining}/{packetData.HuntersTotal}";

                        PropHunt.Instance.Log(text);
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        ui.SetPropHuntMessage(text);
                    }
                });

            _receiver.RegisterPacketHandler<UpdateGraceTimerFromServerToClientData>(
                FromServerToClientPackets.UpdateGraceTimer,
                packetData =>
                {
                    var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                    ui.SetGraceTimeRemaining(packetData.TimeRemaining);
                });

            _receiver.RegisterPacketHandler<UpdateRoundTimerFromServerToClientData>(
                FromServerToClientPackets.UpdateRoundTimer,
                packetData =>
                {
                    var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                    ui.SetTimeRemainingInRound(packetData.TimeRemaining);
                });

            _receiver.RegisterPacketHandler<UpdatePropPositionXYFromServerToClientData>(FromServerToClientPackets.UpdatePropPositionXY,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                        propManager.Prop.transform.localPosition = new Vector3(packetData.X, packetData.Y, propManager.Prop.transform.localPosition.z);
                    }
                });

            _receiver.RegisterPacketHandler<UpdatePropPositionZFromServerToClientData>(FromServerToClientPackets.UpdatePropPositionZ,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                        propManager.Prop.transform.localPosition = new Vector3(
                            propManager.Prop.transform.localPosition.x, propManager.Prop.transform.localPosition.y,
                            packetData.Z);
                    }
                });

            _receiver.RegisterPacketHandler<UpdatePropRotationFromServerToClientData>(FromServerToClientPackets.UpdatePropRotation,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                        propManager.Prop.transform.rotation = Quaternion.Euler(0, 0, packetData.Rotation);
                    }
                });

            _receiver.RegisterPacketHandler<UpdatePropScaleFromServerToClientData>(FromServerToClientPackets.UpdatePropScale,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                        propManager.Prop.transform.SetScaleMatching(packetData.Scale);
                    }
                });

            _receiver.RegisterPacketHandler<UpdatePropSpriteFromServerToClientData>(FromServerToClientPackets.UpdatePropSprite,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                        Sprite sprite = null;
                        if (packetData.SpriteBytes != null)
                        {
                            var texture = new Texture2D(2, 2);
                            texture.LoadImage(packetData.SpriteBytes);
                            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 64);
                            sprite.name = packetData.SpriteName;
                        }
                        
                        propManager.SetPropSprite(sprite);
                        var propTransform = propManager.Prop.transform;
                        propTransform.localPosition = new Vector3(packetData.PositionX, packetData.PositionY, packetData.PositionZ);
                        propTransform.localRotation = Quaternion.Euler(0, 0, packetData.RotationZ);
                        propTransform.localScale = Vector3.one * packetData.Scale;
                    }
                });

            clientApi.CommandManager.RegisterCommand(new PropHuntCommand());

            clientApi.ClientManager.ConnectEvent += OnLocalPlayerConnect;
            clientApi.ClientManager.DisconnectEvent += OnLocalPlayerDisconnect;
            
            clientApi.ClientManager.PlayerEnterSceneEvent += OnRemotePlayerEnterScene;

            GameManager.instance.StartCoroutine(AddRemotePropManagerComponentsToPlayerPrefabs());
        }

        private IEnumerator AddRemotePropManagerComponentsToPlayerPrefabs()
        {
            yield return null;
            yield return new WaitUntil(() => Object.FindObjectsOfType<GameObject>(true).Count(go => go.name == "Player Prefab") > 0);

            var playerObjects = Object.FindObjectsOfType<GameObject>(true)
                .Where(gameObject => gameObject.name.Contains("Player Prefab"));
            foreach (var player in playerObjects)
            {
                player.GetOrAddComponent<RemotePropManager>();
            }
        }

        /// <summary>
        /// Called when the local player connects to the server.
        /// </summary>
        private void OnLocalPlayerConnect()
        {
            InitComponents();
        }

        /// <summary>
        /// Called when the local player disconnects from the server.
        /// </summary>
        private void OnLocalPlayerDisconnect()
        {
            HeroController.instance.GetComponent<Hunter>().enabled = false;
            HeroController.instance.GetComponent<LocalPropManager>().enabled = false;

            var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            ui.SetGraceTimeRemaining(0);
            ui.SetTimeRemainingInRound(0);
        }

        /// <summary>
        /// Called when a remote player enters the same scene as the local player.
        /// </summary>
        /// <param name="player">The player that entered the scene</param>
        private void OnRemotePlayerEnterScene(IClientPlayer player)
        {
            var heroPropManager = HeroController.instance.gameObject.GetOrAddComponent<LocalPropManager>();

            if (heroPropManager.PropSprite == null) return;

            var propTransform = heroPropManager.Prop.transform;
            
            BroadcastPropSprite(heroPropManager.PropSprite, propTransform.localPosition, propTransform.localRotation.eulerAngles.z, propTransform.localScale.x);
        }

        /// <summary>
        /// Damage the player if they are a hunter and break a breakable object.
        /// </summary>
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
            PropHunt.Instance.Log("Initializing components");
            var hunter = HeroController.instance.gameObject.GetOrAddComponent<Hunter>();
            hunter.enabled = false;

            var propManager = HeroController.instance.gameObject.GetOrAddComponent<LocalPropManager>();
            propManager.enabled = true;
        }

        /// <summary>
        /// Called when the local player dies.
        /// </summary>
        public static void BroadcastPlayerDeath()
        {
            PropHunt.Instance.Log("Local player has died.");
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPlayerDeath,
                new BroadcastPlayerDeathFromClientToServerData());
        }

        public static void BroadcastPropPositionXY(float x, float y)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropPositionXY,
                new BroadcastPropPositionXYFromClientToServerData
                {
                    X = x,
                    Y = y,
                });
        }

        public static void BroadcastPropPositionZ(float z)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropPositionZ,
                new BroadcastPropPositionZFromClientToServerData
                {
                    Z = z,
                });
        }

        public static void BroadcastPropRotation(float rotation)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropRotation,
                new BroadcastPropRotationFromClientToServerData
                {
                    Rotation = rotation,
                });
        }

        public static void BroadcastPropScale(float scale)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropScale,
                new BroadcastPropScaleFromClientToServerData
                {
                    Scale = scale,
                });
        }

        /// <summary>
        /// Send a packet to the server with the local player's prop's sprite, position, rotation, and scale.
        /// </summary>
        /// <param name="sprite">The sprite to be deconstructed and sent as bytes</param>
        /// <param name="position">The prop's position</param>
        /// <param name="rotationZ">The prop's rotation along the Z Euler axis</param>
        /// <param name="scale">The prop's scale for all 3 axes</param>
        public static void BroadcastPropSprite(Sprite sprite, Vector3 position, float rotationZ, float scale)
        {
            if (sprite == null)
            {
                PropHunt.Instance.Log("Sending empty sprite");
                _sender.SendSingleData(FromClientToServerPackets.BroadcastPropSprite,
                    new BroadcastPropSpriteFromClientToServerData
                    {
                        SpriteName = string.Empty,
                        NumBytes = 0,
                        SpriteBytes = null,
                        PositionX = position.x, 
                        PositionY = position.y,
                        PositionZ = position.z,
                        RotationZ = rotationZ,
                        Scale = scale,
                    });
                return;
            }
            
            var texture = SpriteUtils.ExtractTextureFromSprite(sprite);
            var bytes = texture.EncodeToPNG();
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropSprite,
                new BroadcastPropSpriteFromClientToServerData
                {
                    SpriteName = sprite.name,
                    NumBytes = bytes.Length,
                    SpriteBytes = bytes,
                    PositionX = position.x,
                    PositionY = position.y,
                    PositionZ = position.z,
                    RotationZ = rotationZ,
                    Scale = scale,
                });
        }

        public static void EndRound(bool huntersWin)
        {
            _sender.SendSingleData(FromClientToServerPackets.EndRound,
                new EndRoundFromClientToServerData
                {
                    HuntersWin = huntersWin,
                });
        }

        public static void StartRound(byte graceTime, ushort roundTime)
        {
            _sender.SendSingleData(FromClientToServerPackets.StartRound,
                new StartRoundFromClientToServerData
                {
                    GraceTime = graceTime,
                    RoundTime = roundTime,
                });
        }
    }
}