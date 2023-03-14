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
            byte graceTime= 15;
            ushort roundTime = 120;
            if (arguments.Length > 3)
            {
                roundTime = ushort.Parse(arguments[3]);
            }
            if (arguments.Length > 2)
            {
                graceTime = byte.Parse(arguments[2]);
            }

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                PropHuntClientAddon.StartRound(graceTime, roundTime);
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                PropHuntClientAddon.EndRound(false);
            }
        }
    }
}
