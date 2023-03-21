using Hkmp.Api.Command.Client;
using System.Linq;
using PropHunt.Client;

namespace PropHunt.HKMP
{
    /// <summary>
    /// Commands for the prop hunt game mode.
    /// </summary>
    internal class PropHuntCommand : IClientCommand
    {
        /// <inheritdoc />
        public string Trigger => "prophunt";

        /// <inheritdoc />
        public string[] Aliases { get; } =
        {
            "PropHunt",  "/PropHunt",  @"\PropHunt",
            "prophunt",  "/prophunt",  @"\prophunt",
            "prop",      "/prop",      @"\prop",
        };

        /// <summary>
        /// Command parameters that signal a request to start a new round.
        /// </summary>
        private readonly string[] _activateCommands = { "on", "true", "yes", "activate", "enable", "start", "begin", "restart" };

        /// <summary>
        /// Command parameters that signal a request to stop the current round.
        /// </summary>
        private readonly string[] _deactivateCommands = { "off", "false", "no", "deactivate", "disable", "stop", "end" };

        private readonly string[] _automateCommands = { "auto", "automate" };

        /// <inheritdoc />
        public void Execute(string[] arguments)
        {
            byte graceTime= 15;
            ushort roundTime = 120;
            ushort secondsBetweenRounds = 60;
            if (arguments.Length > 3)
            {
                if (!ushort.TryParse(arguments[3], out roundTime))
                {
                    TextManager.DisplayDreamMessage("Please provide a number of seconds for the round time.");
                    return;
                }
            }
            if (arguments.Length > 2)
            {
                if (!byte.TryParse(arguments[2], out graceTime))
                {
                    TextManager.DisplayDreamMessage("Please provide a number of seconds for the grace time.");
                    return;
                }
            }

            if (_activateCommands.Contains(arguments[1].ToLower()))
            {
                ClientGameManager.StartRound(graceTime, roundTime);
            }
            else if (_deactivateCommands.Contains(arguments[1].ToLower()))
            {
                ClientGameManager.EndRound();
            }
            else if (_automateCommands.Contains(arguments[1].ToLower()))
            {
                if (arguments.Length > 4)
                {
                    if (!ushort.TryParse(arguments[4], out secondsBetweenRounds))
                    {
                        TextManager.DisplayDreamMessage("Please provide a number of seconds for the time between rounds.");
                        return;
                    }
                }

                ClientGameManager.ToggleAutomation(graceTime, roundTime, secondsBetweenRounds);
            }
        }
    }
}
