using UnityEngine;

namespace PropHunt.Behaviors
{
    internal class Hunter : MonoBehaviour
    {
        private void Awake()
        {

        }

        private void OnEnable()
        {
            var propManager = GetComponent<LocalPropManager>();
            if (propManager) propManager.enabled = false;
        }
    }
}