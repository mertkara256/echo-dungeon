using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Data
{
    public static class UpgradeEffectApplier
    {
        public static bool TryApply(PlayerStats stats, PerkChoice choice)
        {
            if (stats == null || choice == null)
            {
                return false;
            }

            if (!UpgradeCatalog.IsSpecialUpgrade(choice.UpgradeType))
            {
                stats.ApplyUpgrade(choice.UpgradeType, choice.Amount);
                return true;
            }

            bool applied = ApplySpecialUpgrade(stats.gameObject, choice.UpgradeType);
            if (applied)
            {
                UpgradeCatalog.MarkClaimed(choice);
            }

            return applied;
        }

        private static bool ApplySpecialUpgrade(GameObject playerObject, UpgradeType upgradeType)
        {
            if (playerObject == null)
            {
                return false;
            }

            switch (upgradeType)
            {
                case UpgradeType.EchoBarrage:
                    PlayerCombat combat = playerObject.GetComponent<PlayerCombat>();
                    if (combat == null)
                    {
                        return false;
                    }

                    combat.EnableEchoBarrage();
                    return true;
                default:
                    return false;
            }
        }
    }
}
