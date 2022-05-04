using Hkmp.Api.Command.Client;
using System.Linq;

namespace PropHunt.HKMP
{
    internal class PropHuntCommand : IClientCommand
    {
        public string Trigger => "prophunt";

        public string[] Aliases { get; } =
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

            int gracePeriodArg = 15;
            int roundTime = 120;
            if (arguments.Length > 3)
            {
                roundTime = int.Parse(arguments[3]);
            }
            if (arguments.Length > 2)
            {
                gracePeriodArg = int.Parse(arguments[2]);
            }

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = true,
                    GracePeriod = gracePeriodArg,
                    RoundTime = roundTime,
                });
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                sender.SendSingleData(FromClientToServerPackets.SetPlayingPropHunt, new SetPlayingPropHuntFromClientToServerData()
                {
                    Playing = false,
                });
            }
        }
    }
}
