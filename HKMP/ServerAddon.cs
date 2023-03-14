using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
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

        public static PropHuntServerAddon Instance { get; private set; }

        private static IServerAddonNetworkSender<FromServerToClientPackets> _sender;
        private static IServerAddonNetworkReceiver<FromClientToServerPackets> _receiver;

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

            _sender = serverApi.NetServer.GetNetworkSender<FromServerToClientPackets>(Instance);
            _receiver = serverApi.NetServer.GetNetworkReceiver<FromClientToServerPackets>(Instance, serverPacket =>
            {
                Console.WriteLine("Received packet: " + serverPacket);
                return serverPacket switch
                {
                    FromClientToServerPackets.BroadcastPlayerDeath => new BroadcastPlayerDeathFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropPositionXY=> new BroadcastPropPositionXYFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropPositionZ => new BroadcastPropPositionZFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropRotation => new BroadcastPropRotationFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropScale => new BroadcastPropScaleFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropSprite => new BroadcastPropSpriteFromClientToServerData(),
                    FromClientToServerPackets.EndRound => new EndRoundFromClientToServerData(),
                    FromClientToServerPackets.StartRound => new StartRoundFromClientToServerData(),
                    _ => null,
                };
            });

            _receiver.RegisterPacketHandler<BroadcastPlayerDeathFromClientToServerData>(
                FromClientToServerPackets.BroadcastPlayerDeath,
                (id, packetData) =>
                {
                    if (!_roundStarted) return;

                    var deadProp = _livingProps.FirstOrDefault(prop => prop.Id == id);
                    var deadHunter = _livingHunters.FirstOrDefault(hunter => hunter.Id == id);

                    if (deadProp != null)
                    {
                        _livingProps.Remove(deadProp);
                        if (PropsAlive <= 0)
                        {
                            EndRound(true);
                            return;
                        }

                        _sender.SendSingleData(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                        {
                            IsHunter = true,
                        }, id);
                    }

                    if (deadHunter != null)
                    {
                        _livingHunters.Remove(deadHunter);
                        if (HuntersAlive <= 0)
                        {
                            EndRound(false);
                            return;
                        }
                    }

                    var playersExcludingSender =
                        serverApi.ServerManager.Players.Where(player => player.Id != id).ToList();
                    foreach (var player in playersExcludingSender)
                    {
                        _sender.SendSingleData(FromServerToClientPackets.PlayerDeath,
                            new PlayerDeathFromServerToClientData
                            {
                                PlayerId = id,
                                HuntersRemaining = HuntersAlive,
                                HuntersTotal = TotalHunters,
                                PropsRemaining = PropsAlive,
                                PropsTotal = TotalProps,
                                
                            }, player.Id);
                    }

                    if (deadProp != null)
                    {
                        _allHunters.Add(deadProp);
                        _livingHunters.Add(deadProp);
                    }

                    Console.WriteLine(
                        $"Player {id} died: {_livingHunters.Count}/{_allHunters.Count} hunters alive,\t{_livingProps.Count}/{_allProps.Count} props alive");
                });

            _receiver.RegisterPacketHandler<BroadcastPropPositionXYFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropPositionXY,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        Console.WriteLine($"Relaying prop position ({packetData.X}, {packetData.Y}) to {playersInScene.Length} players");
                        _sender.SendSingleData(FromServerToClientPackets.UpdatePropPositionXY, new UpdatePropPositionXYFromServerToClientData
                        {
                            PlayerId = id,
                            X = packetData.X,
                            Y = packetData.Y,
                        }, playersInScene);
                    }
                });

            _receiver.RegisterPacketHandler<BroadcastPropPositionZFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        Console.WriteLine($"Relaying prop position Z {packetData.Z} to {playersInScene.Length} players");
                        _sender.SendSingleData(FromServerToClientPackets.UpdatePropPositionZ, new UpdatePropPositionZFromServerToClientData
                        {
                            PlayerId = id,
                            Z = packetData.Z,   
                        }, playersInScene);
                    }
                });

            _receiver.RegisterPacketHandler<BroadcastPropRotationFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        Console.WriteLine($"Relaying prop rotation {packetData.Rotation} to {playersInScene.Length} players");
                        _sender.SendSingleData(FromServerToClientPackets.UpdatePropRotation, new UpdatePropRotationFromServerToClientData
                        {
                            PlayerId = id,
                            Rotation = packetData.Rotation,
                        }, playersInScene);
                    }
                });

            _receiver.RegisterPacketHandler<BroadcastPropScaleFromClientToServerData>(
                FromClientToServerPackets.BroadcastPropScale,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        Console.WriteLine($"Relaying prop scale {packetData.Scale} to {playersInScene.Length} players");
                        _sender.SendSingleData(FromServerToClientPackets.UpdatePropScale, new UpdatePropScaleFromServerToClientData
                        {
                            PlayerId = id,
                            Scale = packetData.Scale,
                        }, playersInScene);
                    }
                });

            _receiver.RegisterPacketHandler<BroadcastPropSpriteFromClientToServerData>(FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) =>
                {
                    if (serverApi.ServerManager.TryGetPlayer(id, out var player))
                    {
                        var playersInScene = serverApi.ServerManager.Players
                            .Where(p => p.CurrentScene == player.CurrentScene && p != player).Select(p => p.Id)
                            .ToArray();

                        Console.WriteLine($"Relaying prop sprite {packetData.SpriteName} to {playersInScene.Length} players");
                        _sender.SendSingleData(FromServerToClientPackets.UpdatePropSprite, new UpdatePropSpriteFromServerToClientData
                        {
                            PlayerId = id,
                            SpriteName = packetData.SpriteName,
                            NumBytes = packetData.NumBytes,
                            SpriteBytes = packetData.SpriteBytes,
                            PositionX = packetData.PositionX,
                            PositionY = packetData.PositionY,
                            PositionZ = packetData.PositionZ,
                            RotationZ = packetData.RotationZ,
                            Scale = packetData.Scale,
                        }, playersInScene);
                    }
                });

            _receiver.RegisterPacketHandler<EndRoundFromClientToServerData>(FromClientToServerPackets.EndRound,
                (id, packetData) =>
                {
                    EndRound(PropsAlive <= 0);
                });

            _receiver.RegisterPacketHandler<StartRoundFromClientToServerData>(FromClientToServerPackets.StartRound,
                (id, packetData) =>
                {
                    byte graceTime = packetData.GraceTime;
                    ushort roundTime = packetData.RoundTime;
                    Console.WriteLine($"Round started; Grace Time: {graceTime}, Round Time: {roundTime}");
                    _roundStarted = true;
                    var players = serverApi.ServerManager.Players.ToList();
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

                    Console.WriteLine("Number of hunters: " + TotalHunters);
                    Console.WriteLine("Number of props: " + TotalProps);

                    _intervalTimer.Change(1000, 1000);
                    _roundTimer.Change(packetData.RoundTime * 1000, Timeout.Infinite);
                    _dueTimeGrace = DateTime.Now.AddSeconds(graceTime);
                    _dueTimeRound = DateTime.Now.AddSeconds(roundTime);

                    foreach (var hunter in _allHunters)
                    {
                        _sender.SendSingleData(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                        {
                            IsHunter = true,
                            InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0,
                        }, hunter.Id);
                    }

                    foreach (var prop in _allProps)
                    {
                        _sender.SendSingleData(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                        {
                            IsHunter = false,
                            InGrace = false,
                        }, prop.Id);
                    }
                    
                    _sender.BroadcastSingleData(FromServerToClientPackets.UpdateGraceTimer, new UpdateGraceTimerFromServerToClientData
                    {
                        TimeRemaining = graceTime,
                    });

                    _sender.BroadcastSingleData(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
                    {
                        TimeRemaining = roundTime,
                    });
                });

            _intervalTimer = new Timer(IntervalTimerElapse, null, Timeout.Infinite, Timeout.Infinite);
            _roundTimer = new Timer(RoundTimerElapse, null, Timeout.Infinite, Timeout.Infinite);
            Task.Run(async () =>
            {
                while (serverApi.ServerManager == null)
                {
                    await Task.Delay(10);
                }

                serverApi.ServerManager.PlayerConnectEvent += player =>
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

                    Console.WriteLine("New player assigned to Hunters team: " + isHunter);

                    _sender.SendSingleData(FromServerToClientPackets.AssignTeam, new AssignTeamFromServerToClientData
                    {
                        IsHunter = isHunter,
                        InGrace = (_dueTimeGrace - DateTime.Now).TotalSeconds > 0,
                    });
                };
                serverApi.ServerManager.PlayerDisconnectEvent += player =>
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

                    _sender.BroadcastSingleData(FromServerToClientPackets.PlayerLeftGame, new PlayerLeftGameFromServerToClientData
                    {
                        PlayerId = player.Id,
                        HuntersRemaining = HuntersAlive,
                        HuntersTotal = TotalHunters,
                        PropsRemaining = PropsAlive,
                        PropsTotal = TotalProps,
                    });

                    Console.WriteLine(
                        $"Player {player.Id} left the server: {_livingHunters.Count}/{_allHunters.Count} hunters alive,\t{_livingProps.Count}/{_allProps.Count} props alive");
                };

                Console.WriteLine("Registered player connect/disconnect delegates.");
            });
        }

        /// <summary>
        /// Handle when the interval timer elapses.
        /// </summary>
        private void IntervalTimerElapse(object _)
        {
            _sender.BroadcastSingleData(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
            {
                TimeRemaining = (ushort)(_dueTimeRound - DateTime.Now).TotalSeconds,
            });
            
            var graceTimeRemaining = (_dueTimeGrace - DateTime.Now).TotalSeconds;
            if (graceTimeRemaining >= 0)
            {
                _sender.BroadcastSingleData(FromServerToClientPackets.UpdateGraceTimer, new UpdateGraceTimerFromServerToClientData
                {
                    TimeRemaining = (byte)graceTimeRemaining,
                });
            }
        }

        /// <summary>
        /// Handle when the round timer ends.
        /// </summary>
        private void RoundTimerElapse(object _)
        {
            EndRound(PropsAlive <= 0);
        }

        /// <summary>
        /// End a round.
        /// </summary>
        /// <param name="huntersWin">Whether the Hunters team won the round</param>
        private void EndRound(bool huntersWin)
        {
            Console.WriteLine("Rounded ended; hunters win: " + huntersWin);
            _roundStarted = false;
            _roundTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _sender.BroadcastSingleData(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
            {
                HuntersWin = huntersWin,
            });
        }
    }
}