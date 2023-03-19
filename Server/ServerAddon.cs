using Hkmp.Api.Server;
using Hkmp.Logging;

namespace PropHunt.Server
{
    /// <summary>
    /// The prop hunt HKMP server addon.
    /// </summary>
    internal class PropHuntServerAddon : ServerAddon
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
        public override void Initialize(IServerApi serverApi)
        {
            new ServerGameManager(this, serverApi).Initialize();
        }
    }
}