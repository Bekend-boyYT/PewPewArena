using UnityEngine;

namespace SniperGame.Weapons
{
    public class CosmeticBullet : MonoBehaviour
    {
        private Vector3 _targetPosition;
        private Vector3 _hitNormal;
        private GameObject _hitEffectPrefab;
        private float _speed;
        private bool _isInitialized;

        public void Initialize(Vector3 targetPosition, Vector3 hitNormal, GameObject hitEffectPrefab, float speed)
        {
            _targetPosition = targetPosition;
            _hitNormal = hitNormal;
            _hitEffectPrefab = hitEffectPrefab;
            _speed = speed;
            _isInitialized = true;

            transform.LookAt(_targetPosition);
            Destroy(gameObject, 3f); // Safety cleanup
        }

        private void Update()
        {
            if (!_isInitialized) return;

            float step = _speed * Time.deltaTime;
            float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);

            if (distanceToTarget <= step)
            {
                OnHitTarget();
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, step);
            }
        }

        private void OnHitTarget()
        {
            transform.position = _targetPosition;

            if (_hitEffectPrefab != null)
            {
                Quaternion rot = _hitNormal != Vector3.zero ? Quaternion.LookRotation(_hitNormal) : Quaternion.identity;
                GameObject effect = Instantiate(_hitEffectPrefab, _targetPosition, rot);
                Destroy(effect, 3f);
            }

            Destroy(gameObject);
        }
    }
}