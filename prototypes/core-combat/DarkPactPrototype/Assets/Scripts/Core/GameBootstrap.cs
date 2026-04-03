using UnityEngine;

namespace DarkPact.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        void Update()
        {
            HitstopManager.Tick();
        }

        // Pact selection shortcut for prototype — press P to toggle Katliam Paktı
        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                var pact = FindAnyObjectByType<PactManager>();
                if (pact == null) return;

                if (pact.IsKatliamActive)
                    pact.DeactivateAll();
                else
                    pact.ActivateKatliamPact();
            }
        }
    }
}
