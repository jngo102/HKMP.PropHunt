using HkmpPouch;

namespace PropHunt.Events
{
    internal class AssignTeamEvent : PipeEvent
    {
        internal static char[] Separator = { '|' };

        internal static string Name = "AssignTeam";
        
        public bool IsHunter;
        public bool InGrace;
        
        public override string GetName() => Name;

        public override string ToString() => $"{IsHunter}{Separator[0]}{InGrace}";
    }

    internal class AssignTeamEventFactory : IEventFactory
    {
        public static AssignTeamEventFactory Instance = new();

        public string GetName() => AssignTeamEvent.Name;

        public PipeEvent FromSerializedString(string serializedData)
        {
            var @event = new AssignTeamEvent();
            var split = serializedData.Split(AssignTeamEvent.Separator);
            @event.IsHunter = bool.Parse(split[0]);
            @event.InGrace = bool.Parse(split[1]);
            return @event;
        }
    }
}
