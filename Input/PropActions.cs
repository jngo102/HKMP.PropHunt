using InControl;

namespace PropHunt.Input
{
    /// <summary>
    /// A custom player action set for transforming a prop.
    /// </summary>
    public class PropActions : PlayerActionSet
    {
        public PlayerAction SelectKey { get; private set; }
        public PlayerAction TranslateXYKey { get; private set; }
        public PlayerAction TranslateZKey { get; private set; }
        public PlayerAction RotateKey { get; private set; }
        public PlayerAction ScaleKey { get; private set; }

        public PlayerAction SelectButton { get; private set; }
        public PlayerAction TranslateXYButton { get; private set; }
        public PlayerAction TranslateZButton { get; private set; }
        public PlayerAction RotateButton { get; private set; }
        public PlayerAction ScaleButton { get; private set; }

        public PropActions()
        {
            SelectKey = CreatePlayerAction("Select Key");
            TranslateXYKey = CreatePlayerAction("Translate XY Key");
            TranslateXYKey.StateThreshold = 0.3f;
            TranslateZKey = CreatePlayerAction("Translate Z Key");
            TranslateZKey.StateThreshold = 0.3f;
            RotateKey = CreatePlayerAction("Rotate Key");
            RotateKey.StateThreshold = 0.3f;
            ScaleKey = CreatePlayerAction("Scale Key");
            ScaleKey.StateThreshold = 0.3f;

            SelectButton = CreatePlayerAction("Select Button");
            TranslateXYButton = CreatePlayerAction("Translate XY Button");
            TranslateXYButton.StateThreshold = 0.3f;
            TranslateZButton = CreatePlayerAction("Translate Z Button");
            TranslateZButton.StateThreshold = 0.3f;
            RotateButton = CreatePlayerAction("Rotate Button");
            RotateButton.StateThreshold = 0.3f;
            ScaleButton = CreatePlayerAction("Scale Button");
            ScaleButton.StateThreshold = 0.3f;
        }

        public bool SelectWasPressed()
        {
            return SelectKey.WasPressed || SelectButton.WasPressed;
        }

        public bool TranslateXYWasPressed()
        {
            return TranslateXYKey.WasPressed || TranslateXYButton.WasPressed;        }

        public bool TranslateZWasPressed()
        {
            return TranslateZKey.WasPressed || TranslateZButton.WasPressed;
        }

        public bool RotateWasPressed()
        {
            return RotateKey.WasPressed || RotateButton.WasPressed;
        }

        public bool ScaleWasPressed()
        {
            return ScaleKey.WasPressed || ScaleButton.WasPressed;
        }
    }
}
