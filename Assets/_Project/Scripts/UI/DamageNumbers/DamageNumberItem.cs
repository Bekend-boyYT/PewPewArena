using System;
using TMPro;
using UnityEngine;

namespace SniperGame.UI.DamageNumbers
{
    public class DamageNumberItem : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;

        private float _lifetime = 0.85f;
        private float _elapsed = 0f;
        private Vector3 _velocity;
        private Color _baseColor;
        private Action<DamageNumberItem> _onComplete;
        private Transform _camTransform;

        private void Awake()
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        }

        public void Initialize(int damage, Vector3 position, bool isCrit, Action<DamageNumberItem> onComplete)
        {
            transform.position = position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.2f, 0.5f), UnityEngine.Random.Range(-0.25f, 0.25f));
            _onComplete = onComplete;
            _elapsed = 0f;

            if (Camera.main != null) _camTransform = Camera.main.transform;

            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

            if (textMesh != null)
            {
                textMesh.text = damage.ToString();

                if (isCrit)
                {
                    // Critical headshot damage: Orange-Red, larger
                    _baseColor = new Color(1.0f, 0.25f, 0.1f, 1f);
                    textMesh.fontSize = 7f;
                    textMesh.fontStyle = FontStyles.Bold;
                }
                else
                {
                    // Normal body damage: Bright Yellow-White
                    _baseColor = new Color(1.0f, 0.95f, 0.4f, 1f);
                    textMesh.fontSize = 5.5f;
                    textMesh.fontStyle = FontStyles.Normal;
                }

                textMesh.color = _baseColor;
            }

            // Upward drift with slight random horizontal drift
            _velocity = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 1.8f, UnityEngine.Random.Range(-0.4f, 0.4f));
            gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (_camTransform == null && Camera.main != null)
            {
                _camTransform = Camera.main.transform;
            }

            // Billboard towards camera
            if (_camTransform != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _camTransform.position);
            }

            // Move upward
            transform.position += _velocity * Time.deltaTime;
            _velocity.y = Mathf.MoveTowards(_velocity.y, 0.5f, Time.deltaTime * 2.5f);

            _elapsed += Time.deltaTime;
            float progress = _elapsed / _lifetime;

            // Fade out in the last 40% of lifetime
            if (textMesh != null)
            {
                float alpha = progress > 0.6f ? Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f) : 1f;
                textMesh.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            }

            if (_elapsed >= _lifetime)
            {
                gameObject.SetActive(false);
                _onComplete?.Invoke(this);
            }
        }
    }
}
