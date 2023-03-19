using UnityEngine;

namespace PropHunt.Client
{
    /// <summary>
    /// Handles assigning and removing loadouts for players during a round of prop hunt.
    /// Portions taken from https://github.com/Extremelyd1/HKMP-Tag/blob/master/Client/LoadoutUtil.cs
    /// </summary>
    internal static class LoadoutManager
    {
        private static int _originalHP;
        private static int _originalMaxHP;
        private static int _originalMaxHPBase;

        /// <summary>
        /// Make changes to the player as a hunter.
        /// </summary>
        public static void SetHunterLoadout()
        {
            RevertPropLoadout();
            RevertHealth();
            SaveHealth();
            SetHealth(10, true);
            RemoveSoul();
            SetKingSoul();
            EquipCharms(26, 31, 36, 37);
            var animCtrl = HeroController.instance.GetComponent<HeroAnimationController>();
            animCtrl.enabled = true;
            On.HeroController.CanFocus += RemoveFocus;
        }

        /// <summary>
        /// Revert changes to player made as a hunter.
        /// </summary>
        public static void RevertHunterLoadout()
        {
            RevertHealth();
            SaveHealth();
            EquipCharms();
            On.HeroController.CanFocus -= RemoveFocus;
        }

        /// <summary>
        /// Make changes to the player as a prop.
        /// </summary>  
        public static void SetPropLoadout()
        {
            RevertHunterLoadout();
            SetHealth(1, true);
            RemoveSoul();
            EquipCharms();
            HeroController.instance.GetComponent<HeroAnimationController>().enabled = false;
            On.HeroController.CanFocus += RemoveFocus;
            On.HeroController.CanCast += RemoveCast;
            On.HeroController.CanNailCharge += RemoveNailCharge;
            // Prevent float
            HeroController.instance.gameObject.LocateMyFSM("Nail Arts").SendEvent("Fsm CANCEL");
            HeroController.instance.gameObject.LocateMyFSM("Spell Control").SendEvent("Fsm CANCEL");
        }

        /// <summary>
        /// Revert changes to player made as a prop.
        /// </summary>
        public static void RevertPropLoadout()
        {
            RevertHealth();
            SaveHealth();
            On.HeroController.CanFocus -= RemoveFocus;
            On.HeroController.CanCast -= RemoveCast;
            On.HeroController.CanNailCharge -= RemoveNailCharge;
        }
        private static bool RemoveFocus(On.HeroController.orig_CanFocus orig, HeroController self) => false;

        private static bool RemoveCast(On.HeroController.orig_CanCast orig, HeroController self) => false;

        private static bool RemoveNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self) => false;

        /// <summary>
        /// Save the player's current health values.
        /// </summary>
        private static void SaveHealth()
        {
            _originalHP = PlayerData.instance.health;
            _originalMaxHP = PlayerData.instance.maxHealth;
            _originalMaxHPBase = PlayerData.instance.maxHealthBase;
        }

        /// <summary>
        /// Revert health back to their original values.
        /// </summary>
        private static void RevertHealth()
        {
            PlayerData.instance.health = _originalHP;
            PlayerData.instance.maxHealth = _originalMaxHP;
            PlayerData.instance.maxHealthBase = _originalMaxHPBase;
        }

        /// <summary>
        /// Set the local player's health.
        /// </summary>
        /// <param name="health">The max health to set to.</param>
        /// <param name="maxHealth">Whether to fully heal the player.</param>
        public static void SetHealth(int health, bool maxHealth)
        {
            if (maxHealth)
            {
                PlayerData.instance.maxHealth = health;
                PlayerData.instance.maxHealthBase = health;
                HeroController.instance.MaxHealth();
            }
            else
            {
                var healthRatio = PlayerData.instance.health / (float)PlayerData.instance.maxHealth;
                PlayerData.instance.maxHealth = health;
                PlayerData.instance.maxHealthBase = health;
                PlayerData.instance.health = Mathf.FloorToInt(healthRatio * PlayerData.instance.maxHealth);
            }

            var healthParent = GameCameras.instance.hudCanvas.transform.Find("Health");
            for (int healthNum = 1; healthNum <= 11; healthNum++)
            {
                var healthDisplay = healthParent.Find($"Health {healthNum}").gameObject.LocateMyFSM("health_display");
                healthDisplay.SendEvent("CHARM INDICATOR CHECK");
            }
        }

        /// <summary>
        /// Remove all soul from the local player.
        /// </summary>
        private static void RemoveSoul()
        {
            PlayerData.instance.ClearMP();
            GameManager.instance.soulOrb_fsm.SendEvent("MP DRAIN");
            GameManager.instance.soulVessel_fsm.SendEvent("MP RESERVE DOWN");
        }

        /// <summary>
        /// Equip the given indices as charms and un-equip all other charms.
        /// </summary>
        /// <param name="charmIndices">An int array containing the indices of charms to set.</param>
        private static void EquipCharms(params int[] charmIndices)
        {
            // Un-equip all charms
            for (var i = 1; i <= 40; i++)
            {
                PlayerData.instance.SetBool("equippedCharm_" + i, false);
                GameManager.instance.UnequipCharm(i);
            }

            // Equip the charms that are given as parameters
            foreach (var charmIndex in charmIndices)
            {
                PlayerData.instance.SetBool("equippedCharm_" + charmIndex, true);
                GameManager.instance.EquipCharm(charmIndex);
            }

            // Run some update methods to make sure UI doesn't glitch out
            HeroController.instance.CharmUpdate();
            GameManager.instance.RefreshOvercharm();
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
            PlayMakerFSM.BroadcastEvent("CHARM EQUIP CHECK");
            EventRegister.SendEvent("CHARM EQUIP CHECK");
            EventRegister.SendEvent("CHARM INDICATOR CHECK");
        }

        /// <summary>
        /// Adjust the PlayerData to make the King Soul charm available.
        /// </summary>
        private static void SetKingSoul()
        {
            PlayerData.instance.SetBool(nameof(PlayerData.gotCharm_36), true);
            PlayerData.instance.SetBool(nameof(PlayerData.gotShadeCharm), false);

            PlayerData.instance.SetInt(nameof(PlayerData.royalCharmState), 3);
            PlayerData.instance.SetInt(nameof(PlayerData.charmCost_36), 5);
        }
    }
}
