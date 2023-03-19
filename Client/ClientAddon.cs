using Hkmp.Api.Client;
using ILogger = Hkmp.Logging.ILogger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropHunt.Client
{
    /// <summary>
    /// The prop hunt HKMP client addon.
    /// </summary>
    internal class PropHuntClientAddon : ClientAddon
    {
        /// <summary>
        /// Instantiate a new logger that may be used in the addon.
        /// </summary>
        public new ILogger Logger => base.Logger;

        /// <inheritdoc />
        protected override string Name => Constants.NAME;

        /// <inheritdoc />
        protected override string Version => Constants.Version;

        /// <inheritdoc />
        public override bool NeedsNetwork => true;

        /// <inheritdoc />
        public override void Initialize(IClientApi clientApi)
        {   
            new ClientGameManager(this, clientApi).Initialize();
        }
    }
}