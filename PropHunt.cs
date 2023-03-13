using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.HKMP;
using PropHunt.UI;
using Satchel.BetterMenus;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HkmpPouch;
using Modding.Utils;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace PropHunt
{
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        internal static PropHunt Instance { get; private set; }

        internal static PipeClient PipeClient;
        internal static PropHuntClientAddon Client;
        internal static PropHuntServerAddon Server;

        private Menu _menu;

        public Dictionary<string, Sprite> PropIcons { get; } = new();

        public GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base(Constants.NAME) { }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize()
        {
            Instance ??= this;

            LoadAssets();

            if (Server == null)
            {
                Server = new PropHuntServerAddon();
                ServerAddon.RegisterAddon(Server);
            }

            if (Client == null)
            {
                Client = new PropHuntClientAddon();
                ClientAddon.RegisterAddon(Client);
            }

            PipeClient ??= new PipeClient(Name);
                
            PipeClient.ServerCounterPartAvailable(serverAddonPresent =>
            {
                if (serverAddonPresent)
                {
                    
                }
            });

            GameCameras.instance.hudCanvas.GetOrAddComponent<UIPropHunt>();
            PipeClient.ClientApi?.CommandManager?.RegisterCommand(new PropHuntCommand());
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

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
                    Settings.Bindings.TranslateXYKey,
                    Settings.Bindings.TranslateXYButton
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