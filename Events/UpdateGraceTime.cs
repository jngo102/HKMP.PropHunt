using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class UpdateGraceTimeEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdateGraceTime";

        public uint TimeRemaining;

        public override string GetName() => Name;

        public override string ToString() => $"{TimeRemaining}";
    }

    internal class UpdateGraceTimeEventFactory : IEventFactory
    {
        public static UpdateGraceTimeEventFactory Instance = new();

        public string GetName() => UpdateGraceTimeEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdateGraceTimeEvent();
            var split = serializedData.Split(UpdateGraceTimeEvent.Separator);
            @event.TimeRemaining= uint.Parse(split[0], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
