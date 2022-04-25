﻿using Hkmp.Api.Command.Client;
using PropHunt.Behaviors;
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

        private string[] _activateCommands = { "on", "true", "yes", "activate", "enable", "start", "begin" };

        private string[] _deactivateCommands = { "off", "false", "no", "deactivate", "disable", "stop", "end" };


        public void Execute(string[] arguments)
        {
            var propHuntInstance = PropHuntClientAddon.Instance;
            var sender = propHuntInstance.PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(propHuntInstance);

            var propManager = HeroController.instance.GetComponent<LocalPropManager>();

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                propManager.enabled = true;
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = true,
                });
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                propManager.enabled = false;
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = false,
                });
            }
        }
    }
}