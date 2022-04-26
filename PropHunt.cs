using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.HKMP;
using PropHunt.Input;
using Satchel.BetterMenus;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PropHunt
{
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        internal static PropHunt Instance { get; private set; }

        private PropHuntClientAddon _clientAddon = new();
        private PropHuntServerAddon _serverAddon = new();

        private Menu _menu;

        public GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base("Prop Hunt")
        {
        }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance ??= this;

            ClientAddon.RegisterAddon(_clientAddon);
            ServerAddon.RegisterAddon(_serverAddon);
            
            GameManager.instance.gameObject.AddComponent<PropInputHandler>();

            ModHooks.LanguageGetHook += OnLanguageGet;
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

        private string OnLanguageGet(string key, string sheetTitle, string orig)
        {
            if (sheetTitle == "PROP_HUNT")
            {
                switch (key)
                {
                    case "HUNTER_MESSAGE": return "You are now a hunter!";
                    case "PROP_MESSAGE": return "You are now a prop!";
                }
            }

            return orig;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            var keybind = new KeyBind(
                "Select Prop",
                PropInputHandler.Instance.InputActions.Select
            );

            _menu ??= new Menu("Prop Hunt", new Element[] {
                new KeyBind(
                    "Select Prop",
                    PropInputHandler.Instance.InputActions.Select
                ),
                new KeyBind(
                    "Move Prop Around",
                    PropInputHandler.Instance.InputActions.TranslateXY
                ),
                new KeyBind(
                    "Move Prop In/Out",
                    PropInputHandler.Instance.InputActions.TranslateZ
                ),
                new KeyBind(
                    "Rotate Prop",
                    PropInputHandler.Instance.InputActions.Rotate
                ),
                new KeyBind(
                    "Shrink/Grow Prop",
                    PropInputHandler.Instance.InputActions.Scale
                ),
            });

            return _menu.GetMenuScreen(modListMenu);
        }

        public bool ToggleButtonInsideMenu { get; }
    }
}