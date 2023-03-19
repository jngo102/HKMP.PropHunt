using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game;
using Modding;
using Modding.Utils;
using PropHunt.Client.Behaviors;
using PropHunt.HKMP;
using System.Collections;
using System.Linq;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages components related to the prop hunt game mode.
    /// </summary>
    internal static class ComponentManager
    {
        /// <summary>
        /// The client API instance.
        /// </summary>
        private static IClientApi _clientApi;

        /// <summary>
        /// Initialize the component manager.
        /// </summary>
        public static void Initialize(IClientApi clientApi)
        {
            _clientApi = clientApi;

            AddRemotePropManagerComponentsToPlayerPrefabs();
        }

        /// <summary>
        /// Called when the local player dies.
        /// </summary>
        public static void BroadcastPlayerDeath()
        {
            if (HeroController.instance.GetComponent<LocalPropManager>().enabled)
            {
                ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropDeath,
                    new BroadcastPropDeathFromClientToServerData());
            }
            else if (HeroController.instance.GetComponent<Hunter>().enabled)
            {
                ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastHunterDeath,
                    new BroadcastHunterDeathFromClientToServerData());
            }
        }

        /// <summary>
        /// Broadcast the local player's prop's position along the x- and y-axes.
        /// </summary>
        /// <param name="x">The x component of the prop's position.</param>
        /// <param name="y">The y component of the prop's position.</param>
        public static void BroadcastPropPositionXy(float x, float y)
        {
            ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropPositionXy,
                new BroadcastPropPositionXyFromClientToServerData
                {
                    X = x,
                    Y = y,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's position along the z-axis.
        /// </summary>
        /// <param name="z">The z component of the prop's position.</param>
        public static void BroadcastPropPositionZ(float z)
        {
            ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropPositionZ,
                new BroadcastPropPositionZFromClientToServerData
                {
                    Z = z,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's rotation.
        /// </summary>
        /// <param name="rotation">The prop's rotation.</param>
        public static void BroadcastPropRotation(float rotation)
        {
            ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropRotation,
                new BroadcastPropRotationFromClientToServerData
                {
                    Rotation = rotation,
                });
        }

        /// <summary>
        /// Broadcast the local player's prop's scale.
        /// </summary>
        /// <param name="scale">The prop's scale.</param>
        public static void BroadcastPropScale(float scale)
        {
            ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropScale,
                new BroadcastPropScaleFromClientToServerData
                {
                    Scale = scale,
                });
        }

        /// <summary>
        /// Send a packet to the server with the local player's prop's sprite, position, rotation, and scale.
        /// </summary>
        /// <param name="sprite">The sprite to be deconstructed and sent as bytes.</param>
        /// <param name="position">The prop's position.</param>
        /// <param name="rotationZ">The prop's rotation along the Z Euler axis.</param>
        /// <param name="scale">The prop's scale for all 3 axes.</param>
        public static void BroadcastPropSprite(Sprite sprite, Vector3 position, float rotationZ, float scale)
        {
            if (sprite == null)
            {
                ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropSprite,
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
            
            ClientNetManager.SendPacket(FromClientToServerPackets.BroadcastPropSprite,
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
        /// Wait until player prefabs have been instantiated, then add the RemotePropManager component to them.
        /// </summary>
        private static void AddRemotePropManagerComponentsToPlayerPrefabs()
        {
            IEnumerator WaitAndAdd()
            {
                yield return null;
                yield return new WaitUntil(() => UnityEngine.Object.FindObjectsOfType<GameObject>(true).Count(go => go.name == "Player Prefab") > 0);

                var playerObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                    .Where(gameObject => gameObject.name.Contains("Player Prefab"));
                foreach (var player in playerObjects)
                {
                    player.GetOrAddComponent<RemotePropManager>();
                }
            }

            GameManager.instance.StartCoroutine(WaitAndAdd());
        }

        /// <summary>
        /// Called when the local player connects to the server.
        /// </summary>
        public static void OnLocalPlayerConnect()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Initialize components on the local player object.
        /// </summary>
        private static void InitializeComponents()
        {
            var hunter = HeroController.instance.gameObject.GetOrAddComponent<Hunter>();
            hunter.enabled = false;

            var propManager = HeroController.instance.gameObject.GetOrAddComponent<LocalPropManager>();
            propManager.enabled = true;
        }

        /// <summary>
        /// Called when the local player disconnects from the server.
        /// </summary>
        public static void OnLocalPlayerDisconnect()
        {
            HeroController.instance.GetComponent<Hunter>().enabled = false;
            HeroController.instance.GetComponent<LocalPropManager>().enabled = false;

            TextManager.SetRemainingGraceTime(0);
            TextManager.SetRemainingRoundTime(0);
        }

        /// <summary>
        /// Called when a remote player enters the same scene as the local player.
        /// </summary>
        /// <param name="player">The player that entered the scene.</param>
        public static void OnRemotePlayerEnterScene(IClientPlayer player)
        {
            var heroPropManager = HeroController.instance.gameObject.GetOrAddComponent<LocalPropManager>();

            if (heroPropManager.PropSprite == null) return;

            var propTransform = heroPropManager.Prop.transform;

            BroadcastPropSprite(heroPropManager.PropSprite, propTransform.localPosition, propTransform.localRotation.eulerAngles.z, propTransform.localScale.x);
        }

        /// <summary>
        /// Called when the player is assigned a team.
        /// </summary>
        /// <param name="isHunter">Whether the player was assigned to the hunters team.</param>
        /// <param name="inGrace">Whether the round is still in its grace period.</param>
        public static void AssignTeam(bool isHunter, bool inGrace)
        {
            IEnumerator HazardRespawnThenAssignTeam()
            {
                ModHooks.BeforePlayerDeadHook += BroadcastPlayerDeath;
                USceneManager.activeSceneChanged -= PatchManager.OnSceneChange;
                USceneManager.activeSceneChanged += PatchManager.OnSceneChange;

                var propManager = HeroController.instance.GetComponent<LocalPropManager>();
                var hunter = HeroController.instance.GetComponent<Hunter>();
                PlayerData.instance.isInvincible = false;

                GameManager.instance.LoadScene(USceneManager.GetActiveScene().name);
                PlayerData.instance.SetHazardRespawn(Object.FindObjectOfType<HazardRespawnMarker>());
                yield return HeroController.instance.HazardRespawn();

                if (isHunter)
                {
                    _clientApi.ClientManager.ChangeTeam(Team.Grimm);

                    propManager.enabled = false;
                    hunter.enabled = true;

                    if (inGrace)
                    {
                        hunter.BeginGracePeriod();
                    }
                    
                    TextManager.DisplayDreamMessage("You are a hunter!");

                    HeroController.instance.SetMPCharge(198);
                    GameManager.instance.soulOrb_fsm.SendEvent("MP GAIN");

                    On.Breakable.Break += OnBreakableBreak;
                }
                else
                {
                    _clientApi.ClientManager.ChangeTeam(Team.Moss);

                    hunter.enabled = false;
                    propManager.enabled = true;

                    propManager.ClearProp();
                    
                    TextManager.DisplayDreamMessage("You are a prop!");
                }
            }

            GameManager.instance.StartCoroutine(HazardRespawnThenAssignTeam());
        }

        /// <summary>
        /// Called when the round ends.
        /// </summary>
        /// <param name="huntersWin">Whether the hunters team won the round.</param>
        public static void EndRound(bool huntersWin)
        {
            IEnumerator DelayResetTeam(float waitTime)
            {
                yield return new WaitForSeconds(waitTime);
                _clientApi.ClientManager.ChangeTeam(Team.None);
            }

            var hc = HeroController.instance;
            var propManager = hc.GetComponent<LocalPropManager>();
            var hunter = hc.GetComponent<Hunter>();
            hc.GetComponent<HeroAnimationController>().PlayIdle();

            ModHooks.BeforePlayerDeadHook -= BroadcastPlayerDeath;
            On.Breakable.Break -= OnBreakableBreak;
            USceneManager.activeSceneChanged -= PatchManager.OnSceneChange;

            var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
            var blankerCtrl = blanker.LocateMyFSM("Blanker Control");
            blankerCtrl.SendEvent("FADE OUT INSTANT");

            PlayerData.instance.isInvincible = true;

            // In case the change team packet arrives after the end round packet, e.g.
            // when multiple prop players are killed at once.
            GameManager.instance.StartCoroutine(DelayResetTeam(1));

            hunter.enabled = false;
            propManager.enabled = true;
            TextManager.SetRemainingGraceTime(0);
            TextManager.SetRemainingRoundTime(0);            
            TextManager.DisplayDreamMessage($"{(huntersWin ? "Hunters" : "Props")} win!");
        }

        /// <summary>
        /// Called when a prop's position is updated along the x- and y-axes.
        /// </summary>
        /// <param name="playerId">The ID of the player whose prop was translated.</param>
        /// <param name="x">The x-coordinate of the new prop position.</param>
        /// <param name="y">The x-coordinate of the new prop position.</param>
        public static void UpdateRemotePropPositionXy(ushort playerId, float x, float y)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                propManager.Prop.transform.localPosition =
                    new Vector3(x, y, propManager.Prop.transform.localPosition.z);
            }
        }

        /// <summary>
        /// Called when a prop's position is updated along the z-axis.
        /// </summary>
        /// <param name="playerId">The ID of the player whose prop was translated.</param>
        /// <param name="z">The z-coordinate of the new prop position.</param>
        public static void UpdateRemotePropPositionZ(ushort playerId, float z)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                propManager.Prop.transform.localPosition = new Vector3(propManager.Prop.transform.localPosition.x,
                    propManager.Prop.transform.localPosition.y, z);
            }
        }

        /// <summary>
        /// Called when a prop's rotation is updated.
        /// </summary>
        /// <param name="playerId">The ID of the player whose prop was rotated.</param>
        /// <param name="rotation">The new prop rotation.</param>
        public static void UpdateRemotePropRotation(ushort playerId, float rotation)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                propManager.Prop.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            }
        }

        /// <summary>
        /// Called when a prop's scale is updated.
        /// </summary>
        /// <param name="playerId">The ID of the player whose prop was scaled.</param>
        /// <param name="scale">The new prop scale.</param>
        public static void UpdateRemotePropScale(ushort playerId, float scale)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                propManager.Prop.transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        /// <summary>
        /// Called when a prop's sprite is updated.
        /// </summary>
        /// <param name="playerId">The ID of the player whose prop sprite was changed..</param>
        /// <param name="spriteName">The name of the new sprite.</param>
        /// <param name="spriteBytes">The raw bytes of the new sprite.</param>
        /// <param name="positionX">The x-coordinate of the prop's position at the time of the update.</param>
        /// <param name="positionY">The y-coordinate of the prop's position at the time of the update.</param>
        /// <param name="positionZ">The z-coordinate of the prop's position at the time of the update.</param>
        /// <param name="rotation">The prop's rotation at the time of the update.</param>
        /// <param name="scale">The prop's scale at the time of the update.</param>
        public static void UpdateRemotePropSprite(ushort playerId, string spriteName, byte[] spriteBytes, float positionX, float positionY, float positionZ, float rotation, float scale)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                var propManager = player.PlayerObject.GetOrAddComponent<RemotePropManager>();
                Sprite sprite = null;
                if (spriteBytes != null)
                {
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(spriteBytes);
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 64);
                    sprite.name = spriteName;
                }

                propManager.SetPropSprite(sprite);
                var propTransform = propManager.Prop.transform;
                propTransform.localPosition = new Vector3(positionX, positionY, positionZ);
                propTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                propTransform.SetScaleMatching(scale);
            }
        }

        /// <summary>
        /// Damage the player if they are a hunter and break a breakable object.
        /// </summary>
        private static void OnBreakableBreak(On.Breakable.orig_Break orig, Breakable self, float flingAngleMin, float flingAngleMax, float impactMultiplier)
        {
            HeroController.instance.TakeDamage(null, CollisionSide.top, 1, (int)HazardType.PIT);

            orig(self, flingAngleMin, flingAngleMax, impactMultiplier);
        }
    }
}
