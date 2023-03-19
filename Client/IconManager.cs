using System.IO;
using System.Reflection;
using UnityEngine;

namespace PropHunt.Client
{
    /// <summary>
    /// Enumeration for icon type.
    /// </summary>
    internal enum IconType
    {
        /// <summary>
        /// The icon for translating a prop along the x- and y-axes.
        /// </summary>
        TranslateXy,

        /// <summary>
        /// The icon for translating a prop along the z-axis.
        /// </summary>
        TranslateZ,

        /// <summary>
        /// The icon for rotating a prop.
        /// </summary>
        Rotate,

        /// <summary>
        /// The icon for scaling up a prop.
        /// </summary>
        ScaleUp,

        /// <summary>
        /// The icon for scaling down a prop.
        /// </summary>
        ScaleDown,
    }

    /// <summary>
    /// Manages the icons for when the local player transforms their prop.
    /// </summary>
    internal static class IconManager
    {
        /// <summary>
        /// The translate Xy icon.
        /// </summary>
        private static Sprite _translateXyIcon;

        /// <summary>
        /// The translate Z icon.
        /// </summary>
        private static Sprite _translateZIcon;

        /// <summary>
        /// The rotate icon.
        /// </summary>
        private static Sprite _rotateIcon;

        /// <summary>
        /// The scale up icon.
        /// </summary>
        private static Sprite _scaleUpIcon;

        /// <summary>
        /// The scale down icon.
        /// </summary>
        private static Sprite _scaleDownIcon;
        
        /// <summary>
        /// Initialize the icon manager.
        /// </summary>
        public static void Initialize()
        {   
            LoadImages();
        }

        /// <summary>
        /// Load images embedded in assembly.
        /// </summary>
        private static void LoadImages()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    var buffer = new byte[stream.Length];
                    if (stream.Read(buffer, 0, buffer.Length) < buffer.Length)
                    {
                        // TODO: Log error
                    }
                    var iconTexture = new Texture2D(2, 2);
                    iconTexture.LoadImage(buffer);
                    var iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.one * 0.5f);
                    Object.DontDestroyOnLoad(iconSprite);
                    if (resourceName.Contains(IconType.TranslateXy.ToString()))
                        _translateXyIcon = iconSprite;
                    else if (resourceName.Contains(IconType.TranslateZ.ToString()))
                        _translateZIcon = iconSprite;
                    else if (resourceName.Contains(IconType.Rotate.ToString()))
                        _rotateIcon = iconSprite;
                    else if (resourceName.Contains(IconType.ScaleUp.ToString()))
                        _scaleUpIcon = iconSprite;
                    else if (resourceName.Contains(IconType.ScaleDown.ToString()))
                        _scaleDownIcon = iconSprite;

                    stream.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Get a sprite for a given icon.
        /// </summary>
        /// <param name="iconType">The sprite's icon type.</param>
        /// <returns></returns>
        public static Sprite GetIcon(IconType iconType)
        {
            switch (iconType)
            {
                case IconType.TranslateXy:
                    return _translateXyIcon;
                case IconType.TranslateZ:
                    return _translateZIcon;
                case IconType.Rotate:
                    return _rotateIcon;
                case IconType.ScaleUp:
                    return _scaleUpIcon;
                case IconType.ScaleDown:
                    return _scaleDownIcon;
            }

            return null;
        }
    }
}
