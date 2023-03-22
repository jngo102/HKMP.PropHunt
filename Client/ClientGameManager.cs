using GlobalEnums;
using Hkmp.Api.Client;
using PropHunt.HKMP;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using ILogger = Hkmp.Logging.ILogger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages the client side game state.
    /// </summary>
    internal class ClientGameManager
    {
        /// <summary>
        /// The minimum number of players required before a round may start.
        /// </summary>
        private const ushort MinimumPlayers = 2;

        /// <summary>
        /// The client API instance.
        /// </summary>
        private static IClientApi _clientApi;
        
        /// <summary>
        /// An instance of the client network manager.
        /// </summary>
        private readonly ClientNetManager _netManager;

        /// <summary>
        /// A logger for the client game manager.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// An instance of the save manager.
        /// </summary>
        private SaveManager _saveManager;

        /// <summary>
        /// Constructor for the client game manager.
        /// </summary>
        /// <param name="addon">The client add-on instance.</param>
        /// <param name="clientApi">The client API instance.</param>
        public ClientGameManager(PropHuntClientAddon addon, IClientApi clientApi)
        {
            _logger = addon.Logger;
            _clientApi = clientApi;
            _netManager = new ClientNetManager(addon, clientApi.NetClient);
            _saveManager = new SaveManager(_logger);
        }

        /// <summary>
        /// Initialize the client game manager.
        /// </summary>
        public void Initialize()
        {
            ComponentManager.Initialize(_clientApi);
            IconManager.Initialize();
            TextManager.Initialize();

            _clientApi.UiManager.DisableSkinSelection();
            _clientApi.UiManager.DisableTeamSelection();

            _saveManager.Initialize();

            _netManager.AssignTeamEvent += packetData => ComponentManager.AssignTeam(packetData.IsHunter, packetData.InGrace);
            _netManager.EndRoundEvent += packetData => ComponentManager.EndRound(packetData.HuntersWin);
            _netManager.HunterDeathEvent += packetData => OnHunterDeath(packetData.PlayerId, packetData.ConvoNum);
            _netManager.PropDeathEvent += packetData =>
                OnPropDeath(packetData.PlayerId, packetData.PropsRemaining, packetData.PropsTotal);
            _netManager.PlayerLeftRoundEvent += packetData =>
                OnPlayerLeftRound(packetData.PlayerId, packetData.PropsRemaining, packetData.PropsTotal);
            _netManager.UpdateGraceTimerEvent += packetData => OnUpdateGraceTimer(packetData.TimeRemaining);
            _netManager.UpdateRoundTimerEvent += packetData => OnUpdateRoundTimer(packetData.TimeRemaining);
            _netManager.UpdateRoundOverTimerEvent += packetData => OnUpdateRoundOverTimer(packetData.TimeRemaining);
            _netManager.UpdatePropPositionXyEvent += packetData => ComponentManager.UpdateRemotePropPositionXy(packetData.PlayerId, packetData.X, packetData.Y);
            _netManager.UpdatePropPositionZEvent += packetData => ComponentManager.UpdateRemotePropPositionZ(packetData.PlayerId, packetData.Z);
            _netManager.UpdatePropRotationEvent += packetData => ComponentManager.UpdateRemotePropRotation(packetData.PlayerId, packetData.Rotation);
            _netManager.UpdatePropScaleEvent += packetData => ComponentManager.UpdateRemotePropScale(packetData.PlayerId, packetData.Scale);
            _netManager.UpdatePropSpriteEvent += packetData => ComponentManager.UpdateRemotePropSprite(packetData.PlayerId,
                packetData.SpriteName, packetData.SpriteBytes, packetData.PositionX, packetData.PositionY,
                packetData.PositionZ, packetData.RotationZ, packetData.Scale);

            _clientApi.ClientManager.ConnectEvent += ComponentManager.OnLocalPlayerConnect;
            _clientApi.ClientManager.DisconnectEvent += ComponentManager.OnLocalPlayerDisconnect;
            _clientApi.ClientManager.PlayerEnterSceneEvent += ComponentManager.OnRemotePlayerEnterScene;

            _clientApi.CommandManager.RegisterCommand(new PropHuntCommand());
        }

        /// <summary>
        /// Change the HKMP chat box's type.
        /// </summary>
        /// <param name="type">The type to change the HKMP chat box to.</param>
        /// <returns>The changed HKMP chat box.</returns>
        public static object ChangeHkmpChatBoxType(Type type)
        {
            return Convert.ChangeType(_clientApi.UiManager.ChatBox, type);
        }

        /// <summary>
        /// Start a new round.
        /// </summary>
        /// <param name="graceTime">The amount of initial grace time in seconds.</param>
        /// <param name="roundTime">The amount of time in the round in seconds.</param>
        public static void StartRound(byte graceTime, ushort roundTime)
        {
            var numPlayers = _clientApi.ClientManager.Players.Count;
            if (numPlayers < MinimumPlayers - 1)
            {
                TextManager.DisplayDreamMessage($"Not enough players to start a round! {MinimumPlayers} required, {numPlayers + 1} connected.");
                return;
            }

            ClientNetManager.SendPacket(
                FromClientToServerPackets.StartRound,
                new StartRoundFromClientToServerData
                {
                    GraceTime = graceTime,
                    RoundTime = roundTime,
                });
        }

        /// <summary>
        /// End the current round.
        /// </summary>
        public static void EndRound()
        {
            ClientNetManager.SendPacket(
                FromClientToServerPackets.EndRound,
                new EndRoundFromClientToServerData());
        }

        /// <summary>
        /// Set whether rounds will automatically start after a round ends.
        /// </summary>
        /// <param name="graceTime">The amount of initial grace time in seconds that the round automatically starts with.</param>
        /// <param name="roundTime">The amount of time in seconds in an automated round.</param>
        /// <param name="secondsBetweenRounds">The amount of time in seconds between automated rounds.</param>
        public static void ToggleAutomation(byte graceTime, ushort roundTime, ushort secondsBetweenRounds)
        {
            ClientNetManager.SendPacket(
                FromClientToServerPackets.ToggleAutomation,
                new ToggleAutomationFromClientToServerData
                {
                    GraceTime = graceTime,
                    RoundTime = roundTime,
                    SecondsBetweenRounds = secondsBetweenRounds,
                });
        }

        /// <summary>
        /// Called when a hunter dies.
        /// </summary>
        /// <param name="playerId">The ID of the player who died as a hunter.</param>
        /// <param name="convoNum">The convo number sent from the server.</param>
        private void OnHunterDeath(ushort playerId, byte convoNum)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                string text = $"Hunter {player.Username} has died!";
                TextManager.ShowHunterDeathMessage(player.Username, convoNum);
                _logger.Info(text);
            }
        }

        /// <summary>
        /// Called when a prop dies.
        /// </summary>
        /// <param name="playerId">The ID of the player who died as a prop.</param>
        /// <param name="propsRemaining">The number of props remaining in the round.</param>
        /// <param name="propsTotal">The total number of props that were in the round.</param>
        private void OnPropDeath(ushort playerId, ushort propsRemaining, ushort propsTotal)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                string text = $"Player {player.Username} has died!" +
                              $"\nProps remaining: {propsRemaining}/{propsTotal}";
                TextManager.DisplayDreamMessage(text);
                _logger.Info(text);
            }
        }

        /// <summary>
        /// Called when a player leaves the round.
        /// </summary>
        /// <param name="playerId">The ID of the player who left.</param>
        /// <param name="propsRemaining">The number of props remaining in the round.</param>
        /// <param name="propsTotal">The total number of props that were in the round.</param>
        private void OnPlayerLeftRound(ushort playerId, ushort propsRemaining, ushort propsTotal)
        {
            if (_clientApi.ClientManager.TryGetPlayer(playerId, out var player))
            {
                
                string text = $"Player {player.Username} has left the server!" +
                              $"\nProps remaining: {propsRemaining}/{propsTotal}";

                _logger.Info(text);
                TextManager.DisplayDreamMessage(text);
            }
        }

        /// <summary>
        /// Called when the grace timer is updated.
        /// </summary>
        /// <param name="timeRemaining">The amount of time remaining in seconds.</param>
        private void OnUpdateGraceTimer(byte timeRemaining)
        {
            TextManager.SetRemainingGraceTime(timeRemaining);
        }

        /// <summary>
        /// Called when the round timer is updated.
        /// </summary>
        /// <param name="timeRemaining">The amount of time remaining in seconds.</param>
        private void OnUpdateRoundTimer(ushort timeRemaining)
        {
            TextManager.SetRemainingRoundTime(timeRemaining);
        }

        /// <summary>
        /// Called when the round over timer is updated.
        /// </summary>
        /// <param name="timeRemaining">The amount of time remaining in seconds.</param>
        private void OnUpdateRoundOverTimer(ushort timeRemaining)
        {
            TextManager.SetRemainingRoundOverTime(timeRemaining);
        }
    }
}
