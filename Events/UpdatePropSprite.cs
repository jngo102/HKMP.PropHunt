using System.Globalization;
using HkmpPouch;

namespace PropHunt.Events
{
    internal class UpdatePropSpriteEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "UpdatePropSprite";

        public ushort PlayerId;
        public string SpriteName;

        public override string GetName() => Name;

        public override string ToString() => $"{PlayerId}{Separator[0]}{SpriteName}";
    }

    internal class UpdatePropSpriteEventFactory : IEventFactory
    {
        public static UpdatePropSpriteEventFactory Instance = new();

        public string GetName() => UpdatePropSpriteEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new UpdatePropSpriteEvent();
            var split = serializedData.Split(UpdatePropSpriteEvent.Separator);
            @event.PlayerId = ushort.Parse(split[0], CultureInfo.InvariantCulture);
            @event.SpriteName = split[1];
            return @event;
        }
    }
}
