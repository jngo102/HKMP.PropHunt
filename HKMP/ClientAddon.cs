using System;
using System.Collections.Generic;
using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game;
using HkmpPouch;
using Modding;
using Modding.Utils;
using PropHunt.Behaviors;
using PropHunt.Events;
using PropHunt.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            _pipe.On(UpdatePropSpriteEventFactory.Instance).Do<UpdatePropSpriteEvent>(pipeEvent =>
            {
                UpdateRemotePlayerPropSprite(pipeEvent.FromPlayer, pipeEvent.SpriteName);
            });

            clientApi.CommandManager.RegisterCommand(new PropHuntCommand());

            clientApi.ClientManager.ConnectEvent += OnLocalPlayerConnect;
            clientApi.ClientManager.DisconnectEvent += OnLocalPlayerDisconnect;
            
            clientApi.ClientManager.PlayerEnterSceneEvent += OnRemotePlayerEnterScene;

            //var playerObjects = Object.FindObjectsOfType<GameObject>(true)
            //    .Where(gameObject => gameObject.name.Contains("Player Prefab"));
            //foreach (var player in playerObjects)
            //{
            //    player.GetOrAddComponent<RemotePropManager>();
            //}
        }

        /// <summary>
        /// Called when the local player dies.
        /// </summary>
        private void OnLocalPlayerDeath()
        {
            PropHunt.Instance.Log($"Local player has died.");
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

            PropHunt.Instance.Log($"Informing player {player.Id} that prop sprite is: {heroPropManager.PropSprite?.name}");
            
            _pipe.SendToPlayer(player.Id, new UpdatePropSpriteEvent { SpriteName = heroPropManager.PropSprite?.name });
            _pipe.SendToPlayer(player.Id,
                new UpdatePropPositionXYEvent { X = propTransform.position.x, Y = propTransform.position.y });
            _pipe.SendToPlayer(player.Id, new UpdatePropPositionZEvent { Z = propTransform.position.z });
            _pipe.SendToPlayer(player.Id,
                new UpdatePropRotationEvent { Rotation = propTransform.rotation.eulerAngles.z });
            _pipe.SendToPlayer(player.Id, new UpdatePropScaleEvent { Scale = propTransform.localScale.x });
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
        /// Update a remote player's prop's sprite.
        /// </summary>
        /// <param name="playerId">The ID of the remote player to update</param>
        /// <param name="spriteName">The new remote prop's sprite</param>
        private void UpdateRemotePlayerPropSprite(ushort playerId, string spriteName)
        {
            PropHunt.Instance.Log($"Updating player {playerId}'s prop sprite to: {spriteName}");
            if (_pipe.ClientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propSprite = string.IsNullOrEmpty(spriteName)
                    ? null
                    : Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(sprite => sprite.name == spriteName);
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                propManager.SetPropSprite(propSprite);
            }
        }
    }
}