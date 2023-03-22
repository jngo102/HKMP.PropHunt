using UnityEngine;

namespace PropHunt.Client.Behaviors
{
    /// <summary>
    /// Behavior for the hunter.
    /// </summary>
    [RequireComponent(typeof(HeroController))]
    internal class Hunter : MonoBehaviour
    {
        private void OnDisable() => LoadoutManager.RevertHunterLoadout();

        private void OnEnable() => LoadoutManager.SetHunterLoadout();

        /// <summary>
        /// Show blanker and deactivate controls.
        /// </summary>
        public void BeginGracePeriod()
        {
            TextManager.ShowBlanker();
            
            var hc = GetComponent<HeroController>();
            hc.IgnoreInput();
            hc.RelinquishControl();
            InputHandler.Instance.inputActions.quickMap.Enabled = false;
        }
    }
}