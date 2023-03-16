using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Game;
using Hkmp.Util;
using Modding;
using Modding.Utils;
using PropHunt.Behaviors;
using PropHunt.UI;
using System.Collections;
using System.Linq;
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

        private string _deathUsername;
        private ushort _propsRemaining;
        private ushort _propsTotal;

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
                    FromServerToClientPackets.HunterDeath => new HunterDeathFromServerToClientData(),
                    FromServerToClientPackets.PropDeath => new PropDeathFromServerToClientData(),
                    FromServerToClientPackets.PlayerLeftGame => new PlayerLeftGameFromServerToClientData(),
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
                    IEnumerator HazardRespawnThenAssignTeam()
                    {
                        ModHooks.BeforePlayerDeadHook += BroadcastPlayerDeath;
                        USceneManager.activeSceneChanged -= OnSceneChange;
                        USceneManager.activeSceneChanged += OnSceneChange;

                        var propManager = HeroController.instance.GetComponent<LocalPropManager>();
                        var hunter = HeroController.instance.GetComponent<Hunter>();
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        PlayerData.instance.isInvincible = false;

                        GameManager.instance.LoadScene(USceneManager.GetActiveScene().name);
                        PlayerData.instance.SetHazardRespawn(Object.FindObjectOfType<HazardRespawnMarker>());
                        yield return HeroController.instance.HazardRespawn();

                        if (packetData.IsHunter)
                        {
                            clientApi.ClientManager.ChangeTeam(Team.Grimm);

                            propManager.enabled = false;
                            hunter.enabled = true;

                            if (packetData.InGrace)
                            {
                                hunter.BeginGracePeriod();
                            }

                            Logger.Info("You are a HUNTER");
                            ui.SetPropHuntMessage("TEAM_HUNTER");

                            HeroController.instance.SetMPCharge(198);
                            GameManager.instance.soulOrb_fsm.SendEvent("MP GAIN");

                            On.Breakable.Break += OnBreakableBreak;
                        }
                        else
                        {
                            clientApi.ClientManager.ChangeTeam(Team.Moss);

                            hunter.enabled = false;
                            propManager.enabled = true;

                            propManager.ClearProp();

                            Logger.Info("You are a PROP");
                            ui.SetPropHuntMessage("TEAM_PROP");
                        }
                    }

                    GameManager.instance.StartCoroutine(HazardRespawnThenAssignTeam());
                });

            _receiver.RegisterPacketHandler<EndRoundFromServerToClientData>(FromServerToClientPackets.EndRound,
                packetData =>
                {
                    IEnumerator DelayResetTeam(float waitTime)
                    {
                        yield return new WaitForSeconds(waitTime);
                        clientApi.ClientManager.ChangeTeam(Team.None);
                    }

                    var hc = HeroController.instance;
                    var propManager = hc.GetComponent<LocalPropManager>();
                    var hunter = hc.GetComponent<Hunter>();
                    hc.GetComponent<tk2dSpriteAnimator>().Play("Idle");

                    ModHooks.BeforePlayerDeadHook -= BroadcastPlayerDeath;
                    On.Breakable.Break -= OnBreakableBreak;
                    USceneManager.activeSceneChanged -= OnSceneChange;

                    var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                    var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
                    blankerCtrl.SendEvent("FADE OUT INSTANT");

                    PlayerData.instance.isInvincible = true;
                    
                    // In case the change team packet arrives after the end round packet, e.g.
                    // when multiple prop players are killed at once.
                    GameManager.instance.StartCoroutine(DelayResetTeam(1));

                    hunter.enabled = false;
                    propManager.enabled = true;
                    var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                    ui.SetTimeRemainingInRound(0);
                    ui.SetGraceTimeRemaining(0);

                    InitComponents();

                    if (packetData.HuntersWin)
                    {
                        Logger.Info("HUNTERS WIN");
                        ui.SetPropHuntMessage("HUNTERS_WIN");
                    }
                    else
                    {
                        Logger.Info("PROPS WIN");
                        ui.SetPropHuntMessage("PROPS_WIN");
                    }
                });

            _receiver.RegisterPacketHandler<HunterDeathFromServerToClientData>(FromServerToClientPackets.HunterDeath,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        _deathUsername = player.Username;
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        string text = $"Hunter {player.Username} has died!";
                        ui.SetPropHuntMessage("HUNTER_DEATH_" + packetData.ConvoNum);
                        Logger.Info(text);
                    }
                });

            _receiver.RegisterPacketHandler<PropDeathFromServerToClientData>(FromServerToClientPackets.PropDeath,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        _deathUsername = player.Username;
                        _propsRemaining = packetData.PropsRemaining;
                        _propsTotal = packetData.PropsTotal;
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        string text = $"Player {player.Username} has died!" +
                                      $"\nProps remaining: {_propsRemaining}/{_propsTotal}";
                        ui.SetPropHuntMessage("PLAYER_DEATH");
                        Logger.Info(text);
                    }
                });

            _receiver.RegisterPacketHandler<PlayerLeftGameFromServerToClientData>(FromServerToClientPackets.PlayerLeftGame,
                packetData =>
                {
                    if (clientApi.ClientManager.TryGetPlayer(packetData.PlayerId, out var player))
                    {
                        _deathUsername = player.Username;
                        _propsRemaining = packetData.PropsRemaining;
                        _propsTotal = packetData.PropsTotal;
                        string text = $"Player {_deathUsername} has left the server!" +
                                      $"\nProps remaining: {_propsRemaining}/{_propsTotal}";

                        Logger.Info(text);
                        var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                        ui.SetPropHuntMessage("PLAYER_LEFT");
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

            ModHooks.LanguageGetHook += OnLanguageGet;
        }

        private string OnLanguageGet(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "PLAYER_DEATH":
                    return $"Player {_deathUsername} has died!\nProps remaining: {_propsRemaining}/{_propsTotal}";
                case "PLAYER_LEFT":
                    return $"Player {_deathUsername} has left the server!\nProps remaining: {_propsRemaining}/{_propsTotal}";
                case "TEAM_HUNTER":
                    return "You are a hunter!";
                case "TEAM_PROP":
                    return "You are a prop!";
                case "HUNTERS_WIN":
                    return "Hunters win!";
                case "PROPS_WIN":
                    return "Props win!";
                case "HUNTER_DEATH_0":
                    return $"Hunter {_deathUsername} died! Maybe they should learn the room layout better...";
                case "HUNTER_DEATH_1":
                    return $"Hunter {_deathUsername} foolishly broke too many non-props and perished!";
                case "HUNTER_DEATH_2":
                    return $"Hunter {_deathUsername} couldn't deal with the guilt of breaking so many innocent objects!";
                case "HUNTER_DEATH_3":
                    return $"Hunter {_deathUsername} mistakenly broke their own soul!";
                case "HUNTER_DEATH_4":
                    return $"Hunter {_deathUsername} has been banned from IKEA!";
            }

            return orig;
        }

        private void OnSceneChange(Scene prevScene, Scene nextScene)
        {
            foreach (var fsm in Object.FindObjectsOfType<PlayMakerFSM>())
            {
                if (fsm.gameObject.scene != nextScene)
                {
                    continue;
                }

                // Find "Bench Control" FSMs and disable sitting on them
                if (fsm.Fsm.Name.Equals("Bench Control"))
                {
                    Logger.Info("Found FSM with Bench Control, patching...");

                    fsm.InsertMethod("Pause 2", 1, () => { PlayerData.instance.SetBool("atBench", false); });

                    var checkStartState2 = fsm.GetState("Check Start State 2");
                    var pause2State = fsm.GetState("Pause 2");
                    checkStartState2.GetTransition(1).ToFsmState = pause2State;

                    var checkStartState = fsm.GetState("Check Start State");
                    var idleStartPauseState = fsm.GetState("Idle Start Pause");
                    checkStartState.GetTransition(1).ToFsmState = idleStartPauseState;

                    var idleState = fsm.GetState("Idle");
                    idleState.Actions = new[] { idleState.Actions[0] };
                }
            }
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
            Logger.Info("Initializing components");
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
            Instance.Logger.Info("Local player has died.");
            if (HeroController.instance.GetComponent<LocalPropManager>().enabled)
            {
                _sender.SendSingleData(FromClientToServerPackets.BroadcastPropDeath,
                    new BroadcastPropDeathFromClientToServerData());
            }
            else if (HeroController.instance.GetComponent<Hunter>().enabled)
            {
                _sender.SendSingleData(FromClientToServerPackets.BroadcastHunterDeath,
                    new BroadcastHunterDeathFromClientToServerData());
            }
        }

        /// <summary>
        /// Broadcast the local player's prop's position along the x- and y-axes.
        /// </summary>
        /// <param name="x">The x component of the prop's position</param>
        /// <param name="y">The y component of the prop's position</param>
        public static void BroadcastPropPositionXY(float x, float y)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropPositionXY,
                new BroadcastPropPositionXYFromClientToServerData
                {
                    X = x,
                    Y = y,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's position along the z-axis.
        /// </summary>
        /// <param name="z">The z component of the prop's position</param>
        public static void BroadcastPropPositionZ(float z)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropPositionZ,
                new BroadcastPropPositionZFromClientToServerData
                {
                    Z = z,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's rotation.
        /// </summary>
        /// <param name="rotation">The prop's rotation</param>
        public static void BroadcastPropRotation(float rotation)
        {
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropRotation,
                new BroadcastPropRotationFromClientToServerData
                {
                    Rotation = rotation,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's scale.
        /// </summary>
        /// <param name="scale">The prop's scale</param>
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
                Instance.Logger.Info("Sending empty sprite");
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
            
            var texture = Satchel.SpriteUtils.ExtractTextureFromSprite(sprite);
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

        /// <summary>
        /// End a round.
        /// </summary>
        /// <param name="huntersWin">Whether the hunters won the round</param>
        public static void EndRound(bool huntersWin)
        {
            _sender.SendSingleData(FromClientToServerPackets.EndRound,
                new EndRoundFromClientToServerData
                {
                    HuntersWin = huntersWin,
                });
        }

        /// <summary>
        /// Begin a round.
        /// </summary>
        /// <param name="graceTime">The duration of the initial grace period in seconds</param>
        /// <param name="roundTime">The duration of the round</param>
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