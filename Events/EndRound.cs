using HkmpPouch;

namespace PropHunt.Events
{
    internal class EndRoundEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "EndRound";

        public bool HuntersWin;
            
        public override string GetName() => Name;
        
        public override string ToString() => $"{HuntersWin}";
    }

    internal class EndRoundEventFactory : IEventFactory
    {
        public static EndRoundEventFactory Instance = new();

        public string GetName() => EndRoundEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new EndRoundEvent();
            var split = serializedData.Split(EndRoundEvent.Separator);
            @event.HuntersWin = bool.Parse(split[0]);
            return @event;
        }
    }
}
