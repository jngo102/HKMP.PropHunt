using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.HKMP;
using PropHunt.Input;
using PropHunt.UI;
using Satchel.BetterMenus;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace PropHunt
{
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        internal static PropHunt Instance { get; private set; }
        
        private PropHuntClientAddon _clientAddon;
        private PropHuntServerAddon _serverAddon;

        private Menu _menu;

        public Dictionary<string, Sprite> PropIcons { get; private set; } = new();

        public GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base("Prop Hunt")
        {
        }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize()
        {
            Instance ??= this;

            LoadAssets();

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
                Blueprints.KeyAndButtonBind(
                    "Rotate Prop",
                    PropInputHandler.Instance.InputActions.Rotate,
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

        private void LoadAssets()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.Contains("prophunt")) continue;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;

                    var bundle = AssetBundle.LoadFromStream(stream);

                    foreach (var icon in bundle.LoadAllAssets<Texture2D>())
                    {
                        var iconSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.one * 0.5f);
                        UObject.DontDestroyOnLoad(iconSprite);
                        PropIcons.Add(icon.name, iconSprite);
                    }

                    stream.Dispose();
                }
            }
        }
    }
}