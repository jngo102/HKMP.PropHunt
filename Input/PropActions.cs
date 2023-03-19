using InControl;

namespace PropHunt.Input
{
    /// <summary>
    /// A custom player action set for turning into and transforming a prop.
    /// </summary>
    public class PropActions : PlayerActionSet
    {
        /// <summary>
        /// The bindable key used to turn into a prop and back.
        /// </summary>
        public PlayerAction SelectKey { get; }

        /// <summary>
        /// The bindable key used to enable and disable translating a prop along the x- and y-axes.
        /// </summary>
        public PlayerAction TranslateXyKey { get; }

        /// <summary>
        /// The bindable key used to enable and disable translating a prop along the z-axis.
        /// </summary>
        public PlayerAction TranslateZKey { get; }

        /// <summary>
        /// The bindable key used to enable and disable rotating a prop.
        /// </summary>
        public PlayerAction RotateKey { get; }

        /// <summary>
        /// The bindable key used to enable and disable scaling a prop.
        /// </summary>
        public PlayerAction ScaleKey { get; }

        /// <summary>
        /// The bindable button used to turn into a prop and back.
        /// </summary>
        public PlayerAction SelectButton { get; }

        /// <summary>
        /// The bindable button used to enable and disable translating a prop along the x- and y-axes.
        /// </summary>
        public PlayerAction TranslateXyButton { get; }

        /// <summary>
        /// The bindable button used to enable and disable translating a prop along the z-axis.
        /// </summary>
        public PlayerAction TranslateZButton { get; }

        /// <summary>
        /// The bindable button used to enable and disable rotating a prop.
        /// </summary>
        public PlayerAction RotateButton { get; }

        /// <summary>
        /// The bindable button used to enable and disable scaling a prop.
        /// </summary>
        public PlayerAction ScaleButton { get; }

        /// <summary>
        /// Constructor for the prop actions class.
        /// </summary>
        public PropActions()
        {
            SelectKey = CreatePlayerAction("Select Key");
            TranslateXyKey = CreatePlayerAction("Translate Xy Key");
            TranslateXyKey.StateThreshold = 0.3f;
            TranslateZKey = CreatePlayerAction("Translate Z Key");
            TranslateZKey.StateThreshold = 0.3f;
            RotateKey = CreatePlayerAction("Rotate Key");
            RotateKey.StateThreshold = 0.3f;
            ScaleKey = CreatePlayerAction("Scale Key");
            ScaleKey.StateThreshold = 0.3f;

            SelectButton = CreatePlayerAction("Select Button");
            TranslateXyButton = CreatePlayerAction("Translate Xy Button");
            TranslateXyButton.StateThreshold = 0.3f;
            TranslateZButton = CreatePlayerAction("Translate Z Button");
            TranslateZButton.StateThreshold = 0.3f;
            RotateButton = CreatePlayerAction("Rotate Button");
            RotateButton.StateThreshold = 0.3f;
            ScaleButton = CreatePlayerAction("Scale Button");
            ScaleButton.StateThreshold = 0.3f;
        }

        /// <summary>
        /// Check whether the key or button to select or deselect a prop was pressed.
        /// </summary>
        /// <returns>Whether the select action was pressed.</returns>
        public bool SelectWasPressed()
        {
            return SelectKey.WasPressed || SelectButton.WasPressed;
        }

        /// <summary>
        /// Check whether the key or button to translate a prop along the x- and y-axes was pressed.
        /// </summary>
        /// <returns>Whether the translate action for the x- and y-axes was pressed.</returns>
        public bool TranslateXyWasPressed()
        {
            return TranslateXyKey.WasPressed || TranslateXyButton.WasPressed;        }

        /// <summary>
        /// Check whether the key or button to translate a prop along the z-axis was pressed.
        /// </summary>
        /// <returns>Whether the translate action for the z-axis was pressed.</returns>
        public bool TranslateZWasPressed()
        {
            return TranslateZKey.WasPressed || TranslateZButton.WasPressed;
        }

        /// <summary>
        /// Check whether the key or button to rotate a prop was pressed.
        /// </summary>
        /// <returns>Whether the rotate action was pressed.</returns>
        public bool RotateWasPressed()
        {
            return RotateKey.WasPressed || RotateButton.WasPressed;
        }

        /// <summary>
        /// Check whether the key or button to scale a prop was pressed.
        /// </summary>
        /// <returns>Whether the scale action was pressed.</returns>
        public bool ScaleWasPressed()
        {
            return ScaleKey.WasPressed || ScaleButton.WasPressed;
        }
    }
}
