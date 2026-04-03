using UnityEngine;

namespace DarkPact.Core
{
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "DarkPact/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            public ItemDefinition Item;
            [Range(0f, 1f)] public float Weight = 1f;
        }

        public LootEntry[] Entries;
        [Range(0f, 1f)] public float DropChance = 0.5f;

        public ItemDefinition Roll()
        {
            if (Entries == null || Entries.Length == 0) return null;
            if (Random.value > DropChance) return null;

            float totalWeight = 0f;
            foreach (var e in Entries) totalWeight += e.Weight;

            float roll = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var e in Entries)
            {
                cumulative += e.Weight;
                if (roll <= cumulative) return e.Item;
            }
            return Entries[^1].Item;
        }
    }
}
