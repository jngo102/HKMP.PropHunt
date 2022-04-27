using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;

namespace PropHunt.HKMP
{
    internal class PropHuntServerAddon : ServerAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override bool NeedsNetwork => true;

        public static PropHuntServerAddon Instance { get; private set; }

        private IServerAddonNetworkSender<FromServerToClientPackets> _sender;
        private IServerAddonNetworkReceiver<FromClientToServerPackets> _receiver;

        /// <summary>
        /// The timer that handles the length of time that a round goes for.
        /// </summary>
        private Timer _roundTimer;

        /// <summary>
        /// The timer that handles updating every player's timer each second.
        /// </summary>
        private Timer _intervalTimer;

        private DateTime _dueTimeRound;
        private DateTime _dueTimeInterval;

        /// <summary>
        /// A collection of all currently alive players on the Props team.
        /// </summary>
        private List<IServerPlayer> _livingProps = new();

        public override void Initialize(IServerApi serverApi)
        {
            Instance = this;

            _sender = serverApi.NetServer.GetNetworkSender<FromServerToClientPackets>(Instance);
            _receiver = serverApi.NetServer.GetNetworkReceiver<FromClientToServerPackets>(Instance, InstantiatePacket);

            _roundTimer = new Timer();
            _roundTimer.AutoReset = false;

            _intervalTimer = new Timer();
            _intervalTimer.Interval = 1000;
            _intervalTimer.AutoReset = true;

            _roundTimer.Elapsed += OnRoundEnd;
            _intervalTimer.Elapsed += OnUpdateTime;

            _receiver.RegisterPacketHandler<PropSpriteFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropSprite,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var otherPlayers = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    _sender.SendSingleData(FromServerToClientPackets.SendPropSprite, new PropSpriteFromServerToClientData
                    {
                        PlayerId = id,
                        SpriteName = packetData.SpriteName,
                    }, otherPlayers);
                }
            );

            _receiver.RegisterPacketHandler<PropPositionXYFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionXY,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    _sender.SendSingleData(FromServerToClientPackets.SendPropPositionXY, new PropPositionXYFromServerToClientData
                    {
                        PlayerId = id,
                        PositionXY = packetData.PositionXY,
                    }, playersInScene);
                }
            );

            _receiver.RegisterPacketHandler<PropPositionZFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropPositionZ,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    _sender.SendSingleData(FromServerToClientPackets.SendPropPositionZ, new PropPositionZFromServerToClientData
                    {
                        PlayerId = id,
                        PositionZ = packetData.PositionZ,
                    }, playersInScene);
                }
            );

            _receiver.RegisterPacketHandler<PropRotationFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropRotation,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    _sender.SendSingleData(FromServerToClientPackets.SendPropRotation, new PropRotationFromServerToClientData
                    {
                        PlayerId = id,
                        Rotation = packetData.Rotation,
                    }, playersInScene);
                }
            );

            _receiver.RegisterPacketHandler<PropScaleFromClientToServerData>
            (
                FromClientToServerPackets.BroadcastPropScale,
                (id, packetData) =>
                {
                    var localPlayer = serverApi.ServerManager.GetPlayer(id);
                    var playersInScene = serverApi.ServerManager.Players
                        .Where(remotePlayer => remotePlayer.CurrentScene == localPlayer.CurrentScene && remotePlayer != localPlayer)
                        .Select(remotePlayer => remotePlayer.Id).ToArray();
                    _sender.SendSingleData(FromServerToClientPackets.SendPropScale, new PropScaleFromServerToClientData
                    {
                        PlayerId = id,
                        ScaleFactor = packetData.ScaleFactor,
                    }, playersInScene);
                }
            );

            _receiver.RegisterPacketHandler<SetPlayingPropHuntFromClientToServerData>
            (
                FromClientToServerPackets.SetPlayingPropHunt,
                (id, packetData) =>
                {
                    bool playing = packetData.Playing;
                    if (playing)
                    {
                        var players = serverApi.ServerManager.Players.ToList();
                        players = players.OrderBy(_ => Guid.NewGuid()).ToList();
                        int halfCount = players.Count / 2;
                        var hunters = players.GetRange(0, halfCount);
                        var props = players.GetRange(halfCount, players.Count - halfCount);
                        _livingProps.Clear();
                        _livingProps.AddRange(props);

                        _sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Hunters,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, hunters.Select(hunter => hunter.Id).ToArray());

                        _sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Props,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, props.Select(prop => prop.Id).ToArray());

                        _roundTimer.Interval = packetData.RoundTime * 1000;
                        _roundTimer.Start();
                        _dueTimeRound = DateTime.Now.AddMilliseconds(_roundTimer.Interval);

                        _intervalTimer.Start();
                        _dueTimeInterval = DateTime.Now.AddMilliseconds(_intervalTimer.Interval);
                    }
                    else
                    {
                        _roundTimer.Stop();
                        _intervalTimer.Stop();

                        _sender.BroadcastSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PlayerId = id,
                                Playing = false,
                            }
                        );
                    }
                }
            );

            _receiver.RegisterPacketHandler<PlayerDeathFromClientToServerData>
            (
                FromClientToServerPackets.PlayerDeath,
                (id, packetData) =>
                {

                    var deadProp = _livingProps.FirstOrDefault(prop => prop.Id == id);
                    if (deadProp != null) _livingProps.Remove(deadProp);

                    if (_livingProps.Count <= 0)
                    {
                        EndRound();
                        return;
                    }

                    _sender.BroadcastSingleData(FromServerToClientPackets.PlayerDeath,
                        new PlayerDeathFromServerToClientData
                        {
                            PlayerId = id,
                        }
                    );
                }
            );
        }

        /// <summary>
        /// End the currently active round; only called after all props are killed, so broadcast that hunters have won.
        /// </summary>
        private void EndRound()
        {
            _roundTimer.Stop();
            _intervalTimer.Stop();
            _sender.BroadcastSingleData(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
            {
                HuntersWin = true,
            });
        }

        private void OnRoundEnd(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _intervalTimer.Stop();

            _sender.BroadcastSingleData(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
            {
                HuntersWin = _livingProps.Count <= 0,
            });
        }

        private void OnUpdateTime(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _sender.BroadcastSingleData(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
            {
                TimeRemaining = (int)(_dueTimeRound - DateTime.Now).TotalSeconds, 
            });

            _dueTimeInterval = DateTime.Now.AddMilliseconds(_intervalTimer.Interval);
        }

        private static IPacketData InstantiatePacket(FromClientToServerPackets serverPacket)
        {
            switch (serverPacket)
            {
                case FromClientToServerPackets.BroadcastPropSprite:
                    return new PropSpriteFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionXY:
                    return new PropPositionXYFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropPositionZ:
                    return new PropPositionZFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropRotation:
                    return new PropRotationFromClientToServerData();
                case FromClientToServerPackets.BroadcastPropScale:
                    return new PropScaleFromClientToServerData();
                case FromClientToServerPackets.SetPlayingPropHunt:
                    return new SetPlayingPropHuntFromClientToServerData();
                case FromClientToServerPackets.PlayerDeath:
                    return new PlayerDeathFromClientToServerData();
                default:
                    return null;
            }
        }
    }
}