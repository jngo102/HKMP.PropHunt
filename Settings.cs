using InControl;

namespace PropHunt
{
    public class GlobalSettings
    {
        /// <summary>
        /// Key for selecting a prop.
        /// </summary>
        public int SelectKey { get; set; } = (int)Key.M;

        /// <summary>
        /// Key for translating the prop along the horizontal and vertical axes.
        /// </summary>
        public int TranslateXYKey { get; set; } = (int)Key.Y;
        
        /// <summary>
        /// Key for translating the prop along the Z-axis.
        /// </summary>
        public int TranslateZKey { get; set; } = (int)Key.U;

        /// <summary>
        /// Key for rotating the prop.
        /// </summary>
        public int RotateKey { get; set; } = (int)Key.R;

        /// <summary>
        /// Key for scaling the prop.
        /// </summary>
        public int ScaleKey { get; set; } = (int)Key.C;

        /// <summary>
        /// Controller button for selecting a prop.
        /// </summary>
        public int SelectButton { get; set; } = (int)InputControlType.DPadY;

        /// <summary>
        /// Controller button for translating the prop along the horizontal and vertical axes.
        /// </summary>
        public int TranslateXYButton { get; set; } = (int)InputControlType.DPadUp;

        /// <summary>
        /// Controller button for translating the prop along the Z-axis.
        /// </summary>
        public int TranslateZButton { get; set; } = (int)InputControlType.DPadLeft;

        /// <summary>
        /// Controller button for rotating the prop.
        /// </summary>
        public int RotateButton { get; set; } = (int)InputControlType.DPadDown;

        /// <summary>
        /// Controller button for scaling the prop.
        /// </summary>
        public int ScaleButton { get; set; } = (int)InputControlType.DPadRight;
    }
}