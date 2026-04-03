using UnityEngine;

namespace DarkPact.Core
{
    public class DashTrailGhost : MonoBehaviour
    {
        [SerializeField] float _fadeDuration = 0.2f;

        SpriteRenderer _sr;
        float _timer;
        Color _startColor;

        void OnEnable()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _startColor = _sr.color;
                _timer = _fadeDuration;
            }
        }

        void Update()
        {
            if (_sr == null) return;
            _timer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_timer / _fadeDuration);
            _sr.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha * _startColor.a);

            if (_timer <= 0)
                gameObject.SetActive(false);
        }
    }
}
