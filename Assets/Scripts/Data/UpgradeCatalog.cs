using System.Collections.Generic;
using UnityEngine;

namespace EchoDungeon.Data
{
    public static class UpgradeCatalog
    {
        private sealed class Entry
        {
            public string Id;
            public string Title;
            public string Description;
            public UpgradeType UpgradeType;
            public float Amount;
            public PerkRarity Rarity;
            public int MinimumRoomIndex;
            public float Weight;
        }

        private static readonly List<Entry> CachedEntries = new List<Entry>();
        private static readonly HashSet<string> ClaimedLegendaryIds = new HashSet<string>();
        private static bool initialized;

        public static List<PerkChoice> BuildDraft(int roomIndex, bool premium, int choiceCount = 3)
        {
            EnsureInitialized();

            List<Entry> pool = BuildEligiblePool(roomIndex, premium);
            if (pool.Count == 0)
            {
                return new List<PerkChoice>();
            }

            List<PerkChoice> result = new List<PerkChoice>(choiceCount);
            HashSet<UpgradeType> usedTypes = new HashSet<UpgradeType>();

            for (int i = 0; i < choiceCount; i++)
            {
                List<Entry> remaining = FilterRemainingEntries(pool, usedTypes);
                if (remaining.Count == 0)
                {
                    break;
                }

                PerkRarity targetRarity = RollRarity(roomIndex, premium);
                Entry picked = PickWeightedEntry(FilterByRarity(remaining, targetRarity));
                if (picked == null)
                {
                    picked = PickWeightedEntry(remaining);
                }

                if (picked == null)
                {
                    break;
                }

                usedTypes.Add(picked.UpgradeType);
                result.Add(new PerkChoice(
                    picked.Id,
                    picked.Title,
                    picked.Description,
                    picked.UpgradeType,
                    picked.Amount,
                    picked.Rarity));
            }

            return result;
        }

        public static void ResetRunState()
        {
            ClaimedLegendaryIds.Clear();
        }

        public static void MarkClaimed(PerkChoice choice)
        {
            if (choice == null || choice.Rarity != PerkRarity.Legendary || string.IsNullOrWhiteSpace(choice.PerkId))
            {
                return;
            }

            ClaimedLegendaryIds.Add(choice.PerkId);
        }

        public static bool IsSpecialUpgrade(UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.EchoBarrage;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CachedEntries.Clear();

            AddFallbackEntries();
            AddSpecialEntries();
        }

        private static List<Entry> BuildEligiblePool(int roomIndex, bool premium)
        {
            List<Entry> pool = new List<Entry>();
            for (int i = 0; i < CachedEntries.Count; i++)
            {
                Entry entry = CachedEntries[i];
                if (entry.MinimumRoomIndex > roomIndex)
                {
                    continue;
                }

                if (entry.Rarity == PerkRarity.Legendary && ClaimedLegendaryIds.Contains(entry.Id))
                {
                    continue;
                }

                if (!premium && entry.Rarity == PerkRarity.Rare && roomIndex < 5)
                {
                    continue;
                }

                pool.Add(entry);
            }

            return pool;
        }

        private static List<Entry> FilterRemainingEntries(List<Entry> pool, HashSet<UpgradeType> usedTypes)
        {
            List<Entry> remaining = new List<Entry>();
            for (int i = 0; i < pool.Count; i++)
            {
                Entry entry = pool[i];
                if (!usedTypes.Contains(entry.UpgradeType))
                {
                    remaining.Add(entry);
                }
            }

            return remaining;
        }

        private static List<Entry> FilterByRarity(List<Entry> pool, PerkRarity rarity)
        {
            List<Entry> filtered = new List<Entry>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Rarity == rarity)
                {
                    filtered.Add(pool[i]);
                }
            }

            return filtered;
        }

        private static PerkRarity RollRarity(int roomIndex, bool premium)
        {
            float commonWeight;
            float uncommonWeight;
            float rareWeight;
            float legendaryWeight;

            if (premium)
            {
                commonWeight = 0.10f;
                uncommonWeight = roomIndex >= 5 ? 0.45f : 0.60f;
                rareWeight = roomIndex >= 5 ? 0.45f : 0.30f;
                legendaryWeight = roomIndex >= 4 ? 0.08f : 0.04f;
            }
            else if (roomIndex >= 5)
            {
                commonWeight = 0.45f;
                uncommonWeight = 0.40f;
                rareWeight = 0.15f;
                legendaryWeight = 0.04f;
            }
            else if (roomIndex >= 2)
            {
                commonWeight = 0.62f;
                uncommonWeight = 0.33f;
                rareWeight = 0.05f;
                legendaryWeight = 0.02f;
            }
            else
            {
                commonWeight = 0.78f;
                uncommonWeight = 0.22f;
                rareWeight = 0f;
                legendaryWeight = 0f;
            }

            float roll = Random.value * (commonWeight + uncommonWeight + rareWeight + legendaryWeight);
            if (roll < commonWeight)
            {
                return PerkRarity.Common;
            }

            if (roll < commonWeight + uncommonWeight)
            {
                return PerkRarity.Uncommon;
            }

            if (roll < commonWeight + uncommonWeight + rareWeight)
            {
                return PerkRarity.Rare;
            }

            return PerkRarity.Legendary;
        }

        private static Entry PickWeightedEntry(List<Entry> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(0.05f, pool[i].Weight);
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(0.05f, pool[i].Weight);
                if (roll <= 0f)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        private static void AddSpecialEntries()
        {
            AddEntry(
                "echo_barrage",
                "Echo Barrage",
                "Your ranged attack fires a three-shot spread.",
                UpgradeType.EchoBarrage,
                1f,
                PerkRarity.Legendary,
                2,
                0.35f);
        }

        private static void AddFallbackEntries()
        {
            AddEntry("vitality_i", "Vitality I", "+12 max health", UpgradeType.MaxHealth, 12f, PerkRarity.Common, 0, 1.1f);
            AddEntry("sharpshot_i", "Sharpshot I", "+4 weapon damage", UpgradeType.Damage, 4f, PerkRarity.Common, 0, 1f);
            AddEntry("quickdraw_i", "Quickdraw I", "+0.35 attack rate", UpgradeType.AttackRate, 0.35f, PerkRarity.Common, 0, 0.95f);
            AddEntry("strider_i", "Strider I", "+0.9 move speed", UpgradeType.MoveSpeed, 0.9f, PerkRarity.Common, 0, 0.95f);
            AddEntry("velocity_i", "Velocity I", "+2.5 projectile speed", UpgradeType.ProjectileSpeed, 2.5f, PerkRarity.Common, 0, 0.9f);
            AddEntry("guard_i", "Guard I", "+1.5 armor", UpgradeType.Armor, 1.5f, PerkRarity.Common, 0, 0.85f);

            AddEntry("vitality_ii", "Vitality II", "+18 max health", UpgradeType.MaxHealth, 18f, PerkRarity.Uncommon, 2, 0.95f);
            AddEntry("sharpshot_ii", "Sharpshot II", "+6 weapon damage", UpgradeType.Damage, 6f, PerkRarity.Uncommon, 2, 0.9f);
            AddEntry("quickdraw_ii", "Quickdraw II", "+0.55 attack rate", UpgradeType.AttackRate, 0.55f, PerkRarity.Uncommon, 2, 0.9f);
            AddEntry("strider_ii", "Strider II", "+1.35 move speed", UpgradeType.MoveSpeed, 1.35f, PerkRarity.Uncommon, 2, 0.8f);
            AddEntry("velocity_ii", "Velocity II", "+4 projectile speed", UpgradeType.ProjectileSpeed, 4f, PerkRarity.Uncommon, 2, 0.8f);
            AddEntry("guard_ii", "Guard II", "+2.5 armor", UpgradeType.Armor, 2.5f, PerkRarity.Uncommon, 2, 0.75f);

            AddEntry("vitality_iii", "Vitality III", "+28 max health", UpgradeType.MaxHealth, 28f, PerkRarity.Rare, 5, 0.8f);
            AddEntry("sharpshot_iii", "Sharpshot III", "+9 weapon damage", UpgradeType.Damage, 9f, PerkRarity.Rare, 5, 0.75f);
            AddEntry("quickdraw_iii", "Quickdraw III", "+0.8 attack rate", UpgradeType.AttackRate, 0.8f, PerkRarity.Rare, 5, 0.75f);
            AddEntry("strider_iii", "Strider III", "+1.9 move speed", UpgradeType.MoveSpeed, 1.9f, PerkRarity.Rare, 5, 0.65f);
            AddEntry("velocity_iii", "Velocity III", "+6.5 projectile speed", UpgradeType.ProjectileSpeed, 6.5f, PerkRarity.Rare, 5, 0.65f);
            AddEntry("guard_iii", "Guard III", "+4 armor", UpgradeType.Armor, 4f, PerkRarity.Rare, 5, 0.6f);
        }

        private static void AddEntry(
            string id,
            string title,
            string description,
            UpgradeType upgradeType,
            float amount,
            PerkRarity rarity,
            int minimumRoomIndex,
            float weight)
        {
            CachedEntries.Add(new Entry
            {
                Id = id,
                Title = title,
                Description = description,
                UpgradeType = upgradeType,
                Amount = amount,
                Rarity = rarity,
                MinimumRoomIndex = minimumRoomIndex,
                Weight = weight
            });
        }
    }
}
