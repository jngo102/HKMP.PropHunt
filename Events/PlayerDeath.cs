using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class PlayerDeathEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "PlayerDeath";

        public ushort PlayerId;
        public ushort HuntersRemaining;
        public ushort HuntersTotal;
        public ushort PropsRemaining;
        public ushort PropsTotal;

        public override string GetName() => Name;

        public override string ToString() =>
            $"{PlayerId}{Separator[0]}{HuntersRemaining}{Separator[0]}{HuntersTotal}{Separator[0]}{PropsRemaining}{Separator[0]}{PropsTotal}";
    }

    internal class PlayerDeathEventFactory : IEventFactory
    {
        public static PlayerDeathEventFactory Instance = new();

        public string GetName() => PlayerDeathEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new PlayerDeathEvent();
            var split = serializedData.Split(PlayerDeathEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.HuntersRemaining = ushort.Parse(split[1], CultureInfo.InvariantCulture);
            @event.HuntersTotal = ushort.Parse(split[2], CultureInfo.InvariantCulture);
            @event.PropsRemaining = ushort.Parse(split[3], CultureInfo.InvariantCulture);
            @event.PropsTotal = ushort.Parse(split[4], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
