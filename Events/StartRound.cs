using System.Globalization;
using HkmpPouch;

namespace PropHunt.Events
{
    internal class StartRoundEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "StartGame";
        
        public uint GracePeriod;
        public uint RoundTime;

        public override string GetName() => Name;

        public override string ToString() => $"{GracePeriod}{Separator[0]}{RoundTime}";
    }

    internal class StartRoundEventFactory : IEventFactory
    {
        public static StartRoundEventFactory Instance = new();

        public string GetName() => StartRoundEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new StartRoundEvent();
            var split = serializedData.Split(StartRoundEvent.Separator);
            @event.GracePeriod = uint.Parse(split[0], CultureInfo.InvariantCulture);
            @event.RoundTime = uint.Parse(split[1], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
