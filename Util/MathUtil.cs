namespace PropHunt.Util
{
    /// <summary>
    /// Contains extension methods for math operations.
    /// </summary>
    internal static class MathUtil
    {
        /// <summary>
        /// Maps a value from one range to another.
        /// </summary>
        /// <param name="value">The value to map.</param>
        /// <param name="inMin">The minimum value of the input range.</param>
        /// <param name="inMax">The maximum value of the input range.</param>
        /// <param name="outMin">The minimum value of the output range.</param>
        /// <param name="outMax">The maximum value of the output range.</param>
        /// <returns>The mapped value of the input.</returns>
        public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }
    }
}