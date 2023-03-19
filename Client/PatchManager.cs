using Hkmp.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PropHunt.Client
{
    /// <summary>
    /// Patches aspects of the game to work with the Prop Hunt game mode.
    /// </summary>
    internal static class PatchManager
    {
        /// <summary>
        /// Disable resting on a bench when changing scenes.
        /// </summary>
        /// <param name="nextScene">The scene that is loaded next.</param>
        public static void OnSceneChange(Scene _, Scene nextScene)
        {
            foreach (var fsm in Object.FindObjectsOfType<PlayMakerFSM>())
            {
                if (fsm.gameObject.scene != nextScene) continue;

                // Find "Bench Control" Fsms and disable sitting on them
                if (fsm.Fsm.Name.Equals("Bench Control"))
                {
                    fsm.InsertMethod("Pause 2", 1, () => { PlayerData.instance.SetBool("atBench", false); });

                    var checkStartState2 = fsm.GetState("Check Start State 2");
                    var pause2State = fsm.GetState("Pause 2");
                    checkStartState2.GetTransition(1).ToFsmState = pause2State;

                    var checkStartState = fsm.GetState("Check Start State");
                    var idleStartPauseState = fsm.GetState("Idle Start Pause");
                    checkStartState.GetTransition(1).ToFsmState = idleStartPauseState;

                    var idleState = fsm.GetState("Idle");
                    idleState.Actions = new[] { idleState.Actions[0] };
                }
            }
        }
    }
}
