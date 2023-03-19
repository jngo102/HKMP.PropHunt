using System.Reflection;

namespace PropHunt
{
    /// <summary>
    /// Static class containing constant values for the prop hunt mod.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The name of the mod.
        /// </summary>
        public const string NAME = "PropHunt";

        /// <summary>
        /// The mod version.
        /// </summary>
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
