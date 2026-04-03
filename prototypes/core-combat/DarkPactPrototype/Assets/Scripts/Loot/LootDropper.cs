using UnityEngine;

namespace DarkPact.Core
{
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] LootTable _lootTable;
        [SerializeField] GameObject _pickupPrefab;

        public void DropLoot(Vector2 position)
        {
            if (_lootTable == null || _pickupPrefab == null) return;

            var item = _lootTable.Roll();
            if (item == null) return;

            var pickupObj = Instantiate(_pickupPrefab, position, Quaternion.identity);
            var pickup = pickupObj.GetComponent<LootPickup>();
            if (pickup != null)
                pickup.Setup(item, position);
        }
    }
}
