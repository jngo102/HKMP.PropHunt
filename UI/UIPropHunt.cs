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
        private TextMeshPro _msgTextMesh;

        private void Awake()
        {
            var graceTimer = new GameObject("Grace Timer")
            {
                layer = (int)PhysLayers.UI
            };

            graceTimer.transform.SetParent(transform.Find("Geo Counter"));
            graceTimer.transform.localPosition = new Vector2(-9, 4.85f);
            graceTimer.transform.localScale = Vector3.one * 0.1527f;
            _graceTextMesh = graceTimer.AddComponent<TextMeshPro>();
            _graceTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _graceTextMesh.fontSize = 35;
            _graceTextMesh.text = "";
            _graceTextMesh.enableWordWrapping = false;

            var roundTimer = new GameObject("Round Timer")
            {
                layer = (int)PhysLayers.UI
            };

            roundTimer.transform.SetParent(transform.Find("Geo Counter"));
            roundTimer.transform.localPosition = new Vector2(-9, 5.5f);
            roundTimer.transform.localScale = Vector3.one * 0.1527f;
            _roundTextMesh = roundTimer.AddComponent<TextMeshPro>();
            _roundTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _roundTextMesh.fontSize = 35;
            _roundTextMesh.text = "";
            _roundTextMesh.enableWordWrapping = false;

            var msg = new GameObject("Prop Hunt Message")
            {
                layer = (int)PhysLayers.UI
            };

            msg.transform.SetParent(transform.Find("Geo Counter"));
            msg.transform.localPosition = new Vector2(-9, 4.2f);
            msg.transform.localScale = Vector3.one * 0.1527f;
            _msgTextMesh = msg.AddComponent<TextMeshPro>();
            _msgTextMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _msgTextMesh.fontSize = 35;
            _msgTextMesh.text = "";
            _msgTextMesh.enableWordWrapping = false;
        }

        /// <summary>
        /// Set the remaining amount of grace time for Hunters showed in this text.
        /// </summary>
        /// <param name="seconds">The amount of grace time left in seconds.</param>
        public void SetGraceTimeRemaining(int seconds)
        {
            if (seconds <= 0)
            {
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
        /// <param name="seconds">The amount of time left in the round in seconds.</param>
        public void SetTimeRemainingInRound(int seconds)
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
        /// Set a message for the Prop Hunt message object.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void SetPropHuntMessage(string message)
        {
            StartCoroutine(ShowMessageRoutine());

            IEnumerator ShowMessageRoutine()
            {
                _msgTextMesh.text = message;

                _msgTextMesh.alpha = 0;
                yield return new WaitUntil(() =>
                {
                    _msgTextMesh.alpha += Time.deltaTime * 4;
                    return _msgTextMesh.alpha >= 1;
;               });

                _msgTextMesh.alpha = 1;

                yield return new WaitForSeconds(4);

                yield return new WaitUntil(() =>
                {
                    _msgTextMesh.alpha -= Time.deltaTime * 4;
                    return _msgTextMesh.alpha <= 0;
                });

                _msgTextMesh.alpha = 0;
            }
        }
    }
}
