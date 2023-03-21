using System;
using System.IO;
using System.Reflection;
using Modding;
using Newtonsoft.Json;
using ILogger = Hkmp.Logging.ILogger;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages player data so that all clients have the same skills, charms, etc.
    /// Taken and modified from https://github.com/Extremelyd1/HKMP-Tag/blob/master/Client/SaveManager.cs
    /// </summary>
    internal class SaveManager
    {
        /// <summary>
        /// An instance of the HKMP logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// An instance of the loaded complete save.
        /// </summary>
        private SaveGameData _loadedSave;

        /// <summary>
        /// Constructor for the save manager.
        /// </summary>
        /// <param name="logger">The logger to be passed to the save manager.</param>
        public SaveManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialize the save manager.
        /// </summary>
        public void Initialize()
        {
            ModHooks.NewGameHook += NewGame;
            ModHooks.AfterSavegameLoadHook += AfterLoadGame;
            ModHooks.BeforeSavegameSaveHook += BeforeSaveGame;
        }

        /// <summary>
        /// Called when a new game is started.
        /// </summary>
        private void NewGame()
        {
            _logger.Info("Starting new game, overwriting save game data.");
            
            OverwriteSave();
        }

        /// <summary>
        /// Called after the saved game is loaded.
        /// </summary>
        /// <param name="data"></param>
        private void AfterLoadGame(SaveGameData data)
        {
            _logger.Info("Saving loaded save game data.");

            _loadedSave = data;

            OverwriteSave();
        }

        /// <summary>
        /// Called just before the game is saved.
        /// </summary>
        /// <param name="data">The save data.</param>
        private void BeforeSaveGame(SaveGameData data)
        {
            _logger.Info("Restoring loaded save game data.");

            if (_loadedSave != null)
            {
                data.playerData = _loadedSave.playerData;
                data.sceneData = _loadedSave.sceneData;
            }
        }

        /// <summary>
        /// Overwrite the local save's data with a completed save.
        /// </summary>
        private void OverwriteSave()
        {
            var resourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("PropHunt.Client.Resources.Save.json");

            if (resourceStream == null)
            {
                _logger.Error("Failed to load completed save file.");
                return;
            }

            var saveString = new StreamReader(resourceStream).ReadToEnd();

            try
            {
                var completedSaveData = JsonConvert.DeserializeObject<SaveGameData>(saveString);
                var gameManager = GameManager.instance;
                gameManager.playerData = PlayerData.instance = completedSaveData.playerData;
                gameManager.sceneData = SceneData.instance = completedSaveData.sceneData;
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to deserialize completed save file: {e.GetType()}\t{e.Message}");
            }
        }
    }
}
