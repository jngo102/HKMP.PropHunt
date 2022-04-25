using InControl;
using Modding;
using UnityEngine;

namespace PropHunt.Input
{
    [RequireComponent(typeof(GameManager))]
    internal class PropInputHandler : MonoBehaviour
    {
        public static PropInputHandler Instance;

        public PropActions InputActions;

        private void Awake()
        {
            Instance = this;
            InputActions = new PropActions();
            SetupBindings();
        }

        private void Update()
        {
            if (InputActions.Select.GetKeyOrMouseBinding().Key != Key.None &&
                InputActions.Select.GetKeyOrMouseBinding().Key != (Key)PropHunt.Instance.Settings.SelectKey)
            {
                PropHunt.Instance.Settings.SelectKey = (int)InputActions.Select.GetKeyOrMouseBinding().Key;
            }

            if (InputActions.TranslateXY.GetKeyOrMouseBinding().Key != Key.None &&
                InputActions.TranslateXY.GetKeyOrMouseBinding().Key != (Key)PropHunt.Instance.Settings.TranslateXYKey)
            {
                PropHunt.Instance.Settings.TranslateXYKey = (int)InputActions.TranslateXY.GetKeyOrMouseBinding().Key;
            }

            if (InputActions.TranslateZ.GetKeyOrMouseBinding().Key != Key.None &&
                InputActions.TranslateZ.GetKeyOrMouseBinding().Key != (Key)PropHunt.Instance.Settings.TranslateZKey)
            {
                PropHunt.Instance.Settings.TranslateZKey = (int)InputActions.TranslateZ.GetKeyOrMouseBinding().Key;
            }

            if (InputActions.Rotate.GetKeyOrMouseBinding().Key != Key.None &&
                InputActions.Rotate.GetKeyOrMouseBinding().Key != (Key)PropHunt.Instance.Settings.RotateKey)
            {
                PropHunt.Instance.Settings.RotateKey = (int)InputActions.Rotate.GetKeyOrMouseBinding().Key;
            }

            if (InputActions.Scale.GetKeyOrMouseBinding().Key != Key.None &&
                InputActions.Scale.GetKeyOrMouseBinding().Key != (Key)PropHunt.Instance.Settings.ScaleKey)
            {
                PropHunt.Instance.Settings.ScaleKey = (int)InputActions.Scale.GetKeyOrMouseBinding().Key;
            }
        }

        public void SetupBindings()
        {
            InputActions.Select.AddDefaultBinding((Key)PropHunt.Instance.Settings.SelectKey);
            InputActions.TranslateXY.AddDefaultBinding((Key)PropHunt.Instance.Settings.TranslateXYKey);
            InputActions.TranslateZ.AddDefaultBinding((Key)PropHunt.Instance.Settings.TranslateZKey);
            InputActions.Rotate.AddDefaultBinding((Key)PropHunt.Instance.Settings.RotateKey);
            InputActions.Scale.AddDefaultBinding((Key)PropHunt.Instance.Settings.ScaleKey);
        }
    }
}
