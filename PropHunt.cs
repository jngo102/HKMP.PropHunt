using Hkmp.Api.Client;
using Hkmp.Api.Server;
using InControl;
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

        private PropActions _inputActions;

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

            var inputHandler = GameManager.instance.gameObject.AddComponent<PropInputHandler>();
            _inputActions = inputHandler.InputActions;

            GameCameras.instance.hudCanvas.AddComponent<UIPropHunt>();
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal()
        {
            // Save input settings
            Settings.SelectKey = (int)_inputActions.GetPlayerActionByName("Select").GetKeyOrMouseBinding().Key;
            Settings.TranslateXYKey = (int)_inputActions.GetPlayerActionByName("Translate XY").GetKeyOrMouseBinding().Key;
            Settings.TranslateZKey = (int)_inputActions.GetPlayerActionByName("Translate Z").GetKeyOrMouseBinding().Key;
            Settings.RotateKey = (int)_inputActions.GetPlayerActionByName("Rotate").GetKeyOrMouseBinding().Key;
            Settings.ScaleKey = (int)_inputActions.GetPlayerActionByName("Scale").GetKeyOrMouseBinding().Key;
            Settings.SelectButton = (int)_inputActions.GetPlayerActionByName("Select").GetControllerButtonBinding();
            Settings.TranslateXYButton = (int)_inputActions.GetPlayerActionByName("Translate XY").GetControllerButtonBinding();
            Settings.TranslateZButton = (int)_inputActions.GetPlayerActionByName("Translate Z").GetControllerButtonBinding();
            Settings.RotateButton = (int)_inputActions.GetPlayerActionByName("Rotate").GetControllerButtonBinding();
            Settings.ScaleButton = (int)_inputActions.GetPlayerActionByName("Scale").GetControllerButtonBinding();

            return Settings;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            _menu ??= new Menu("Prop Hunt", new Element[] {
                Blueprints.KeyAndButtonBind(
                    "Select Prop",
                    _inputActions.Select,
                    _inputActions.Select
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop Around",
                    _inputActions.TranslateXY,
                    _inputActions.TranslateXY
                ),
                Blueprints.KeyAndButtonBind(
                    "Move Prop In/Out",
                    _inputActions.TranslateZ,
                    _inputActions.TranslateZ
                ),
                Blueprints.KeyAndButtonBind(
                    "Rotate Prop",
                    _inputActions.Rotate,
                    _inputActions.Rotate
                ),
                Blueprints.KeyAndButtonBind(
                    "Grow/Shrink Prop",
                    _inputActions.Scale,
                    _inputActions.Scale
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