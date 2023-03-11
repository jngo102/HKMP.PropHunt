using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class UpdatePropPositionZEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdatePropPositionZ";

        public ushort PlayerId;
        public float Z;
        
        public override string GetName() => Name;

        public override string ToString() => $"{PlayerId}{Separator[0]}{Z}";
    }

    internal class UpdatePropPositionZEventFactory : IEventFactory
    {
        public static UpdatePropPositionZEventFactory Instance = new();

        public string GetName() => UpdatePropPositionZEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdatePropPositionZEvent();
            var split = serializedData.Split(UpdatePropPositionZEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.Z = float.Parse(split[1], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
