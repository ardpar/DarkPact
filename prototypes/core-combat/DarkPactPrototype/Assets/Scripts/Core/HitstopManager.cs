using UnityEngine;

namespace DarkPact.Core
{
    public static class HitstopManager
    {
        static float _hitstopTimer;
        static float _originalTimeScale = 1f;

        public static void TriggerHitstop(float duration)
        {
            if (_hitstopTimer > 0) return; // don't stack
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _hitstopTimer = duration;
        }

        public static void Tick()
        {
            if (_hitstopTimer <= 0) return;
            _hitstopTimer -= Time.unscaledDeltaTime;
            if (_hitstopTimer <= 0)
            {
                Time.timeScale = _originalTimeScale;
            }
        }
    }
}
