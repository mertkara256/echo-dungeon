using System;

namespace EchoDungeon.Data
{
    [Serializable]
    public sealed class PerkChoice
    {
        public PerkChoice(
            string perkId,
            string title,
            string description,
            UpgradeType upgradeType,
            float amount,
            PerkRarity rarity)
        {
            PerkId = perkId;
            Title = title;
            Description = description;
            UpgradeType = upgradeType;
            Amount = amount;
            Rarity = rarity;
        }

        public string PerkId { get; }

        public string Title { get; }

        public string Description { get; }

        public UpgradeType UpgradeType { get; }

        public float Amount { get; }

        public PerkRarity Rarity { get; }
    }
}
