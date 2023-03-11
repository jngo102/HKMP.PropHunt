using Hkmp.Api.Command.Client;
using System.Linq;
using PropHunt.Events;

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
            var pipe = PropHunt.PipeClient;

            uint gracePeriod = 15;
            uint roundTime = 120;
            if (arguments.Length > 3)
            {
                roundTime = uint.Parse(arguments[3]);
            }
            if (arguments.Length > 2)
            {
                gracePeriod = uint.Parse(arguments[2]);
            }

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                pipe.SendToServer(new StartRoundEvent { GracePeriod = gracePeriod, RoundTime = roundTime });
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                pipe.SendToServer(new EndRoundEvent { HuntersWin = false });
            }
        }
    }
}
