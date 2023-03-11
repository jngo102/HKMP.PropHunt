using HkmpPouch;
using System.Globalization;

namespace PropHunt.Events
{
    internal class UpdatePropRotationEvent: PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdatePropRotation";

        public ushort PlayerId;
        public float Rotation;

        public override string GetName() => Name;

        public override string ToString() => $"{PlayerId}{Separator[0]}{Rotation}";
    }

    internal class UpdatePropRotationEventFactory : IEventFactory
    {
        public static UpdatePropRotationEventFactory Instance = new();

        public string GetName() => UpdatePropRotationEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdatePropRotationEvent();
            var split = serializedData.Split(UpdatePropRotationEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.Rotation = float.Parse(split[1], CultureInfo.InvariantCulture);
            return @event;
        }
    }
}
