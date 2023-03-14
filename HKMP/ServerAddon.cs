using Hkmp.Api.Server;
using HkmpPouch;
using PropHunt.Events;
using PropHunt.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Random = System.Random;

namespace PropHunt.HKMP
{
    internal class PropHuntServerAddon : ServerAddon
    {
        protected override string Name => Constants.NAME;
        protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override bool NeedsNetwork => true;

        private PipeServer _pipe;

        public static PropHuntServerAddon Instance { get; private set; }

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
        /// Whether a round has started.
        /// </summary>
        private bool _roundStarted;
        /// <summary>
        /// Timer that handles the length of time that a round goes for.
        /// </summary>
        private Timer _roundTimer;
        /// <summary>
        /// A date-time object that contains the time at which a round will end.
        /// </summary>
        private DateTime _dueTimeRound;
        /// <summary>
        /// A date-time object that contains the time at which the grace period will end.
        /// </summary>
        private DateTime _dueTimeGrace;
        /// <summary>
        /// Timer that handles updating every player's timer each second.
        /// </summary>
        private Timer _intervalTimer;

        public override void Initialize(IServerApi serverApi)
        {
            Instance = this;
            
            _pipe = new PipeServer(Name);
            
            _pipe.On(StartRoundEventFactory.Instance).Do<StartRoundEvent>(pipeEvent =>
            {
                StartRound(pipeEvent.GracePeriod, pipeEvent.RoundTime);
            });
            
            _pipe.On(EndRoundEventFactory.Instance).Do<EndRoundEvent>(pipeEvent =>
            {
                EndRound(pipeEvent.HuntersWin);
            });
            
            _pipe.On(PlayerDeathEventFactory.Instance).Do<PlayerDeathEvent>(pipeEvent =>
            {
                PlayerDeath(pipeEvent.FromPlayer);
            });
            
            _pipe.On(UpdateGraceTimeEventFactory.Instance).Do<UpdateGraceTimeEvent>(pipeEvent =>
            {
                UpdateGraceTime(pipeEvent.TimeRemaining);
            });
            
            _pipe.On(UpdateRoundTimeEventFactory.Instance).Do<UpdateRoundTimeEvent>(pipeEvent =>
            {
                UpdateRoundTime(pipeEvent.TimeRemaining);
            });

            var sender = serverApi.NetServer.GetNetworkSender<FromServerToClientPackets>(Instance);
            var receiver = serverApi.NetServer.GetNetworkReceiver<FromClientToServerPackets>(Instance, serverPacket =>
            {
                Console.WriteLine("Received packet: " + serverPacket);
                return serverPacket switch
                {
                    FromClientToServerPackets.BroadcastPropSprite => new PropSpriteFromClientToServerData(),
                    _ => null,
                };
            });

            receiver.RegisterPacketHandler<PropSpriteFromClientToServerData>(FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        _pipe.Logger.Info($"Relaying prop sprite {packetData.SpriteName} to {playersInScene.Length} players");
                        sender.SendSingleData(FromServerToClientPackets.UpdatePropSprite, new PropSpriteFromServerToClientData
                        {
                            PlayerId = id,
                            SpriteName = packetData.SpriteName,
                            NumBytes = packetData.NumBytes,
                            SpriteBytes = packetData.SpriteBytes,
                            PositionXY = packetData.PositionXY,
                            PositionZ = packetData.PositionZ,
                            RotationZ = packetData.RotationZ,
                            Scale = packetData.Scale,
                        }, playersInScene);
                    }
                });

            _intervalTimer = new Timer(IntervalTimerElapse, null, Timeout.Infinite, Timeout.Infinite);
            _roundTimer = new Timer(RoundTimerElapse, null, Timeout.Infinite, Timeout.Infinite);
            Task.Run(async () =>
            {
                while (_pipe.ServerApi.ServerManager == null)
                {
                    await Task.Delay(10);
                }

                _pipe.ServerApi.ServerManager.PlayerConnectEvent += OnPlayerConnect;
                _pipe.ServerApi.ServerManager.PlayerDisconnectEvent += OnPlayerDisconnect;

                _pipe.Logger.Info("Registered player connect/disconnect delegates.");
            });
        }

        private void IntervalTimerElapse(object stateInfo)
        {
            _pipe.Broadcast(new UpdateRoundTimeEvent { TimeRemaining = (uint)(_dueTimeRound - DateTime.Now).TotalSeconds });
            
            var graceTimeRemaining = (_dueTimeGrace - DateTime.Now).TotalSeconds;
            if (graceTimeRemaining >= 0)
            {
                _pipe.Broadcast(new UpdateGraceTimeEvent { TimeRemaining = (uint)graceTimeRemaining });
            }
        }

        private void RoundTimerElapse(object stateInfo)
        {
            EndRound(PropsAlive <= 0);
        }
        
        /// <summary>
        /// Start a round.
        /// </summary>
        /// <param name="gracePeriod">The starting amount of time in the grace period</param>
        /// <param name="roundTime">The starting amount of time in the round</param>
        private void StartRound(uint gracePeriod, uint roundTime)
        {
            _pipe.Logger.Info($"Round started; Grace Time: {gracePeriod}, Round Time: {roundTime}");
            _roundStarted = true;
            var players = _pipe.ServerApi.ServerManager.Players.ToList();
            players.Shuffle();
            int halfCount = players.Count / 2;

            _allHunters.Clear();
            _allProps.Clear();
            _livingHunters.Clear();
            _livingProps.Clear();

            _allHunters.AddRange(players.GetRange(0, halfCount));
            _allProps.AddRange(players.GetRange(halfCount, players.Count - halfCount));

            _livingHunters.AddRange(_allHunters);
            _livingProps.AddRange(_allProps);

            _pipe.Logger.Debug("Number of hunters: " + TotalHunters);
            _pipe.Logger.Debug("Number of props: " + TotalProps);

            _intervalTimer.Change(1000, 1000);
            _roundTimer.Change(roundTime * 1000, Timeout.Infinite);
            _dueTimeGrace = DateTime.Now.AddSeconds(gracePeriod);
            _dueTimeRound = DateTime.Now.AddSeconds(roundTime);

            _allHunters.ForEach(hunter => _pipe.SendToPlayer(hunter.Id, new AssignTeamEvent { IsHunter = true, InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0 }));
            _allProps.ForEach(prop => _pipe.SendToPlayer(prop.Id, new AssignTeamEvent { IsHunter = false }));
            _pipe.Broadcast(new UpdateGraceTimeEvent { TimeRemaining = gracePeriod });
            _pipe.Broadcast(new UpdateRoundTimeEvent { TimeRemaining = roundTime });
        }

        /// <summary>
        /// End a round.
        /// </summary>
        /// <param name="huntersWin">Whether the Hunters team won the round</param>
        private void EndRound(bool huntersWin)
        {
            _pipe.Logger.Info("Rounded ended; hunters win: " + huntersWin);
            _roundStarted = false;
            _roundTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _pipe.Broadcast(new EndRoundEvent { HuntersWin = huntersWin });
        }

        /// <summary>
        /// Handle a player's death.
        /// </summary>
        /// <param name="playerId">The player that died</param>
        private void PlayerDeath(ushort playerId)
        {
            if (!_roundStarted) return;

            var deadProp = _livingProps.FirstOrDefault(prop => prop.Id == playerId);
            var deadHunter = _livingHunters.FirstOrDefault(hunter => hunter.Id == playerId);
            
            if (deadProp != null)
            {
                _livingProps.Remove(deadProp);
                if (PropsAlive <= 0)
                {
                    EndRound(true);
                }
                   
                _pipe.SendToPlayer(playerId, new AssignTeamEvent { IsHunter = true });
            }
            
            if (deadHunter != null)
            {
                _livingHunters.Remove(deadHunter);
                if (HuntersAlive <= 0)
                {
                    EndRound(false);
                }
            }

            if (_roundStarted)
            {
                var playersExcludingSender =
                    _pipe.ServerApi.ServerManager.Players.Where(player => player.Id != playerId).ToList();
                playersExcludingSender.ForEach(player => _pipe.SendToPlayer(player.Id,
                    new PlayerDeathEvent
                    {
                        PlayerId = playerId, HuntersRemaining = HuntersAlive, HuntersTotal = TotalHunters,
                        PropsRemaining = PropsAlive, PropsTotal = TotalProps
                    }));
            }

            if (deadProp != null)
            {
                _allHunters.Add(deadProp);
                _livingHunters.Add(deadProp);
            }

            _pipe.Logger.Info(
                $"Player {playerId} died: {_livingHunters.Count}/{_allHunters.Count} hunters alive,\t{_livingProps.Count}/{_allProps.Count} props alive");
        }

        /// <summary>
        /// Called when a player connects to the server.
        /// </summary>
        /// <param name="player">The player that connected</param>
        private void OnPlayerConnect(IServerPlayer player)
        {
            if (!_roundStarted) return;

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
                var rand = new Random();
                isHunter = teamChoices[rand.Next(0, 2)];
            }

            _pipe.Logger.Info("New player assigned to Hunters team: " + isHunter);

            _pipe.SendToPlayer(player.Id, new StartRoundEvent { GracePeriod = (uint)(_dueTimeGrace - DateTime.Now).TotalSeconds, RoundTime = (uint)(_dueTimeRound - DateTime.Now).TotalSeconds });
            _pipe.SendToPlayer(player.Id, new AssignTeamEvent { IsHunter = isHunter, InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0 });
        }

        /// <summary>
        /// Called when a player disconnects from the server.
        /// </summary>
        /// <param name="player">The player that disconnected</param>
        private void OnPlayerDisconnect(IServerPlayer player)
        {
            if (!_roundStarted) return;

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
            else if (disconnectedHunter != null)
            {
                _livingHunters.Remove(disconnectedHunter);

                if (HuntersAlive <= 0)
                {
                    EndRound(false);
                    return;
                }
            }

            _pipe.Broadcast(new PlayerLeaveEvent { PlayerId = player.Id, HuntersRemaining = HuntersAlive, HuntersTotal = TotalHunters, PropsRemaining = PropsAlive, PropsTotal = TotalProps });

            _pipe.Logger.Info(
                $"Player {player.Id} left the server: {_livingHunters.Count}/{_allHunters.Count} hunters alive,\t{_livingProps.Count}/{_allProps.Count} props alive");
        }
        
        /// <summary>
        /// Update the amount of time remaining in the grace period.
        /// </summary>
        /// <param name="timeRemaining">The remaining grace time</param>
        private void UpdateGraceTime(uint timeRemaining)
        {
            _pipe.Broadcast(new UpdateGraceTimeEvent { TimeRemaining = timeRemaining });
        }

        /// <summary>
        /// Update the amount of time remaining in the round.
        /// </summary>
        /// <param name="timeRemaining">The remaining round time</param>
        private void UpdateRoundTime(uint timeRemaining)
        {
            _pipe.Broadcast(new UpdateRoundTimeEvent { TimeRemaining = timeRemaining });
        }
    }
}