using System.Globalization;
using HkmpPouch;

namespace PropHunt.Events
{
    internal class UpdatePropPositionXYEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdatePropPositionXY";

        public ushort PlayerId;
        public float X;
        public float Y;

        public override string GetName() => Name;

        public override string ToString() => $"{PlayerId}{Separator[0]}{X}{Separator[0]}{Y}";
    }

    internal class UpdatePropPositionXYEventFactory : IEventFactory
    {
        public static UpdatePropPositionXYEventFactory Instance = new();

        public string GetName() => UpdatePropPositionXYEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdatePropPositionXYEvent();
            var split = serializedData.Split(UpdatePropPositionXYEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.X = float.Parse(split[1], CultureInfo.InvariantCulture);
            @event.Y = float.Parse(split[2], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
