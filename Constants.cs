using System.Reflection;

namespace PropHunt
{
    internal static class Constants
    {
        /// <summary>
        /// The name of the mod.
        /// </summary>
        public const string NAME = "PropHunt";

        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
