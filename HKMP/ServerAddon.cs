using Hkmp.Api.Server;
using HkmpPouch;
using PropHunt.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Random = System.Random;

namespace PropHunt.HKMP
{
    internal class PropHuntServerAddon : ServerAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override bool NeedsNetwork => true;

        private PipeServer _pipe;

        public static PropHuntServerAddon Instance { get; private set; }

        /// <summary>
        /// A collection of all hunters playing.
        /// </summary>
        private List<IServerPlayer> _allHunters = new();
        /// <summary>
        /// A collection of all props playing.
        /// </summary>
        private List<IServerPlayer> _allProps = new();
        /// <summary>
        /// A collection of all currently alive players on the Hunters team.
        /// </summary>
        private List<IServerPlayer> _livingHunters = new();
        /// <summary>
        /// A collection of all currently alive players on the Props team.
        /// </summary>
        private List<IServerPlayer> _livingProps = new();
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
        private readonly Timer _roundTimer = new();
        /// <summary>
        /// A date-time object that contains the time at which a round will end.
        /// </summary>
        private DateTime _dueTimeRound = DateTime.Now;
        /// <summary>
        /// A date-time object that contains the time at which the grace period will end.
        /// </summary>
        private DateTime _dueTimeGrace = DateTime.Now;
        /// <summary>
        /// Timer that handles updating every player's timer each second.
        /// </summary>
        private readonly Timer _intervalTimer = new();

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
                Console.WriteLine("Player death: " + pipeEvent.FromPlayer);
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

            _intervalTimer.Interval = 1000;
            _intervalTimer.AutoReset = true;
            
            _roundTimer.AutoReset = false;
            _roundTimer.Elapsed += (_, _) =>
            {
                _intervalTimer.Stop();

                _pipe.Broadcast(new EndRoundEvent { HuntersWin = PropsAlive <= 0 });
            };
            
            _intervalTimer.Elapsed += (_, _) =>
            {
                _pipe.Broadcast(new UpdateRoundTimeEvent { TimeRemaining = (uint)(_dueTimeRound - DateTime.Now).TotalSeconds });

                var graceTimeRemaining = (_dueTimeGrace - DateTime.Now).TotalSeconds;
                if (graceTimeRemaining >= 0)
                {
                    _pipe.Broadcast(new UpdateGraceTimeEvent { TimeRemaining = (uint)graceTimeRemaining });
                }
            };

            _pipe.ServerApi.ServerManager.PlayerConnectEvent += OnPlayerConnect;
            _pipe.ServerApi.ServerManager.PlayerDisconnectEvent += OnPlayerDisconnect;
        }
        
        /// <summary>
        /// Start a round.
        /// </summary>
        /// <param name="gracePeriod">The starting amount of time in the grace period</param>
        /// <param name="roundTime">The starting amount of time in the round</param>
        private void StartRound(uint gracePeriod, uint roundTime)
        {
            _roundStarted = true;
            
            var players = _pipe.ServerApi.ServerManager.Players.ToList();
            players = players.OrderBy(_ => Guid.NewGuid()).ToList();
            int halfCount = players.Count / 2;

            _allHunters.Clear();
            _allProps.Clear();
            _livingHunters.Clear();
            _livingProps.Clear();

            _allHunters = players.GetRange(0, halfCount);
            _allProps = players.GetRange(halfCount, players.Count - halfCount);

            _livingHunters.AddRange(_allHunters);
            _livingProps.AddRange(_allProps);

            Console.WriteLine("Number of hunters: " + TotalHunters);
            Console.WriteLine("Number of props: " + TotalProps);

            _roundTimer.Interval = roundTime * 1000;
            _roundTimer.Start();
            _dueTimeRound = DateTime.Now.AddMilliseconds(_roundTimer.Interval);
            _dueTimeGrace = DateTime.Now.AddSeconds(gracePeriod);

            _intervalTimer.Start();

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
            _roundStarted = false;
            _roundTimer.Stop();
            _intervalTimer.Stop();
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
                var playersExcludingSender = _pipe.ServerApi.ServerManager.Players.Where(player => player.Id != playerId).ToList();
                playersExcludingSender.ForEach(player => _pipe.SendToPlayer(player.Id, new PlayerDeathEvent { PlayerId = playerId, HuntersRemaining = HuntersAlive, HuntersTotal = TotalHunters, PropsRemaining = PropsAlive, PropsTotal = TotalProps }));
            }

            if (deadProp != null)
            {
                _allHunters.Add(deadProp);
                _livingHunters.Add(deadProp);
            }
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
                    _roundStarted = false;
                    _roundTimer.Stop();
                    _intervalTimer.Stop();
                    _pipe.Broadcast(new EndRoundEvent { HuntersWin = true });
                    return;
                }
            }
            else if (disconnectedHunter != null)
            {
                _livingHunters.Remove(disconnectedHunter);

                if (HuntersAlive <= 0)
                {
                    _roundStarted = false;
                    _roundTimer.Stop();
                    _intervalTimer.Stop();
                    _pipe.Broadcast(new EndRoundEvent() { HuntersWin = false });
                    return;
                }
            }

            _pipe.Broadcast(new PlayerLeaveEvent { PlayerId = player.Id, HuntersRemaining = HuntersAlive, HuntersTotal = TotalHunters, PropsRemaining = PropsAlive, PropsTotal = TotalProps });
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