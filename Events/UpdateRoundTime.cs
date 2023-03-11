using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class UpdateRoundTimeEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdateRoundTime";

        public uint TimeRemaining;

        public override string GetName() => Name;

        public override string ToString() => $"{TimeRemaining}";
    }

    internal class UpdateRoundTimeEventFactory : IEventFactory
    {
        public static UpdateRoundTimeEventFactory Instance = new();

        public string GetName() => UpdateRoundTimeEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdateRoundTimeEvent();
            var split = serializedData.Split(UpdateRoundTimeEvent.Separator);
            @event.TimeRemaining = uint.Parse(split[0], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
