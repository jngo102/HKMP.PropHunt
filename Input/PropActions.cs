using InControl;

namespace PropHunt.Input
{
    internal class PropActions : PlayerActionSet
    {
        public PlayerAction Select;
        public PlayerAction TranslateXY;
        public PlayerAction TranslateZ;
        public PlayerAction Rotate;
        public PlayerAction Scale;

        public PropActions()
        {
            Select = CreatePlayerAction("Select");
            TranslateXY = CreatePlayerAction("Translate XY");
            TranslateXY.StateThreshold = 0.3f;
            TranslateZ = CreatePlayerAction("Translate Z");
            TranslateZ.StateThreshold = 0.3f;
            Rotate = CreatePlayerAction("Rotate");
            Rotate.StateThreshold = 0.3f;
            Scale = CreatePlayerAction("Scale");
            Scale.StateThreshold = 0.3f;
        }
    }
}
