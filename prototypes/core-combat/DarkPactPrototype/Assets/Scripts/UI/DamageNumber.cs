using UnityEngine;

namespace DarkPact.Core
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] float _floatSpeed = 1.5f;
        [SerializeField] float _lifetime = 0.8f;

        TMPro.TextMeshPro _text;
        float _timer;
        Color _startColor;

        public void Setup(int damage, Vector2 position, bool isCritical = false)
        {
            transform.position = position + Vector2.up * 0.5f;

            _text = GetComponent<TMPro.TextMeshPro>();
            if (_text == null) return;

            _text.text = damage.ToString();
            _text.fontSize = isCritical ? 8f : 5f;
            _text.color = Color.white;
            _startColor = _text.color;
            _timer = _lifetime;

            // Random horizontal offset
            transform.position += (Vector3)(Random.insideUnitCircle * 0.2f);
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

            if (_text != null)
            {
                float alpha = Mathf.Clamp01(_timer / (_lifetime * 0.5f));
                _text.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            }

            if (_timer <= 0)
                Destroy(gameObject);
        }
    }
}
