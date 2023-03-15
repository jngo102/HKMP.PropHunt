using GlobalEnums;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PropHunt.UI
{
    internal class UIPropHunt : MonoBehaviour
    {
        private TextMeshPro _graceTextMesh;
        private TextMeshPro _roundTextMesh;
        private PlayMakerFSM _displayDreamMsg;

        private void Awake()
        {
            var graceTimer = new GameObject("Grace Timer")
            {
                layer = (int)PhysLayers.UI
            };

            var dreamMsg = GameCameras.instance.hudCamera.transform.Find("DialogueManager/Dream Msg").gameObject;
            foreach (var rend in dreamMsg.GetComponentsInChildren<Renderer>())
            {
                // Place dream msg above blanker so hunters can read that they are hunters.
                rend.sortingOrder = 11;
            }
            _displayDreamMsg = dreamMsg.LocateMyFSM("Display");

            graceTimer.transform.SetParent(transform.Find("Geo Counter"));
            graceTimer.transform.localPosition = new Vector3(-9, 4.85f, 40);
            graceTimer.transform.localScale = Vector3.one * 0.1527f;
            _graceTextMesh = graceTimer.AddComponent<TextMeshPro>();
            _graceTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _graceTextMesh.fontSize = 35;
            _graceTextMesh.text = "";
            _graceTextMesh.enableWordWrapping = false;
            var renderer = _graceTextMesh.renderer;
            renderer.sortingLayerName = "HUD";
            renderer.sortingOrder = 11;

            var roundTimer = new GameObject("Round Timer")
            {
                layer = (int)PhysLayers.UI
            };

            roundTimer.transform.SetParent(transform.Find("Geo Counter"));
            roundTimer.transform.localPosition = new Vector3(-9, 5.5f, 40);
            roundTimer.transform.localScale = Vector3.one * 0.1527f;
            _roundTextMesh = roundTimer.AddComponent<TextMeshPro>();
            _roundTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _roundTextMesh.fontSize = 35;
            _roundTextMesh.text = "";
            _roundTextMesh.enableWordWrapping = false;
            renderer = _roundTextMesh.renderer;
            renderer.sortingLayerName = "HUD";
            renderer.sortingOrder = 11;
        }

        /// <summary>
        /// Set the remaining amount of grace time for Hunters showed in this text.
        /// </summary>
        /// <param name="seconds">The amount of grace time left in seconds</param>
        public void SetGraceTimeRemaining(byte seconds)
        {
            if (seconds <= 0)
            {
                var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
                blanker.LocateMyFSM("Blanker Control").SendEvent("FADE OUT INSTANT");

                var hc = HeroController.instance;
                hc.AcceptInput();
                hc.RegainControl();
                InputHandler.Instance.inputActions.quickMap.Enabled = true;

                _graceTextMesh.text = "";
                return;
            }

            var timeSpan = TimeSpan.FromSeconds(seconds);
            string timeText = $"Grace period: {timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}";
            _graceTextMesh.text = timeText;
        }

        /// <summary>
        /// Set the remaining amount of time in the current round showed in this text.
        /// </summary>
        /// <param name="seconds">The amount of time left in the round in seconds</param>
        public void SetTimeRemainingInRound(ushort seconds)
        {
            if (seconds <= 0)
            {
                _roundTextMesh.text = "";
                return;
            }

            var timeSpan = TimeSpan.FromSeconds(seconds);
            string timeText = $"Round time: {timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}";
            _roundTextMesh.text = timeText;
        }

        /// <summary>
        /// Set a message for the Dream dialogue UI.
        /// </summary>
        /// <param name="convoTitle">THe convo title to set the Dream dialogue to</param>
        public void SetPropHuntMessage(string convoTitle)
        {
            _displayDreamMsg.Fsm.GetFsmString("Convo Title").Value = convoTitle;
            _displayDreamMsg.SendEvent("DISPLAY DREAM MSG");
        }
    }
}
