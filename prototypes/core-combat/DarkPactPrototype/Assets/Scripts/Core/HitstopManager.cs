using UnityEngine;

namespace DarkPact.Core
{
    public static class HitstopManager
    {
        static float _hitstopTimer;

        public static bool IsHitstopActive => _hitstopTimer > 0;

        public static void TriggerHitstop(float duration)
        {
            if (_hitstopTimer > 0) return; // don't stack
            if (Time.timeScale == 0f) return; // don't hitstop during pause
            Time.timeScale = 0f;
            _hitstopTimer = duration;
        }

        public static void Tick()
        {
            if (_hitstopTimer <= 0) return;
            _hitstopTimer -= Time.unscaledDeltaTime;
            if (_hitstopTimer <= 0)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
