using UnityEngine;

namespace DarkPact.Core
{
    public class PactManager : MonoBehaviour
    {
        public bool IsKatliamActive { get; private set; }

        const float KatliamDamageMultiplier = 1.6f;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void ActivateKatliamPact()
        {
            IsKatliamActive = true;

            // Apply damage bonus to player
            if (ServiceLocator.TryGet<PlayerController>(out var player))
            {
                player.DamageMultiplier = KatliamDamageMultiplier;
            }

            Debug.Log("Katliam Paktı aktif: +%60 hasar, düşmanlar dirilecek!");
        }

        public void DeactivateAll()
        {
            IsKatliamActive = false;
            if (ServiceLocator.TryGet<PlayerController>(out var player))
            {
                player.DamageMultiplier = 1f;
            }
        }
    }
}
