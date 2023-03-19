using PropHunt.HKMP;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages starting and ending a round.
    /// </summary>
    internal static class RoundManager
    {
        /// <summary>
        /// Start a new round.
        /// </summary>
        /// <param name="graceTime">The amount of initial grace time in seconds.</param>
        /// <param name="roundTime">The amount of time in the round in seconds.</param>
        public static void StartRound(byte graceTime, ushort roundTime)
        {
            ClientNetManager.SendPacket(
                FromClientToServerPackets.StartRound,
                new StartRoundFromClientToServerData
                {
                    GraceTime = graceTime,
                    RoundTime = roundTime,
                });
        }

        /// <summary>
        /// End the current round.
        /// </summary>
        public static void EndRound()
        {
            ClientNetManager.SendPacket(
                FromClientToServerPackets.EndRound,
                new EndRoundFromClientToServerData());
        }
    }
}
