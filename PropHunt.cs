using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.HKMP;
using PropHunt.Input;
using PropHunt.UI;
using Satchel.BetterMenus;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PropHunt
{
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        internal static PropHunt Instance { get; private set; }

        private PropHuntClientAddon _clientAddon;
        private PropHuntServerAddon _serverAddon;

        private Menu _menu;

        public GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base("Prop Hunt")
        {
        }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance ??= this;

            _clientAddon = new PropHuntClientAddon();
            _serverAddon = new PropHuntServerAddon();

            ClientAddon.RegisterAddon(_clientAddon);
            ServerAddon.RegisterAddon(_serverAddon);

            GameManager.instance.gameObject.AddComponent<PropInputHandler>();
            GameCameras.instance.hudCanvas.AddComponent<UIPropHunt>();
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            _menu ??= new Menu("Prop Hunt", new Element[] {
                Blueprints.KeyAndButtonBind(
                    "Select Prop",
                    PropInputHandler.Instance.InputActions.Select,
                    PropInputHandler.Instance.InputActions.Select
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop Around",
                    PropInputHandler.Instance.InputActions.TranslateXY,
                    PropInputHandler.Instance.InputActions.TranslateXY
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop In/Out",
                    PropInputHandler.Instance.InputActions.TranslateZ,
                    PropInputHandler.Instance.InputActions.TranslateZ
                ),
                new KeyBind(
                    "Rotate Prop",
                    PropInputHandler.Instance.InputActions.Rotate
                ),
                Blueprints.KeyAndButtonBind(
                    "Grow/Shrink Prop",
                    PropInputHandler.Instance.InputActions.Scale,
                    PropInputHandler.Instance.InputActions.Scale
                ),
            });

            return _menu.GetMenuScreen(modListMenu);
        }

        public bool ToggleButtonInsideMenu { get; }
    }
}