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

    /// <summary>
    /// The team that each player will be assigned.
    /// </summary>
    internal enum PropHuntTeam
    {
        Hunters,
        Props,
    }
}
