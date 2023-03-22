﻿using GlobalEnums;
using Modding;
using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PropHunt.Client
{
    /// <summary>
    /// Manages the Prop Hunt user interface.
    /// </summary>
    internal static class TextManager
    {
        private static readonly string[] HunterDeathSuffixes =
        {
            "died! Maybe they should learn the room layout better...",
            "foolishly broke too many non-props and perished!",
            "couldn't deal with the guilt of breaking so many innocent objects!",
            "mistakenly broke their own soul!",
            "was banned from IKEA!",
            "behaved im-prop-erly!",
        };

        /// <summary>
        /// The language key for Prop Hunt messages.
        /// </summary>
        private const string ConvoTitle = "PROP_HUNT";

        /// <summary>
        /// The convo title that will contain Prop Hunt messages.
        /// </summary>
        private static string _convoTitle;

        /// <summary>
        /// Display FSM of the Dream Msg game object.
        /// </summary>
        private static PlayMakerFSM _dreamDisplayFsm;

        /// <summary>
        /// The renderer for the grace timer object.
        /// </summary>
        private static TextMeshPro _graceTextMesh;

        /// <summary>
        /// The renderer for the round timer object.
        /// </summary>
        private static TextMeshPro _roundTextMesh;

        /// <summary>
        /// Initialize the text manager.
        /// </summary>
        public static void Initialize()
        {
            On.HUDCamera.OnEnable += OnHudCameraEnable;
            ModHooks.LanguageGetHook += (key, _, orig) =>
            {
                return key == ConvoTitle ? _convoTitle : orig;
            };
        }

        /// <summary>
        /// Display a message as dream dialogue.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public static void DisplayDreamMessage(string text)
        {
            _convoTitle = text;
            _dreamDisplayFsm.Fsm.GetFsmString("Convo Title").Value = ConvoTitle;
            _dreamDisplayFsm.SendEvent("DISPLAY DREAM MSG");
        }

        /// <summary>
        /// Set the remaining amount of grace time for Hunters shown in this text.
        /// </summary>
        /// <param name="seconds">The amount of grace time left in seconds.</param>
        public static void SetRemainingGraceTime(byte seconds)
        {
            if (seconds <= 0)
            {
                ShowBlanker(false);

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
        /// Set the remaining amount of time in the current round shown in this text.
        /// </summary>
        /// <param name="seconds">The amount of time left in the round in seconds.</param>
        public static void SetRemainingRoundTime(ushort seconds)
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
        /// Set the remaining amount of time before a new rounds begins shown in this text.
        /// </summary>
        /// <param name="seconds">The amount of time left in the round in seconds.</param>
        public static void SetRemainingRoundOverTime(ushort seconds)
        {
            if (seconds <= 0)
            {
                _roundTextMesh.text = "";
                return;
            }

            var timeSpan = TimeSpan.FromSeconds(seconds);
            string timeText = $"Time before next round: {timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}";
            _roundTextMesh.text = timeText;
        }

        /// <summary>
        /// Show a message for when a hunter dies.
        /// </summary>
        /// <param name="username">The name of the hunter that died.</param>
        /// <param name="index">The index of the hunter messages randomly generated by the server.</param>
        public static void ShowHunterDeathMessage(string username, byte index)
        {
            DisplayDreamMessage($"Hunter {username} {HunterDeathSuffixes[index]}");
        }

        /// <summary>
        /// Set up the prop hunt user interface only once the HUD Camera is enabled so TextMeshPro can initialize.
        /// </summary>
        private static void OnHudCameraEnable(On.HUDCamera.orig_OnEnable orig, HUDCamera self)
        {
            orig(self);

            SetupDreamMsg();
            SetupGraceTimer();
            SetupRoundTimer();
        }

        /// <summary>
        /// Set up the Dream Msg object's UI.
        /// </summary>
        private static void SetupDreamMsg()
        {
            var dreamMsg = GameCameras.instance.hudCamera.transform.Find("DialogueManager/Dream Msg").gameObject;
            foreach (var renderer in dreamMsg.GetComponentsInChildren<Renderer>())
            {
                // Place dream msg above blanker so hunters can read that they are hunters.
                renderer.sortingOrder = 11;
            }

            _dreamDisplayFsm = dreamMsg.LocateMyFSM("Display");
        }

        /// <summary>
        /// Set up the grace timer UI.
        /// </summary>
        private static void SetupGraceTimer()
        {
            var graceTimer = new GameObject("Grace Timer")
            {
                layer = (int)PhysLayers.UI
            };

            graceTimer.transform.SetParent(GameCameras.instance.hudCanvas.transform.Find("Geo Counter"));
            graceTimer.transform.localPosition = new Vector3(-9, 4.85f, 40);
            graceTimer.transform.localScale = Vector3.one * 0.1527f;
            _graceTextMesh = graceTimer.AddComponent<TextMeshPro>();
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _graceTextMesh.font = font;
            _graceTextMesh.fontSize = 35;
            _graceTextMesh.text = "";
            _graceTextMesh.enableWordWrapping = false;
            var renderer = _graceTextMesh.renderer;
            renderer.sortingLayerName = "HUD";
            renderer.sortingOrder = 11;
        }

        /// <summary>
        /// Set up the round timer UI.
        /// </summary>
        private static void SetupRoundTimer()
        {
            var roundTimer = new GameObject("Round Timer")
            {
                layer = (int)PhysLayers.UI
            };

            roundTimer.transform.SetParent(GameCameras.instance.hudCanvas.transform.Find("Geo Counter"));
            roundTimer.transform.localPosition = new Vector3(-9, 5.5f, 40);
            roundTimer.transform.localScale = Vector3.one * 0.1527f;
            _roundTextMesh = roundTimer.AddComponent<TextMeshPro>();
            _roundTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _roundTextMesh.fontSize = 35;
            _roundTextMesh.text = "";
            _roundTextMesh.enableWordWrapping = false;
            var renderer = _roundTextMesh.renderer;
            renderer.sortingLayerName = "HUD";
            renderer.sortingOrder = 11;
        }

        /// <summary>
        /// Show or hide the blanker.
        /// </summary>
        /// <param name="show">Whether to make the blanker visible.</param>
        public static void ShowBlanker(bool show = true)
        {
            var blanker = GameCameras.instance.hudCamera.transform.Find("2dtk Blanker").gameObject;
            blanker.LocateMyFSM("Blanker Control").SendEvent("FADE " + (show ? "IN" : "OUT") + " INSTANT");
        }
    }
}
