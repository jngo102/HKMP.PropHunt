using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.Client;
using PropHunt.Server;
using Satchel.BetterMenus;

namespace PropHunt
{
    /// <summary>
    /// The main prop hunt mod class.
    /// </summary>
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        /// <summary>
        /// The prop hunt mod's menu.
        /// </summary>
        private Menu _menu;

        /// <summary>
        /// Global settings for the mod.
        /// </summary>
        public static GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base(Constants.NAME) { }

        /// <inheritdoc />
        public override string GetVersion() => Constants.Version;

        /// <inheritdoc />
        public override void Initialize()
        {
            ClientAddon.RegisterAddon(new PropHuntClientAddon());
            ServerAddon.RegisterAddon(new PropHuntServerAddon());
        }

        /// <inheritdoc />
        public void OnLoadGlobal(GlobalSettings settings) => Settings = settings;

        /// <inheritdoc />
        public GlobalSettings OnSaveGlobal() => Settings;

        /// <inheritdoc />
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            _menu ??= new Menu(Constants.NAME, new Element[] {
                Blueprints.KeyAndButtonBind(
                    "Select Prop",
                    Settings.Bindings.SelectKey,
                    Settings.Bindings.SelectButton
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop Around",
                    Settings.Bindings.TranslateXyKey,
                    Settings.Bindings.TranslateXyButton
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop In/Out",
                    Settings.Bindings.TranslateZKey,
                    Settings.Bindings.TranslateZButton
                ),
                Blueprints.KeyAndButtonBind(
                    "Rotate Prop",
                    Settings.Bindings.RotateKey,
                    Settings.Bindings.RotateButton
                ),
                Blueprints.KeyAndButtonBind(
                    "Grow/Shrink Prop",
                    Settings.Bindings.ScaleKey,
                    Settings.Bindings.ScaleButton
                ),
            });

            return _menu.GetMenuScreen(modListMenu);
        }

        /// <inheritdoc />
        public bool ToggleButtonInsideMenu { get; }
    }
}