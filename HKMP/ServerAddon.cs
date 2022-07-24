using Hkmp.Api.Server;
using Hkmp.Networking.Packet.Data;
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

        public static PropHuntServerAddon Instance { get; private set; }

        // A collection of all hunters playing
        private List<IServerPlayer> _allHunters = new();
        // A collection of all props playing
        private List<IServerPlayer> _allProps = new();
        // A collection of all currently alive players on the Hunters team
        private List<IServerPlayer> _livingHunters = new();
        // A collection of all currently alive players on the Props team
        private List<IServerPlayer> _livingProps = new();
        // The total number of hunters playing
        private ushort TotalHunters => (ushort)_allHunters.Count;
        // The total number of props playing
        private ushort TotalProps => (ushort)_allProps.Count;
        // The number of hunters that are alive
        private ushort HuntersAlive => (ushort)_livingHunters.Count;
        // The number of props that are alive
        private ushort PropsAlive => (ushort)_livingProps.Count;
        // Whether a round has started
        private bool _roundStarted;

        public override void Initialize(IServerApi serverApi)
        {
            Instance = this;

            var sender = serverApi.NetServer.GetNetworkSender<FromServerToClientPackets>(Instance);
            var receiver = serverApi.NetServer.GetNetworkReceiver<FromClientToServerPackets>(Instance, serverPacket =>
            {
                return serverPacket switch
                {
                    FromClientToServerPackets.BroadcastPropSprite => new PropSpriteFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropPositionXY => new PropPositionXYFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropPositionZ => new PropPositionZFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropRotation => new PropRotationFromClientToServerData(),
                    FromClientToServerPackets.BroadcastPropScale => new PropScaleFromClientToServerData(),
                    FromClientToServerPackets.SetPlayingPropHunt => new SetPlayingPropHuntFromClientToServerData(),
                    FromClientToServerPackets.PlayerDeath => new ReliableEmptyData(),
                    _ => null
                };
            });

            // Timer that handles the length of time that a round goes for
            var roundTimer = new Timer();
            roundTimer.AutoReset = false;
            // A date-time object that contains the time at which a round will end.
            var dueTimeRound = DateTime.Now;
            // A date-time object that contains the time at which the grace period will end.
            var dueTimeGrace = DateTime.Now;

            // Timer that handles updating every player's timer each second
            var intervalTimer = new Timer();
            intervalTimer.Interval = 1000;
            intervalTimer.AutoReset = true;

            roundTimer.Elapsed += (_, _) =>
            {
                intervalTimer.Stop();

                sender.BroadcastSingleData(
                    FromServerToClientPackets.EndRound,
                    new EndRoundFromServerToClientData
                    {
                        HuntersWin = PropsAlive <= 0,
                    });
            };

            intervalTimer.Elapsed += (obj, e) =>
            {
                sender.BroadcastSingleData(
                    FromServerToClientPackets.UpdateRoundTimer,
                    new UpdateRoundTimerFromServerToClientData
                    {
                        TimeRemaining = (int)(dueTimeRound - DateTime.Now).TotalSeconds,
                    }
                );

                var graceTimeRemaining = (dueTimeGrace - DateTime.Now).TotalSeconds;
                if (graceTimeRemaining >= 0)
                {
                    sender.BroadcastSingleData(
                        FromServerToClientPackets.UpdateGraceTimer,
                        new UpdateGraceTimerFromServerToClientData
                        {
                            TimeRemaining = (int)(dueTimeGrace - DateTime.Now).TotalSeconds,
                        }
                    );
                }
            };

            receiver.RegisterPacketHandler<PropSpriteFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var otherPlayers = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(
                        FromServerToClientPackets.SendPropSprite,
                        new PropSpriteFromServerToClientData
                        {
                            PlayerId = id,
                            SpriteName = packetData.SpriteName,
                        }, otherPlayers);
                }
            );

            receiver.RegisterPacketHandler<PropPositionXYFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionXY,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(
                        FromServerToClientPackets.SendPropPositionXY,
                        new PropPositionXYFromServerToClientData
                        {
                            PlayerId = id,
                            PositionXY = packetData.PositionXY,
                        }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropPositionZFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(
                        FromServerToClientPackets.SendPropPositionZ,
                        new PropPositionZFromServerToClientData
                        {
                            PlayerId = id,
                            PositionZ = packetData.PositionZ,
                        }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropRotationFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(
                        FromServerToClientPackets.SendPropRotation,
                        new PropRotationFromServerToClientData
                        {
                            PlayerId = id,
                            Rotation = packetData.Rotation,
                        }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<PropScaleFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropScale,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    sender.SendSingleData(
                        FromServerToClientPackets.SendPropScale,
                        new PropScaleFromServerToClientData
                        {
                            PlayerId = id,
                            ScaleFactor = packetData.ScaleFactor,
                        }, playersInScene);
                }
            );

            receiver.RegisterPacketHandler<SetPlayingPropHuntFromClientToServerData>
            (
                FromClientToServerPackets.SetPlayingPropHunt,
                (id, packetData) =>
                {
                    _roundStarted = packetData.Playing;
                    if (_roundStarted)
                    {
                        var players = serverApi.ServerManager.Players.ToList();
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

                        sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Hunters,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, _allHunters.Select(hunter => hunter.Id).ToArray());

                        sender.SendSingleData(
                            FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Props,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, _allProps.Select(prop => prop.Id).ToArray());

                        roundTimer.Interval = packetData.RoundTime * 1000;
                        roundTimer.Start();
                        dueTimeRound = DateTime.Now.AddMilliseconds(roundTimer.Interval);
                        dueTimeGrace = DateTime.Now.AddSeconds(packetData.GracePeriod);

                        intervalTimer.Start();
                    }
                    else
                    {
                        roundTimer.Stop();
                        intervalTimer.Stop();

                        sender.BroadcastSingleData(
                            FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PlayerId = id,
                                Playing = false,
                            }
                        );
                    }
                }
            );

            receiver.RegisterPacketHandler<ReliableEmptyData>
            (
                FromClientToServerPackets.PlayerDeath,
                (id, _) =>
                {
                    if (!_roundStarted) return;

                    var deadProp = _livingProps.FirstOrDefault(prop => prop.Id == id);
                    
                    if (deadProp != null)
                    {
                        Console.WriteLine("Dead prop");
                        _livingProps.Remove(deadProp);
                        _allHunters.Add(deadProp);
                        _livingHunters.Add(deadProp);

                        if (PropsAlive <= 0)
                        {
                            _roundStarted = false;
                            roundTimer.Stop();
                            intervalTimer.Stop();
                            sender.BroadcastSingleData(
                                FromServerToClientPackets.EndRound,
                                new EndRoundFromServerToClientData
                                {
                                    HuntersWin = true,
                                }
                            );
                            return;
                        }
                        else
                        {
                            sender.SendSingleData(
                                FromServerToClientPackets.SetPlayingPropHunt,
                                new SetPlayingPropHuntFromServerToClientData
                                {
                                    GracePeriod = 0,
                                    PlayerId = id,
                                    Playing = true,
                                    PropHuntTeam = (byte)PropHuntTeam.Hunters,
                                }, id);

                            return;
                        }
                    }
                    
                    sender.BroadcastSingleData(
                        FromServerToClientPackets.PlayerDeath,
                        new PlayerDeathFromServerToClientData
                        {
                            PlayerId = id,
                            HuntersRemaining = HuntersAlive,
                            HuntersTotal = TotalHunters,
                            PropsRemaining = PropsAlive,
                            PropsTotal = TotalProps,
                        }
                    );
                }
            );

            serverApi.ServerManager.PlayerConnectEvent += player =>
            {
                PropHuntTeam team;
                if (HuntersAlive > PropsAlive)
                {
                    team = PropHuntTeam.Props;
                    _allHunters.Add(player);
                    _livingProps.Add(player);
                }
                else if (PropsAlive > HuntersAlive)
                {
                    team = PropHuntTeam.Hunters;
                    _allProps.Add(player);
                    _livingHunters.Add(player);
                }
                else
                {
                    var teamChoices = new[] { PropHuntTeam.Hunters, PropHuntTeam.Props };
                    var rand = new Random();
                    team = teamChoices[rand.Next(0, 2)];
                }

                sender.SendSingleData(
                    FromServerToClientPackets.SetPlayingPropHunt,
                    new SetPlayingPropHuntFromServerToClientData
                    {
                        Playing = _roundStarted,
                        PropHuntTeam = (byte)team,
                        GracePeriod = (int)(dueTimeGrace - DateTime.Now).TotalSeconds,
                        RoundTime = (int)(dueTimeRound - DateTime.Now).TotalSeconds,
                    }, player.Id);
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
                        _roundStarted = false;
                        roundTimer.Stop();
                        intervalTimer.Stop();
                        sender.BroadcastSingleData(
                            FromServerToClientPackets.EndRound,
                            new EndRoundFromServerToClientData
                            {
                                HuntersWin = true,
                            }
                        );
                        return;
                    }
                }
                else if (disconnectedHunter != null)
                {
                    _livingHunters.Remove(disconnectedHunter);

                    if (HuntersAlive <= 0)
                    {
                        _roundStarted = false;
                        roundTimer.Stop();
                        intervalTimer.Stop();
                        sender.BroadcastSingleData(
                            FromServerToClientPackets.EndRound,
                            new EndRoundFromServerToClientData
                            {
                                HuntersWin = false,
                            }
                        );
                        return;
                    }
                }

                sender.BroadcastSingleData(
                    FromServerToClientPackets.PlayerLeftGame,
                    new PlayerLeftGameFromServerToClientData
                    {
                        PlayerId = player.Id,
                        Username = player.Username,
                        HuntersRemaining = HuntersAlive,
                        HuntersTotal = TotalHunters,
                        PropsRemaining = PropsAlive,
                        PropsTotal = TotalProps,
                    });
            };
        }
    }
}