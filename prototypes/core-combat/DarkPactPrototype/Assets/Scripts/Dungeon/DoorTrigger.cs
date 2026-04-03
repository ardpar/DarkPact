using UnityEngine;

namespace DarkPact.Core
{
    public class DoorTrigger : MonoBehaviour
    {
        public int FromRoomIndex { get; set; }
        public int ToRoomIndex { get; set; }
        public Direction DoorDirection { get; set; }

        bool _used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;
            if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            _used = true;

            if (ServiceLocator.TryGet<DungeonManager>(out var dm))
                dm.TransitionToRoom(ToRoomIndex, DoorDirection);

            // Reset after short delay to prevent re-trigger
            Invoke(nameof(ResetTrigger), 1f);
        }

        void ResetTrigger() => _used = false;
    }
}
