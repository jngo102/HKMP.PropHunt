using Hkmp.Api.Server;
using Hkmp.Logging;
using PropHunt.HKMP;
using PropHunt.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace PropHunt.Server
{
    /// <summary>
    /// Enumeration for the states that a round can be in.
    /// </summary>
    internal enum GameState
    {
        /// <summary>
        /// Wait time while there are not enough players to start a round.
        /// </summary>
        WaitingForPlayers,
        
        /// <summary>
        /// Wait time before a new round begins.
        /// </summary>
        WaitingForRoundStart,

        /// <summary>
        /// While a round is in progress.
        /// </summary>
        InRound,
    }

    /// <summary>
    /// Manages the server side game state.
    /// </summary>
    internal class ServerGameManager
    {
        /// <summary>
        /// The minimum number of players required before a round may start.
        /// </summary>
        private const ushort MinimumPlayers = 2;

        /// <summary>
        /// A collection of all hunters playing.
        /// </summary>
        private readonly List<IServerPlayer> _allHunters = new();

        /// <summary>
        /// A collection of all props playing.
        /// </summary>
        private readonly List<IServerPlayer> _allProps = new();

        /// <summary>
        /// A collection of all currently alive players on the Hunters team.
        /// </summary>
        private readonly List<IServerPlayer> _livingHunters = new();

        /// <summary>
        /// A collection of all currently alive players on the Props team.
        /// </summary>
        private readonly List<IServerPlayer> _livingProps = new();

        /// <summary>
        /// The total number of hunters playing.
        /// </summary>
        private ushort TotalHunters => (ushort)_allHunters.Count;

        /// <summary>
        /// The total number of props playing.
        /// </summary>
        private ushort TotalProps => (ushort)_allProps.Count;

        /// <summary>
        /// The number of hunters that are alive.
        /// </summary>
        private ushort HuntersAlive => (ushort)_livingHunters.Count;

        /// <summary>
        /// The number of props that are alive.
        /// </summary>
        private ushort PropsAlive => (ushort)_livingProps.Count;

        /// <summary>
        /// Timer that handles the length of time that a round goes for.
        /// </summary>
        private Timer _roundTimer;

        /// <summary>
        /// Timer that handles the length of time that a round is over for if rounds are automated.
        /// </summary>
        private Timer _roundOverTimer;

        /// <summary>
        /// A date-time object that contains the time at which a round will end.
        /// </summary>
        private DateTime _dueTimeRound;

        /// <summary>
        /// A date-time object that contains the time at which the grace period will end.
        /// </summary>
        private DateTime _dueTimeGrace;

        /// <summary>
        /// A date-time object that contains the time at which a new automated round will start.
        /// </summary>
        private DateTime _dueTimeRoundOver;

        /// <summary>
        /// Timer that handles updating every player's timer each second.
        /// </summary>
        private Timer _intervalTimer;

        /// <summary>
        /// The server API instance.
        /// </summary>
        private IServerApi _serverApi;

        /// <summary>
        /// An instance of the server network manager.
        /// </summary>
        private readonly ServerNetManager _netManager;

        /// <summary>
        /// The current game state.
        /// </summary>
        private GameState _gameState;

        /// <summary>
        /// A logger for the server game manager.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The instance of random used to identify props and hunters.
        /// </summary>
        private Random _random;

        /// <summary>
        /// An instance of server settings.
        /// </summary>
        private static ServerSettings _settings;

        /// <summary>
        /// Constructor for the server game manager.
        /// </summary>
        /// <param name="addon">The server add-on instance.</param>
        /// <param name="serverApi">The server API instance.</param>
        public ServerGameManager(PropHuntServerAddon addon, IServerApi serverApi)
        {
            _logger = addon.Logger;
            _serverApi = serverApi;

            _settings = ServerSettings.LoadFromFile();

            _random = new Random();

            _netManager = new ServerNetManager(addon, serverApi.NetServer);
        }

        /// <summary>
        /// Initialize the server game manager.
        /// </summary>
        public void Initialize()
        {
            _gameState = GameState.WaitingForPlayers;

            _netManager.HunterDeathEvent += OnHunterDeath;
            _netManager.PropDeathEvent += OnPropDeath;
            _netManager.UpdatePropPositionXyEvent += (playerId, packetData) => OnUpdatePropPositionXy(playerId, packetData.X, packetData.Y);
            _netManager.UpdatePropPositionZEvent += (playerId, packetData) => OnUpdatePropPositionZ(playerId, packetData.Z);
            _netManager.UpdatePropRotationEvent += (playerId, packetData) => OnUpdatePropRotation(playerId, packetData.Rotation);
            _netManager.UpdatePropScaleEvent += (playerId, packetData) => OnUpdatePropScale(playerId, packetData.Scale);
            _netManager.UpdatePropSpriteEvent += (playerId, packetData) => OnUpdatePropSprite(playerId,
                packetData.SpriteName, packetData.NumBytes, packetData.SpriteBytes, packetData.PositionX,
                packetData.PositionY, packetData.PositionZ, packetData.RotationZ, packetData.Scale);
            _netManager.StartRoundEvent += packetData =>
                StartRound((ushort)(_serverApi.ServerManager.Players.Count / 2), packetData.GraceTime,
                    packetData.RoundTime);
            _netManager.EndRoundEvent += _ => OnEndRound();
            _netManager.ToggleAutomationEvent += packetData =>
                ToggleAutomation(packetData.GraceTime, packetData.RoundTime, packetData.SecondsBetweenRounds);

            _intervalTimer = new Timer(1000);
            _roundTimer = new Timer(_settings.RoundTimeSeconds * 1000);
            _roundOverTimer = new Timer(_settings.SecondsBetweenRounds * 1000);

            _intervalTimer.Elapsed += IntervalTimerElapse;
            _roundTimer.Elapsed += RoundTimerElapse;
            _roundOverTimer.Elapsed += RoundOverTimerElapse;

            _serverApi.ServerManager.PlayerConnectEvent += OnPlayerConnect;
            _serverApi.ServerManager.PlayerDisconnectEvent += OnPlayerDisconnect;
        }

        /// <summary>
        /// Handle a death from a player who was a hunter.
        /// </summary>
        /// <param name="playerId">The name of the player who died.</param>
        private void OnHunterDeath(ushort playerId)
        {
            if (_gameState != GameState.InRound) return;

            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                int convoNum = _random.Next(0, 6);
                _netManager.BroadcastPacket(FromServerToClientPackets.HunterDeath,
                    new HunterDeathFromServerToClientData
                    {
                        PlayerId = playerId,
                        ConvoNum = (byte)convoNum,
                    });
            }
        }

        /// <summary>
        /// Handle a death from a player who was a prop.
        /// </summary>
        /// <param name="playerId">The name of the player who died.</param>
        private void OnPropDeath(ushort playerId)
        {
            if (_gameState != GameState.InRound) return;

            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                _livingProps.Remove(player);
                if (PropsAlive <= 0)
                {
                    EndRound(true);
                    return;
                }

                var playersExcludingSender = _serverApi.ServerManager.Players.Where(p => p.Id != playerId);
                foreach (var p in playersExcludingSender)
                {
                    _netManager.SendPacket(FromServerToClientPackets.PropDeath,
                        new PropDeathFromServerToClientData
                        {
                            PlayerId = playerId,
                            PropsRemaining = PropsAlive,
                            PropsTotal = TotalProps,

                        }, p.Id);
                }

                _netManager.SendPacket(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                {
                    IsHunter = true,
                }, playerId);

                _allHunters.Add(player);
                _livingHunters.Add(player);
            }

            _logger.Info($"Player {playerId} died:\t{PropsAlive}/{TotalProps} props alive");
        }

        /// <summary>
        /// Relay a prop's position update for the x- and y-coordinates.
        /// </summary>
        /// <param name="playerId">The ID of the player who translated their prop.</param>
        /// <param name="x">The x component of the new prop position.</param>
        /// <param name="y">The y component of the new prop position.</param>
        private void OnUpdatePropPositionXy(ushort playerId, float x, float y)
        {
            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                var playersInScene = _serverApi.ServerManager.Players
                    .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id).ToArray();

                _logger.Info($"Relaying prop position ({x}, {y}) to {playersInScene.Length} players");
                _netManager.SendPacket(FromServerToClientPackets.UpdatePropPositionXy,
                    new UpdatePropPositionXyFromServerToClientData
                    {
                        PlayerId = playerId,
                        X = x,
                        Y = y,
                    }, playersInScene);
            }
        }

        /// <summary>
        /// Relay a prop's position update for the z-coordinate.
        /// </summary>
        /// <param name="playerId">The ID of the player who translated their prop.</param>
        /// <param name="z">The z component of the new prop position.</param>
        private void OnUpdatePropPositionZ(ushort playerId, float z)
        {
            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                var playersInScene = _serverApi.ServerManager.Players
                    .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id).ToArray();

                _logger.Info($"Relaying prop position Z {z} to {playersInScene.Length} players");
                _netManager.SendPacket(FromServerToClientPackets.UpdatePropPositionZ,
                    new UpdatePropPositionZFromServerToClientData
                    {
                        PlayerId = playerId,
                        Z = z,
                    }, playersInScene);
            }
        }

        /// <summary>
        /// Relay a prop's rotation update.
        /// </summary>
        /// <param name="playerId">The ID of the player who rotated their prop.</param>
        /// <param name="rotation">The new prop rotation.</param>
        private void OnUpdatePropRotation(ushort playerId, float rotation)
        {
            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                var playersInScene = _serverApi.ServerManager.Players
                    .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id).ToArray();

                _logger.Info($"Relaying prop rotation {rotation} to {playersInScene.Length} players");
                _netManager.SendPacket(FromServerToClientPackets.UpdatePropRotation,
                    new UpdatePropRotationFromServerToClientData
                    {
                        PlayerId = playerId,
                        Rotation = rotation,
                    }, playersInScene);
            }
        }

        /// <summary>
        /// Relay a prop's scale update.
        /// </summary>
        /// <param name="playerId">The ID of the player who scaled their prop.</param>
        /// <param name="scale">The new prop scale.</param>
        private void OnUpdatePropScale(ushort playerId, float scale)
        {
            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                var playersInScene = _serverApi.ServerManager.Players
                    .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id).ToArray();

                _logger.Info($"Relaying prop scale {scale} to {playersInScene.Length} players");
                _netManager.SendPacket(FromServerToClientPackets.UpdatePropScale,
                    new UpdatePropScaleFromServerToClientData
                    {
                        PlayerId = playerId,
                        Scale = scale,
                    }, playersInScene);
            }
        }

        /// <summary>
        /// Relay a prop's sprite update.
        /// </summary>
        /// <param name="playerId">The ID of the player who changed their prop sprite.</param>
        /// <param name="spriteName">The name of the prop's sprite.</param>
        /// <param name="numBytes">The number of bytes of the prop's sprite.</param>
        /// <param name="spriteBytes">The raw bytes of the prop's sprite.</param>
        /// <param name="positionX">The x-coordinate of the prop's position at the time of the update.</param>
        /// <param name="positionY">The y-coordinate of the prop's position at the time of the update.</param>
        /// <param name="positionZ">The z-coordinate of the prop's position at the time of the update.</param>
        /// <param name="rotation">The prop's rotation at the time of the update.</param>
        /// <param name="scale">The prop's scale at the time of the update.</param>
        private void OnUpdatePropSprite(ushort playerId, string spriteName, int numBytes, byte[] spriteBytes, float positionX,
            float positionY, float positionZ, float rotation, float scale)
        {
            if (_serverApi.ServerManager.TryGetPlayer(playerId, out var player))
            {
                var playersInScene = _serverApi.ServerManager.Players
                    .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id).ToArray();

                _logger.Info($"Relaying prop sprite {spriteName} to {playersInScene.Length} players");
                _netManager.SendPacket(FromServerToClientPackets.UpdatePropSprite, new UpdatePropSpriteFromServerToClientData
                {
                    PlayerId = playerId,
                    SpriteName = spriteName,
                    NumBytes = numBytes,
                    SpriteBytes = spriteBytes,
                    PositionX = positionX,
                    PositionY = positionY,
                    PositionZ = positionZ,
                    RotationZ = rotation,
                    Scale = scale,
                }, playersInScene);
            }
        }

        /// <summary>
        /// Start a new round.
        /// </summary>
        /// <param name="numHunters">The number of initial hunters at the start of the round.</param>
        /// <param name="graceTime">The amount of initial grace time in seconds.</param>
        /// <param name="roundTime">The amount of time in the round in seconds.</param>
        private void StartRound(ushort numHunters, byte graceTime, ushort roundTime)
        {
            var numPlayers = _serverApi.ServerManager.Players.Count;
            if (numPlayers < MinimumPlayers)
            {
                _logger.Warn($"Not enough players to start a round! {MinimumPlayers} required, {numPlayers} connected.");
                return;
            }

            _logger.Info($"Round started; Grace Time: {graceTime}, Round Time: {roundTime}");

            _gameState = GameState.InRound;

            if (_settings.Automated)
            {
                _roundOverTimer.Stop();
            }

            var players = _serverApi.ServerManager.Players.ToList();
            players.Shuffle();

            List<IServerPlayer> huntersLastRound = new(_allHunters);

            _allHunters.Clear();
            _allProps.Clear();
            _livingHunters.Clear();
            _livingProps.Clear();

            var possibleHunters = players.Where(player => !huntersLastRound.Contains(player)).ToList();

            _allHunters.AddRange(possibleHunters.GetRange(0, numHunters));
            _allProps.AddRange(players.Where(player => !_allHunters.Contains(player)));

            _livingHunters.AddRange(_allHunters);
            _livingProps.AddRange(_allProps);

            _logger.Info("Number of hunters: " + TotalHunters);
            _logger.Info("Number of props: " + TotalProps);

            _intervalTimer.Start();
            _roundTimer.Interval = roundTime * 1000;
            _roundTimer.Start();
            _dueTimeGrace = DateTime.Now.AddSeconds(graceTime);
            _dueTimeRound = DateTime.Now.AddSeconds(roundTime);

            foreach (var hunter in _allHunters)
            {
                _netManager.SendPacket(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                {
                    IsHunter = true,
                    InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0,
                }, hunter.Id);
            }

            foreach (var prop in _allProps)
            {
                _netManager.SendPacket(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                {
                    IsHunter = false,
                    InGrace = false,
                }, prop.Id);
            }

            _netManager.BroadcastPacket(FromServerToClientPackets.UpdateGraceTimer, new UpdateGraceTimerFromServerToClientData
            {
                TimeRemaining = graceTime,
            });

            _netManager.BroadcastPacket(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
            {
                TimeRemaining = roundTime,
            });
        }

        /// <summary>
        /// End the current round based on a player's request.
        /// </summary>
        private void OnEndRound()
        {
            EndRound(PropsAlive <= 0);
        }

        /// <summary>
        /// End the current round.
        /// </summary>
        /// <param name="huntersWin"></param>
        private void EndRound(bool huntersWin)
        {
            if (_settings.Automated)
            {
                _intervalTimer.Start();
            }
            else
            {
                _intervalTimer.Stop();
            }

            _roundTimer.Stop();

            if (_gameState != GameState.InRound) return;

            _logger.Info("Rounded ended; hunters won: " + huntersWin);

            if (_settings.Automated)
            {
                _dueTimeRoundOver = DateTime.Now.AddSeconds(_settings.SecondsBetweenRounds);
                _roundOverTimer.Start();
                _netManager.BroadcastPacket(FromServerToClientPackets.UpdateRoundOverTimer, new UpdateRoundOverTimerFromServerToClientData
                {
                    TimeRemaining = (ushort)_settings.SecondsBetweenRounds,
                });
            }

            _gameState = GameState.WaitingForRoundStart;
            _netManager.BroadcastPacket(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
            {
                HuntersWin = huntersWin,
            });
        }

        /// <summary>
        /// Toggle in settings whether rounds should be automatically started.
        /// </summary>
        /// <param name="graceTime">The duration of the initial grace time in seconds at the start of an automated round.</param>
        /// <param name="roundTime">The duration of time in an automated round.</param>
        /// <param name="secondsBetweenRounds">The duration of time between automated rounds.</param>
        private void ToggleAutomation(byte graceTime, ushort roundTime, ushort secondsBetweenRounds)
        {
            _logger.Info("Toggling automated rounds to " + !_settings.Automated);

            _settings.Automated = !_settings.Automated;
            _settings.GraceTimeSeconds = graceTime;
            _settings.RoundTimeSeconds = roundTime;
            _settings.SecondsBetweenRounds = secondsBetweenRounds;
            if (_settings.Automated && _gameState != GameState.InRound)
            {
                _intervalTimer.Start();
                _dueTimeRoundOver = DateTime.Now.AddSeconds(_settings.SecondsBetweenRounds);
                _roundOverTimer.Interval = _settings.SecondsBetweenRounds * 1000;
                _roundOverTimer.Start();
            }
            else
            {
                if (_gameState != GameState.InRound)
                {
                    _intervalTimer.Stop();
                }

                _dueTimeRoundOver = DateTime.Now;
                _roundOverTimer.Stop();
                _netManager.BroadcastPacket(FromServerToClientPackets.UpdateRoundOverTimer,
                    new UpdateRoundOverTimerFromServerToClientData
                    {
                        TimeRemaining = 0,
                    });
            }

            _settings.SaveToFile();
        }

        /// <summary>
        /// Handle when a player connects to the server.
        /// </summary>
        /// <param name="player">The player who connected.</param>
        private void OnPlayerConnect(IServerPlayer player)
        {
            if (_settings.Automated && _gameState == GameState.WaitingForPlayers)
            {
                // Start the countdown timer if enough players have connected.
                if (_gameState != GameState.InRound && _serverApi.ServerManager.Players.Count >= MinimumPlayers)
                {
                    _dueTimeRoundOver = DateTime.Now.AddSeconds(_settings.SecondsBetweenRounds);
                    _roundOverTimer.Start();
                    _intervalTimer.Start();
                }
            }

            // Assign the new player a team if a round is in progress.
            if (_gameState == GameState.InRound)
            {
                bool isHunter;
                if (HuntersAlive > PropsAlive)
                {
                    isHunter = false;
                    _allHunters.Add(player);
                    _livingProps.Add(player);
                }
                else if (PropsAlive > HuntersAlive)
                {
                    isHunter = true;
                    _allProps.Add(player);
                    _livingHunters.Add(player);
                }
                else
                {
                    var teamChoices = new[] { false, true };
                    isHunter = teamChoices[_random.Next(0, 2)];
                }

                _logger.Info("New player assigned to Hunters team: " + isHunter);

                _netManager.SendPacket(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                {
                    IsHunter = isHunter,
                    InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0,
                });
            }
        }

        /// <summary>
        /// Handle when a player disconnects from the server.
        /// </summary>
        /// <param name="player">The player who disconnected.</param>
        private void OnPlayerDisconnect(IServerPlayer player)
        {
            var numPlayers = _serverApi.ServerManager.Players.Count;
            if (numPlayers < MinimumPlayers)
            {
                _logger.Warn(
                    $"Not enough players to start a round. {MinimumPlayers} required, {numPlayers} connected. Ending round.");
                EndRound(PropsAlive <= 0);
                return;
            }

            if (_gameState != GameState.InRound) return;

            var disconnectedProp = _livingProps.FirstOrDefault(prop => prop.Id == player.Id);
            var disconnectedHunter = _livingHunters.FirstOrDefault(hunter => hunter.Id == player.Id);

            if (disconnectedProp != null)
            {
                _livingProps.Remove(disconnectedProp);

                if (PropsAlive <= 0)
                {
                    EndRound(true);
                    return;
                }
            }

            _netManager.BroadcastPacket(FromServerToClientPackets.PlayerLeftRound, new PlayerLeftRoundFromServerToClientData
            {
                PlayerId = player.Id,
                PropsRemaining = disconnectedHunter != null ? (ushort)0 : PropsAlive,
                PropsTotal = disconnectedHunter != null ? (ushort)0 : TotalProps,
            });

            _logger.Info(
                $"Player {player.Id} left the server: {_livingHunters.Count}/{_allHunters.Count} hunters alive,\t{_livingProps.Count}/{_allProps.Count} props alive");
        }

        /// <summary>
        /// Handle when the interval timer elapses.
        /// </summary>
        private void IntervalTimerElapse(object _, ElapsedEventArgs elapsedEventArgs)
        {
            if (_gameState == GameState.InRound)
            {
                var roundTimeRemaining = (_dueTimeRound - DateTime.Now).TotalSeconds;
                if (roundTimeRemaining >= 0)
                {
                    _netManager.BroadcastPacket(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
                    {
                        TimeRemaining = (ushort)roundTimeRemaining,
                    });
                }
            }
            else
            {
                var roundOverTimeRemaining = (_dueTimeRoundOver - DateTime.Now).TotalSeconds;
                if (roundOverTimeRemaining >= 0)
                {
                    _netManager.BroadcastPacket(FromServerToClientPackets.UpdateRoundOverTimer, new UpdateRoundOverTimerFromServerToClientData
                    {
                        TimeRemaining = (ushort)roundOverTimeRemaining,
                    });
                }
            }

            var graceTimeRemaining = (_dueTimeGrace - DateTime.Now).TotalSeconds;
            if (graceTimeRemaining >= 0)
            {
                _netManager.BroadcastPacket(FromServerToClientPackets.UpdateGraceTimer, new UpdateGraceTimerFromServerToClientData
                {
                    TimeRemaining = (byte)graceTimeRemaining,
                });
            }
        }

        /// <summary>
        /// Handle when the round timer ends.
        /// </summary>
        private void RoundTimerElapse(object _, ElapsedEventArgs elapsedEventArgs)
        {
            EndRound(PropsAlive <= 0);
        }

        /// <summary>
        /// Handle when the round over timer ends.
        /// </summary>
        private void RoundOverTimerElapse(object _, ElapsedEventArgs elapsedEventArgs)
        {
            var numPlayers = _serverApi.ServerManager.Players.Count;
            StartRound((ushort)Math.Min(ushort.MaxValue, Math.Max(1, numPlayers / 10)), _settings.GraceTimeSeconds, _settings.RoundTimeSeconds);
        }
    }
}
