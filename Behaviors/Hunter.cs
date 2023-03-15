using System.Collections.Generic;
using UnityEngine;

namespace PropHunt.Behaviors
{
    internal class Hunter : MonoBehaviour
    {
        private List<PlayMakerFSM> _healthDisplays;
        private PlayerData _pd;

        private void Awake()
        {
            _healthDisplays = new List<PlayMakerFSM>();
            _pd = PlayerData.instance;

            var healthParent = GameCameras.instance.hudCanvas.transform.Find("Health");
            for (int healthNum = 1; healthNum <= 11; healthNum++)
            {
                var health = healthParent.Find($"Health {healthNum}").gameObject;
                _healthDisplays.Add(health.LocateMyFSM("health_display"));
            }
        }

        private void OnDisable()
        {
            On.HeroController.CanFocus -= RemoveFocus;
        }

        private void OnEnable()
        {
            _pd.maxHealth = 10;
            _pd.maxHealthBase = 10;
            HeroController.instance.MaxHealth();
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            HeroController.instance.StartAnimationControl();
            On.HeroController.CanFocus += RemoveFocus;
        }

        private bool RemoveFocus(On.HeroController.orig_CanFocus orig, HeroController self) => false;

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