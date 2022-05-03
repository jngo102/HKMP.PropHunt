using GlobalEnums;
using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PropHunt.UI
{
    internal class RoundTimer : MonoBehaviour
    {
        private GameObject _timer;
        private TextMeshPro _textMesh;

        private void Awake()
        {
            _timer = new GameObject("Round Timer")
            {
                layer = (int)PhysLayers.UI
            };

            _timer.transform.SetParent(transform.Find("Geo Counter"));
            _timer.transform.localPosition = new Vector2(-9, 5.5f);
            _timer.transform.localScale = Vector3.one * 0.1527f;
            _textMesh = _timer.AddComponent<TextMeshPro>();
            _textMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(font => font.name == "trajan_bold_tmpro");
            _textMesh.fontSize = 45;
            _textMesh.text = "0:00";
        }

        /// <summary>
        /// Set the remaining amount of time showed in this text.
        /// </summary>
        /// <param name="seconds">The amount of time left in seconds.</param>
        public void SetTimeRemaining(float seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            string timeText = $"{timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}";
            _textMesh.text = timeText;
        }
    }
}
