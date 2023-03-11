using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class UpdatePropScaleEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdatePropScale";

        public ushort PlayerId;
        public float Scale;

        public override string GetName() => Name;

        public override string ToString() => $"{PlayerId}{Separator[0]}{Scale}";
    }

    internal class UpdatePropScaleEventFactory : IEventFactory
    {
        public static UpdatePropScaleEventFactory Instance = new();

        public string GetName() => UpdatePropScaleEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdatePropScaleEvent();
            var split = serializedData.Split(UpdatePropScaleEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.Scale = float.Parse(split[1], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
