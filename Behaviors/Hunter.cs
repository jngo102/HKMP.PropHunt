using System.Collections;
using UnityEngine;

namespace PropHunt.Behaviors
{
    internal class Hunter : MonoBehaviour
    {
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