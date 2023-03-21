using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace PropHunt.Server
{
    /// <summary>
    /// Configuration for the server.
    /// </summary>
    internal class ServerSettings
    {
        /// <summary>
        /// The name of the file to save the server settings to.
        /// </summary>
        private const string FileName = "prophunt_settings.json";

        /// <summary>
        /// Whether games are automatically started.
        /// </summary>
        [JsonProperty("automated")]
        public bool Automated;

        /// <summary>
        /// The amount of grace time in seconds once a round starts automatically.
        /// </summary>
        [JsonProperty("grace_time_seconds")]
        public byte GraceTimeSeconds = 20;

        /// <summary>
        /// The amount of time in a round in seconds during an automated round.
        /// </summary>
        [JsonProperty("round_time_seconds")]
        public ushort RoundTimeSeconds = 380;

        /// <summary>
        /// The amount of time between rounds in seconds, if games are automated.
        /// </summary>
        [JsonProperty("seconds_between_rounds")]
        public int SecondsBetweenRounds = 90;

        /// <summary>
        /// Save server settings to the local disk.
        /// </summary>
        public void SaveToFile()
        {
            var dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dirPath == null) return;

            var settingsPath = Path.Combine(dirPath, FileName);
            var settingsJson = JsonConvert.SerializeObject(this, Formatting.Indented);

            try
            {
                File.WriteAllText(settingsPath, settingsJson);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Load server settings from the local disk.
        /// </summary>
        /// <returns>The loaded server settings, or a new settings instance if an error occurs.</returns>
        public static ServerSettings LoadFromFile()
        {
            var dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dirPath == null)
            {
                return new ServerSettings();
            }

            var settingsPath = Path.Combine(dirPath, FileName);
            if (!File.Exists(settingsPath))
            {
                var settings = new ServerSettings();
                settings.SaveToFile();
                return settings;
            }

            try
            {
                var fileContents = File.ReadAllText(settingsPath);
                var settings = JsonConvert.DeserializeObject<ServerSettings>(fileContents);
                return settings ?? new ServerSettings();
            }
            catch
            {
                return new ServerSettings();
            }
        }
    }
}
