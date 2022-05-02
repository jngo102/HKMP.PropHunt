using System.Collections;
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
            _pd.health = 10;
            _pd.maxHealth = 10;
            _pd.maxHealthBase = 10;
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            On.HeroController.CanFocus += RemoveFocus;
        }

        private bool RemoveFocus(On.HeroController.orig_CanFocus orig, HeroController self) => false;

        /// <summary>
        /// Deactivate controls, then wait for a certain amount of time before reactivating them.
        /// </summary>
        /// <param name="graceTime">The amount of time to wait in seconds</param>
        public void BeginGracePeriod(float graceTime)
        {
            StartCoroutine(GracePeriod());

            IEnumerator GracePeriod()
            {
                var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                var blankerCtrl = blanker.LocateMyFSM("Blanker Control");

                var hc = GetComponent<HeroController>();
                var actions = InputHandler.Instance.inputActions;

                blankerCtrl.SendEvent("FADE IN INSTANT");
                hc.IgnoreInput();
                hc.RelinquishControl();
                actions.quickMap.Enabled = false;

                yield return new WaitForSeconds(graceTime);

                blankerCtrl.SendEvent("FADE OUT INSTANT");
                hc.AcceptInput();
                hc.RegainControl();
                actions.quickMap.Enabled = true;
            }
        }
    }
}