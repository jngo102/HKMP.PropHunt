using Hkmp.Api.Server;
using Hkmp.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Hkmp.Networking.Packet.Data;

namespace PropHunt.HKMP
{
    internal class PropHuntServerAddon : ServerAddon
    {
        protected override string Name => "Prop Hunt";
        protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override bool NeedsNetwork => true;

        public static PropHuntServerAddon Instance { get; private set; }

        public override void Initialize(IServerApi serverApi)
        {
            Instance = this;
            
            // A collection of all currently alive players on the Props team
            List<IServerPlayer> livingProps = new();

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

            // Timer that handles updating every player's timer each second
            var intervalTimer = new Timer();
            intervalTimer.Interval = 1000;
            intervalTimer.AutoReset = true;

            roundTimer.Elapsed += (obj, e) =>
            {
                intervalTimer.Stop();

                sender.BroadcastSingleData(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
                {
                    HuntersWin = livingProps.Count <= 0,
                });
            };

            intervalTimer.Elapsed += (obj, e) =>
            {
                sender.BroadcastSingleData(FromServerToClientPackets.UpdateRoundTimer, new UpdateRoundTimerFromServerToClientData
                {
                    TimeRemaining = (int)(dueTimeRound - DateTime.Now).TotalSeconds,
                });
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
                    sender.SendSingleData(FromServerToClientPackets.SendPropSprite, new PropSpriteFromServerToClientData
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
                    sender.SendSingleData(FromServerToClientPackets.SendPropPositionXY, new PropPositionXYFromServerToClientData
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
                    sender.SendSingleData(FromServerToClientPackets.SendPropPositionZ, new PropPositionZFromServerToClientData
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
                    sender.SendSingleData(FromServerToClientPackets.SendPropRotation, new PropRotationFromServerToClientData
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
                    sender.SendSingleData(FromServerToClientPackets.SendPropScale, new PropScaleFromServerToClientData
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
                    bool playing = packetData.Playing;
                    if (playing)
                    {
                        var players = serverApi.ServerManager.Players.ToList();
                        players = players.OrderBy(_ => Guid.NewGuid()).ToList();
                        int halfCount = players.Count / 2;
                        var hunters = players.GetRange(0, halfCount);
                        var props = players.GetRange(halfCount, players.Count - halfCount);
                        livingProps.Clear();
                        livingProps.AddRange(props);

                        sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Hunters,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, hunters.Select(hunter => hunter.Id).ToArray());

                        sender.SendSingleData(FromServerToClientPackets.SetPlayingPropHunt,
                            new SetPlayingPropHuntFromServerToClientData
                            {
                                PropHuntTeam = (byte)PropHuntTeam.Props,
                                PlayerId = id,
                                Playing = true,
                                GracePeriod = packetData.GracePeriod,
                                RoundTime = packetData.RoundTime,
                            }, props.Select(prop => prop.Id).ToArray());

                        roundTimer.Interval = packetData.RoundTime * 1000;
                        roundTimer.Start();
                        dueTimeRound = DateTime.Now.AddMilliseconds(roundTimer.Interval);

                        intervalTimer.Start();
                    }
                    else
                    {
                        roundTimer.Stop();
                        intervalTimer.Stop();

                        sender.BroadcastSingleData(FromServerToClientPackets.SetPlayingPropHunt,
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
                (id, packetData) =>
                {

                    var deadProp = livingProps.FirstOrDefault(prop => prop.Id == id);
                    if (deadProp != null) livingProps.Remove(deadProp);

                    if (livingProps.Count <= 0)
                    {
                        roundTimer.Stop();
                        intervalTimer.Stop();
                        sender.BroadcastSingleData(FromServerToClientPackets.EndRound, new EndRoundFromServerToClientData
                        {
                            HuntersWin = true,
                        });
                        return;
                    }

                    sender.BroadcastSingleData(FromServerToClientPackets.PlayerDeath,
                        new PlayerDeathFromServerToClientData
                        {
                            PlayerId = id,
                        }
                    );
                }
            );
        }
    }
}