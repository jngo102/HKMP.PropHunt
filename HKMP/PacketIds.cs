namespace PropHunt.HKMP
{
    /// <summary>
    /// Enumeration for packets that are sent from a client to the server.
    /// </summary>
    internal enum FromClientToServerPackets
    {
        /// <summary>
        /// Broadcast that the local player has died as a hunter.
        /// </summary>
        BroadcastHunterDeath,

        /// <summary>
        /// Broadcast that the local player has died as a prop.
        /// </summary>
        BroadcastPropDeath,

        /// <summary>
        /// Broadcast the local player's prop's position along the x- and y-axes.
        /// </summary>
        BroadcastPropPositionXy,

        /// <summary>
        /// Broadcast the local player's prop's position along the z-axis.
        /// </summary>
        BroadcastPropPositionZ,

        /// <summary>
        /// Broadcast the local player's prop's rotation.
        /// </summary>
        BroadcastPropRotation,

        /// <summary>
        /// Broadcast the local player's prop's scale.
        /// </summary>
        BroadcastPropScale,

        /// <summary>
        /// Broadcast the local player's prop's sprite.
        /// </summary>
        BroadcastPropSprite,
        
        /// <summary>
        /// Request to end a round.
        /// </summary>
        EndRound,

        /// <summary>
        /// Request to start a new round.
        /// </summary>
        StartRound,
    }

    /// <summary>
    /// Enumeration for packets that are sent from the server to a client.
    /// </summary>
    internal enum FromServerToClientPackets
    {
        /// <summary>
        /// Assign a player a team.
        /// </summary>
        AssignTeam,

        /// <summary>
        /// End a round.
        /// </summary>
        EndRound,
        
        /// <summary>
        /// Announce a hunter death.
        /// </summary>
        HunterDeath,

        /// <summary>
        /// Announce a prop death.
        /// </summary>
        PropDeath,

        /// <summary>
        /// Announce a player leaving the current round.
        /// </summary>
        PlayerLeftRound,

        /// <summary>
        /// Update the grace timer.
        /// </summary>
        UpdateGraceTimer,

        /// <summary>
        /// Update the round timer.
        /// </summary>
        UpdateRoundTimer,

        /// <summary>
        /// Update a prop's position along the x- and y-axes.
        /// </summary>
        UpdatePropPositionXy,

        /// <summary>
        /// Update a prop's position along the z-axis.
        /// </summary>
        UpdatePropPositionZ,

        /// <summary>
        /// Update a prop's rotation.
        /// </summary>
        UpdatePropRotation,

        /// <summary>
        /// Update a prop's scale.
        /// </summary>
        UpdatePropScale,

        /// <summary>
        /// Update a prop's sprite.
        /// </summary>
        UpdatePropSprite,
    }
}
