namespace PropHunt
{
    /// <summary>
    /// The current state of the local player's prop.
    /// </summary>
    internal enum PropState
    {
        Free,
        TranslateXY,
        TranslateZ,
        Rotate,
        Scale,
    }
}
