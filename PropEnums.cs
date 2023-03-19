namespace PropHunt
{
    /// <summary>
    /// The current state of the local player's prop.
    /// </summary>
    internal enum PropState
    {
        /// <summary>
        /// The local player's prop is not being transformed.
        /// </summary>
        Free,

        /// <summary>
        /// The local player's prop is being translated along the x- and y-axes.
        /// </summary>
        TranslateXy,

        /// <summary>
        /// The local player's prop is being translated along the z-axis.
        /// </summary>
        TranslateZ,

        /// <summary>
        /// The local player's prop is being rotated.
        /// </summary>
        Rotate,
        
        /// <summary>
        /// The local player's prop is being scaled.
        /// </summary>
        Scale,
    }
}
