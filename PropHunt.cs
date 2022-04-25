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

        public float LargestSpriteDiagonalLength;
        public float SmallestSpriteDiagonalLength;

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

            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            LargestSpriteDiagonalLength = sprites.Select(sprite => sprite.bounds.size.magnitude).Max();
            SmallestSpriteDiagonalLength = sprites.Select(sprite => sprite.bounds.size.magnitude).Min();

            GameManager.instance.gameObject.AddComponent<PropInputHandler>();
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

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