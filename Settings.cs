using InControl;

namespace PropHunt
{
    public class GlobalSettings
    {
        public int SelectKey { get; set; } = (int)Key.M;
        public int TranslateXYKey { get; set; } = (int)Key.Y;
        public int TranslateZKey { get; set; } = (int)Key.U;
        public int RotateKey { get; set; } = (int)Key.R;
        public int ScaleKey { get; set; } = (int)Key.C;
    }
}