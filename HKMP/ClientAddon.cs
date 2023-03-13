using System.Collections;
using System.Linq;
using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Game;
using HkmpPouch;
using Modding;
using Modding.Utils;
using PropHunt.Behaviors;
using PropHunt.Events;
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

        private PipeClient _pipe;
        public static PropHuntClientAddon Instance { get; private set; }

        private static IClientAddonNetworkSender<FromClientToServerPackets> _sender;
        private static IClientAddonNetworkReceiver<FromServerToClientPackets> _receiver;

        public override void Initialize(IClientApi clientApi)
        {
            Instance = this;
            _pipe = PropHunt.PipeClient;

            _pipe.On(EndRoundEventFactory.Instance).Do<EndRoundEvent>(pipeEvent =>
            {
                RoundEndHandler(pipeEvent.HuntersWin);
            });

            _pipe.On(PlayerDeathEventFactory.Instance).Do<PlayerDeathEvent>(pipeEvent =>
            {
                OnRemotePlayerDeath(pipeEvent.PlayerId, pipeEvent.HuntersRemaining, pipeEvent.HuntersTotal, pipeEvent.PropsRemaining, pipeEvent.PropsTotal);
            });

            _pipe.On(AssignTeamEventFactory.Instance).Do<AssignTeamEvent>(pipeEvent =>
            {
                AssignTeam(pipeEvent.IsHunter, pipeEvent.InGrace);
            });

            _pipe.On(UpdateGraceTimeEventFactory.Instance).Do<UpdateGraceTimeEvent>(pipeEvent =>
            {
                UpdateGraceTime(pipeEvent.TimeRemaining);
            });

            _pipe.On(UpdateRoundTimeEventFactory.Instance).Do<UpdateRoundTimeEvent>(pipeEvent =>
            {
                UpdateRoundTime(pipeEvent.TimeRemaining);
            });

            _pipe.On(UpdatePropPositionXYEventFactory.Instance).Do<UpdatePropPositionXYEvent>(pipeEvent =>
            {
                UpdateRemotePlayerPropPositionXY(pipeEvent.FromPlayer, pipeEvent.X, pipeEvent.Y);
            });

            _pipe.On(UpdatePropPositionZEventFactory.Instance).Do<UpdatePropPositionZEvent>(pipeEvent =>
            {
                UpdateRemotePlayerPropPositionZ(pipeEvent.FromPlayer, pipeEvent.Z);
            });

            _pipe.On(UpdatePropRotationEventFactory.Instance).Do<UpdatePropRotationEvent>(pipeEvent =>
            {
                UpdateRemotePlayerPropRotation(pipeEvent.FromPlayer, pipeEvent.Rotation);
            });

            _pipe.On(UpdatePropScaleEventFactory.Instance).Do<UpdatePropScaleEvent>(pipeEvent =>
            {
                UpdateRemotePlayerPropScale(pipeEvent.FromPlayer, pipeEvent.Scale);
            });

            // Use lower level sender and receiver for better network performance
            _sender = clientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(Instance);
            _receiver = clientApi.NetClient.GetNetworkReceiver<FromServerToClientPackets>(Instance, clientPacket =>
            {
                return clientPacket switch
                {
                    FromServerToClientPackets.UpdatePropSprite => new PropSpriteFromServerToClientData(),
                    _ => null,
                };
            });

            _receiver.RegisterPacketHandler<PropSpriteFromServerToClientData>(FromServerToClientPackets.UpdatePropSprite,
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
                        propTransform.localPosition = new Vector3(packetData.PositionXY.X, packetData.PositionXY.Y, packetData.PositionZ);
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
        /// Called when the local player dies.
        /// </summary>
        private void OnLocalPlayerDeath()
        {
            PropHunt.Instance.Log("Local player has died.");
            _pipe.SendToServer(new PlayerDeathEvent());
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

            //PropHunt.Instance.Log($"Informing player {player.Id} that prop sprite is: {heroPropManager.PropSprite?.name}");

            SendPropSpritePacket(heroPropManager.PropSprite, propTransform.localPosition, propTransform.localRotation.eulerAngles.z, propTransform.localScale.x);
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
        /// Handle when a round ends.
        /// </summary>
        /// <param name="huntersWin">Whether the Hunters team won</param>
        private void RoundEndHandler(bool huntersWin)
        {
            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            var hunter = HeroController.instance.GetComponent<Hunter>();

            ModHooks.BeforePlayerDeadHook -= OnLocalPlayerDeath;

            var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
            var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
            blankerCtrl.SendEvent("FADE OUT INSTANT");

            PlayerData.instance.isInvincible = true;

            _pipe.ClientApi.ClientManager.ChangeTeam(Team.None);

            hunter.enabled = false;
            propManager.enabled = true;
            var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            ui.SetTimeRemainingInRound(0);
            ui.SetGraceTimeRemaining(0);

            On.Breakable.Break -= OnBreakableBreak;

            InitComponents();

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

        /// <summary>
        /// Handle when a remote player dies.
        /// </summary>
        private void OnRemotePlayerDeath(ushort playerId, ushort huntersRemaining, ushort huntersTotal, ushort propsRemaining, ushort propsTotal)
        {
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                string text = $"Player {player.Username} has died!" +
                              $"\nProps remaining: {propsRemaining}/{propsTotal}" +
                              $"\nHunters remaining: {huntersRemaining}/{huntersTotal}";

                PropHunt.Instance.Log(text);
                var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
                ui.SetPropHuntMessage(text);
            }
        }

        /// <summary>
        /// Called when the local player is assigned to a team.
        /// </summary>
        /// <param name="isHunter">Whether the player is a hunter</param>
        /// <param name="inGrace">Whether the round is still in its grace period</param>
        private void AssignTeam(bool isHunter, bool inGrace)
        {
            ModHooks.BeforePlayerDeadHook += OnLocalPlayerDeath;

            var propManager = HeroController.instance.GetComponent<LocalPropManager>();
            var hunter = HeroController.instance.GetComponent<Hunter>();
            var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            PlayerData.instance.isInvincible = false;

            if (isHunter)
            {
                _pipe.ClientApi.ClientManager.ChangeTeam(Team.Grimm);

                propManager.enabled = false;
                hunter.enabled = true;

                if (inGrace)
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
                _pipe.ClientApi.ClientManager.ChangeTeam(Team.Moss);

                hunter.enabled = false;
                propManager.enabled = true;

                PropHunt.Instance.Log("You are a PROP");
                ui.SetPropHuntMessage("You are a prop!");
            }

            USceneManager.LoadScene(USceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        /// <summary>
        /// Update the remaining grace time.
        /// </summary>
        /// <param name="timeRemaining">The remaining grace time</param>
        private void UpdateGraceTime(uint timeRemaining)
        {
            var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            ui.SetGraceTimeRemaining(timeRemaining);
        }

        /// <summary>
        /// Update the remaining round time.
        /// </summary>
        /// <param name="timeRemaining">The remaining round time</param>
        private void UpdateRoundTime(uint timeRemaining)
        {
            var ui = GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            ui.SetTimeRemainingInRound(timeRemaining);
        }

        /// <summary>
        /// Update a remote player's prop's position along the x and y axes.
        /// </summary>
        /// <param name="playerId">The ID of the remote player to update</param>
        /// <param name="x">The new x component of the remote prop's position</param>
        /// <param name="y">The new y component of the remote prop's position</param>
        private void UpdateRemotePlayerPropPositionXY(ushort playerId, float x, float y)
        {
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propTransform = player.PlayerObject.transform.Find("Prop");
                var newPos = new Vector3(x, y, propTransform.localPosition.z);
                propTransform.localPosition = newPos;
            }
        }

        /// <summary>
        /// Update a remote player's prop's position along the z axis.
        /// </summary>
        /// <param name="playerId">The ID of the remote player to update</param>
        /// <param name="z">The new z component of the remote prop's position</param>
        private void UpdateRemotePlayerPropPositionZ(ushort playerId, float z)
        {
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propTransform = player.PlayerObject.transform.Find("Prop");
                var newPos = new Vector3(propTransform.localPosition.x, propTransform.localPosition.y, z);
                propTransform.localPosition = newPos;
            }
        }

        /// <summary>
        /// Update a remote player's prop's rotation.
        /// </summary>
        /// <param name="playerId">The ID of the remote player to update</param>
        /// <param name="rotation">The new remote prop's rotation</param>
        private void UpdateRemotePlayerPropRotation(ushort playerId, float rotation)
        {
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propTransform = player.PlayerObject.transform.Find("Prop");
                var newRot = Quaternion.Euler(0, 0, rotation);
                propTransform.localRotation = newRot;
            }
        }

        /// <summary>
        /// Update a remote player's prop's scale.
        /// </summary>
        /// <param name="playerId">The ID of the remote player to update</param>
        /// <param name="scale">The new remote prop's scale</param>
        private void UpdateRemotePlayerPropScale(ushort playerId, float scale)
        {
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propTransform = player.PlayerObject.transform.Find("Prop");
                var newScale = Vector3.one * scale;
                propTransform.localScale = newScale;
            }
        }
        
        /// <summary>
        /// Send a packet to the server with the local player's prop's sprite, position, rotation, and scale.
        /// </summary>
        /// <param name="sprite">The sprite to be deconstructed and sent as bytes</param>
        /// <param name="position">The prop's position</param>
        /// <param name="rotationZ">The prop's rotation along the Z Euler axis</param>
        /// <param name="scale">The prop's scale for all 3 axes</param>
        public static void SendPropSpritePacket(Sprite sprite, Vector3 position, float rotationZ, float scale)
        {
            if (sprite == null)
            {
                _sender.SendSingleData(FromClientToServerPackets.BroadcastPropSprite, new PropSpriteFromClientToServerData
                {
                    SpriteName = string.Empty,
                    NumBytes = 0,
                    SpriteBytes = null,
                });
                return;
            }


            var texture = SpriteUtils.ExtractTextureFromSprite(sprite);
            var bytes = texture.EncodeToPNG();
            _sender.SendSingleData(FromClientToServerPackets.BroadcastPropSprite, new PropSpriteFromClientToServerData
            {
                SpriteName = sprite.name,
                NumBytes = bytes.Length,
                SpriteBytes = bytes,
                PositionXY = new Hkmp.Math.Vector2(position.x, position.y),
                PositionZ = position.z,
                RotationZ = rotationZ,
                Scale = scale,
            });
        }
    }
}