using UnityEngine;

namespace DarkPact.Core
{
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Pacted }
    public enum StatType { MaxHP, AttackDamage, MoveSpeed, CritChance, Defense, AttackSpeed }

    [CreateAssetMenu(fileName = "NewItem", menuName = "DarkPact/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string ItemName;
        public string Description;
        public Sprite Icon;
        public ItemRarity Rarity;
        public StatType BonusStat;
        public float BonusValue;

        public Color RarityColor => Rarity switch
        {
            ItemRarity.Common => Color.gray,
            ItemRarity.Uncommon => Color.green,
            ItemRarity.Rare => new Color(0.2f, 0.4f, 1f),
            ItemRarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            ItemRarity.Pacted => new Color(1f, 0.8f, 0f),
            _ => Color.white
        };
    }
}
