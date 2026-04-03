using UnityEngine;

namespace DarkPact.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] float _smoothTime = 0.15f;
        [SerializeField] float _lookAheadDistance = 1f;
        [SerializeField] int _pixelsPerUnit = 16;

        Transform _target;
        Vector3 _velocity;

        // Screen shake
        float _shakeIntensity;
        float _shakeDuration;
        float _shakeTimer;

        void LateUpdate()
        {
            if (_target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) _target = player.transform;
                else return;
            }

            // SmoothDamp follow
            Vector3 targetPos = _target.position;
            targetPos.z = transform.position.z;

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);

            // Screen shake
            if (_shakeTimer > 0)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                float t = _shakeTimer / _shakeDuration;
                Vector2 offset = Random.insideUnitCircle * _shakeIntensity * t;
                smoothed += (Vector3)offset;
            }

            // Pixel snap
            smoothed.x = Mathf.Round(smoothed.x * _pixelsPerUnit) / _pixelsPerUnit;
            smoothed.y = Mathf.Round(smoothed.y * _pixelsPerUnit) / _pixelsPerUnit;

            transform.position = smoothed;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeTimer = duration;
        }
    }
}
