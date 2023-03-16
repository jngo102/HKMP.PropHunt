using System.Collections.Generic;
using PropHunt.Util;
using UnityEngine;

namespace PropHunt.Behaviors
{
    internal class Hunter : MonoBehaviour
    {
        private void OnDisable() => LoadoutUtil.RevertHunterLoadout();

        private void OnEnable() => LoadoutUtil.SetHunterLoadout();

        /// <summary>
        /// Show blanker and deactivate controls.
        /// </summary>
        public void BeginGracePeriod()
        {
            var hudCam = GameCameras.instance.hudCamera;
            var blanker = hudCam.transform.Find("2dtk Blanker").gameObject;
            blanker.LocateMyFSM("Blanker Control").SendEvent("FADE IN INSTANT");
            
            var hc = GetComponent<HeroController>();
            hc.IgnoreInput();
            hc.RelinquishControl();
            InputHandler.Instance.inputActions.quickMap.Enabled = false;
        }
    }
}