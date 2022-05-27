using InControl;
using Modding;
using UnityEngine;

namespace PropHunt.Input
{
    /// <summary>
    /// A custom input handler for transforming the local player's prop.
    /// </summary>
    [RequireComponent(typeof(GameManager))]
    internal class PropInputHandler : MonoBehaviour
    {
        public static PropInputHandler Instance;

        public PropActions InputActions { get; private set; }

        private GlobalSettings _settings;

        private void Awake()
        {
            Instance = this;
            InputActions = new PropActions();
            _settings = PropHunt.Instance.Settings;
            
            SetupBindings();
        }

        public void SetupBindings()
        {
            // Keyboard inputs
            InputActions.Select.AddKeyOrMouseBinding(new InputHandler.KeyOrMouseBinding((Key)_settings.SelectKey));
            InputActions.TranslateXY.AddKeyOrMouseBinding(new InputHandler.KeyOrMouseBinding((Key)_settings.TranslateXYKey));
            InputActions.TranslateZ.AddKeyOrMouseBinding(new InputHandler.KeyOrMouseBinding((Key)_settings.TranslateZKey));
            InputActions.Rotate.AddKeyOrMouseBinding(new InputHandler.KeyOrMouseBinding((Key)_settings.RotateKey));
            InputActions.Scale.AddKeyOrMouseBinding(new InputHandler.KeyOrMouseBinding((Key)_settings.ScaleKey));

            // Controller inputs
            InputActions.Select.AddInputControlType((InputControlType)_settings.SelectButton);
            InputActions.TranslateXY.AddInputControlType((InputControlType)_settings.TranslateXYButton);
            InputActions.TranslateZ.AddInputControlType((InputControlType)_settings.TranslateZButton);
            InputActions.Rotate.AddInputControlType((InputControlType)_settings.RotateButton);
            InputActions.Scale.AddInputControlType((InputControlType)_settings.ScaleButton);
        }
    }
}
