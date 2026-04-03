using UnityEngine;

namespace DarkPact.Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] GameObject _hitSparkPrefab;
        [SerializeField] int _poolSize = 15; // GDD: MaxConcurrent(10) × PoolMultiplier(1.5)

        GameObject[] _hitSparkPool;
        int _poolIndex;

        void Awake()
        {
            Instance = this;

            _hitSparkPool = new GameObject[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                if (_hitSparkPrefab != null)
                {
                    _hitSparkPool[i] = Instantiate(_hitSparkPrefab, transform);
                    _hitSparkPool[i].SetActive(false);
                }
            }
        }

        public void PlayHitSpark(Vector2 position)
        {
            if (_hitSparkPrefab == null) return;

            var obj = _hitSparkPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _poolSize;

            obj.transform.position = position;
            obj.SetActive(true);

            var ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }

        public void ShakeCamera(float intensity, float duration)
        {
            var cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null) cam.Shake(intensity, duration);
        }
    }
}
