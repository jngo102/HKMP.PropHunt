﻿using Hkmp.Api.Command.Client;
using System.Linq;

namespace PropHunt.HKMP
{
    internal class PropHuntCommand : IClientCommand
    {
        public string Trigger { get; } = "prophunt";

        public string[] Aliases { get; } = new[]
        {
            "PropHunt",  "/PropHunt",  @"\PropHunt",
            "prophunt",  "/prophunt",  @"\prophunt", 
            "prop",      "/prop",      @"\prop",
        };

        private string[] _activateCommands = { "on", "true", "yes", "activate", "enable", "start", "begin", "restart" };

        private string[] _deactivateCommands = { "off", "false", "no", "deactivate", "disable", "stop", "end" };


        public void Execute(string[] arguments)
        {
            var propHuntInstance = PropHuntClientAddon.Instance;
            var sender = propHuntInstance.PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(propHuntInstance);

            float gracePeriodArg = 15;
            if (arguments.Length > 2)
            {
                gracePeriodArg = float.Parse(arguments[2]);
            }

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = true,
                    GracePeriod = gracePeriodArg,
                });
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = false,
                    GracePeriod = gracePeriodArg,
                });
            }
        }
    }
}
