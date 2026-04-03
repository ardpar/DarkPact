using UnityEngine;

namespace DarkPact.Core
{
    public class VFXManager : MonoBehaviour
    {
        [SerializeField] GameObject _hitSparkPrefab;
        [SerializeField] int _poolSize = 15;
        [SerializeField] GameObject _dashTrailPrefab;
        [SerializeField] int _dashTrailPoolSize = 10;

        GameObject[] _hitSparkPool;
        int _hitSparkIndex;
        GameObject[] _dashTrailPool;
        int _dashTrailIndex;

        void Awake()
        {
            ServiceLocator.Register(this);

            _hitSparkPool = new GameObject[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                if (_hitSparkPrefab != null)
                {
                    _hitSparkPool[i] = Instantiate(_hitSparkPrefab, transform);
                    _hitSparkPool[i].SetActive(false);
                }
            }

            _dashTrailPool = new GameObject[_dashTrailPoolSize];
            for (int i = 0; i < _dashTrailPoolSize; i++)
            {
                if (_dashTrailPrefab != null)
                {
                    _dashTrailPool[i] = Instantiate(_dashTrailPrefab, transform);
                    _dashTrailPool[i].SetActive(false);
                }
            }
        }

        public void PlayHitSpark(Vector2 position)
        {
            if (_hitSparkPool == null || _hitSparkPrefab == null) return;

            var obj = _hitSparkPool[_hitSparkIndex];
            _hitSparkIndex = (_hitSparkIndex + 1) % _poolSize;

            obj.transform.position = position;
            obj.SetActive(true);

            var ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }

        public void PlayDashTrail(Vector2 position, Sprite sprite, bool flipX)
        {
            if (_dashTrailPool == null || _dashTrailPrefab == null) return;

            var obj = _dashTrailPool[_dashTrailIndex];
            _dashTrailIndex = (_dashTrailIndex + 1) % _dashTrailPoolSize;

            obj.transform.position = position;
            obj.SetActive(true);

            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.flipX = flipX;
                sr.color = new Color(0.5f, 0.5f, 1f, 0.6f);
            }
        }

        public void ShakeCamera(float intensity, float duration)
        {
            var cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null) cam.Shake(intensity, duration);
        }
    }
}
