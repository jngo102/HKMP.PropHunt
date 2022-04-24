using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using PropHunt.HKMP;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PropHunt
{
    internal class PropHunt : Mod, IGlobalSettings<GlobalSettings>
    {
        internal static PropHunt Instance { get; private set; }

        private PropHuntClient _clientAddon = new();
        private PropHuntServer _serverAddon = new();

        public GlobalSettings Settings { get; private set; } = new();

        public PropHunt() : base("Prop Hunt") { }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance ??= this;

            ClientAddon.RegisterAddon(_clientAddon);
            ServerAddon.RegisterAddon(_serverAddon);
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;
    }
}