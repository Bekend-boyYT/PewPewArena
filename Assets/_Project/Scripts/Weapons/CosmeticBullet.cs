using UnityEngine;

namespace SniperGame.Weapons
{
    public class CosmeticBullet : MonoBehaviour
    {
        [Header("Trail & Particle Configuration")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Material sparksMaterial;
        [SerializeField] private float trailTime = 0.35f;
        [SerializeField] private float trailWidth = 0.075f;
        [SerializeField] private bool isHeavyBullet = false;
        [SerializeField] private Color bulletLightColor = new Color(0.5f, 0.9f, 1f);
        [SerializeField] private float bulletLightIntensity = 3.5f;
        [SerializeField] private float bulletLightRange = 5.5f;

        private Vector3 _targetPosition;
        private Vector3 _hitNormal;
        private GameObject _hitEffectPrefab;
        private float _speed;
        private bool _isCharacterHit;
        private bool _isInitialized;
        private Light _bulletLight;

        public void Initialize(Vector3 targetPosition, Vector3 hitNormal, GameObject hitEffectPrefab, float speed, bool isCharacterHit = false)
        {
            _targetPosition = targetPosition;
            _hitNormal = hitNormal;
            _hitEffectPrefab = hitEffectPrefab;
            _speed = Mathf.Max(10f, speed);
            _isCharacterHit = isCharacterHit;
            _isInitialized = true;

            transform.LookAt(_targetPosition);
            SetupVisuals();
            Destroy(gameObject, 5f); // Safety cleanup timeout
        }

        private void SetupVisuals()
        {
            // 1. Dynamic Point Light illuminating the flight path
            _bulletLight = GetComponent<Light>();
            if (_bulletLight == null)
            {
                _bulletLight = gameObject.AddComponent<Light>();
            }

            _bulletLight.type = LightType.Point;
            _bulletLight.color = isHeavyBullet ? new Color(1f, 0.72f, 0.2f) : bulletLightColor;
            _bulletLight.intensity = isHeavyBullet ? 6.5f : bulletLightIntensity;
            _bulletLight.range = isHeavyBullet ? 7.5f : bulletLightRange;
            _bulletLight.shadows = LightShadows.None;
            _bulletLight.enabled = true;

            // 2. Trail Renderer Configuration
            var trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            if (trailMaterial != null)
            {
                trail.material = trailMaterial;
            }

            trail.time = trailTime;
            trail.widthMultiplier = trailWidth;
            trail.minVertexDistance = 0.05f;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            // Tubular, solid sniper contrail profile (broad core tapering to needle tip at tail)
            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(new Keyframe(0f, 1f));
            widthCurve.AddKey(new Keyframe(0.35f, 0.95f));
            widthCurve.AddKey(new Keyframe(0.7f, 0.55f));
            widthCurve.AddKey(new Keyframe(1f, 0f));
            trail.widthCurve = widthCurve;

            Gradient grad = new Gradient();
            if (isHeavyBullet)
            {
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(1f, 0.78f, 0.22f), 0.3f),
                        new GradientColorKey(new Color(1f, 0.42f, 0.05f), 0.75f),
                        new GradientColorKey(new Color(0.85f, 0.15f, 0.02f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.95f, 0.65f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else
            {
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0.4f, 0.88f, 1f), 0.35f),
                        new GradientColorKey(new Color(0.12f, 0.6f, 1f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.85f, 0.55f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            trail.colorGradient = grad;
            trail.emitting = true;

            // 3. Wake particles (ionization motes or molten slag sparks)
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps == null && sparksMaterial != null)
            {
                var psGo = new GameObject(isHeavyBullet ? "EmbersAndSparks" : "TrailEnergy");
                psGo.transform.SetParent(transform, false);
                ps = psGo.AddComponent<ParticleSystem>();

                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = true;

                var em = ps.emission;
                em.rateOverTime = 0;

                var psr = psGo.GetComponent<ParticleSystemRenderer>();
                psr.material = sparksMaterial;

                if (isHeavyBullet)
                {
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
                    main.gravityModifier = 0.25f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                    main.startColor = new Color(1f, 0.7f, 0.18f, 1f);
                    em.rateOverDistance = 45f;

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.04f;

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    Gradient colGrad = new Gradient();
                    colGrad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) }
                    );
                    col.color = colGrad;

                    var sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                }
                else
                {
                    main.startLifetime = 0.22f;
                    main.startSpeed = 0f;
                    main.startSize = 0.045f;
                    main.startColor = new Color(0.6f, 0.92f, 1f, 0.85f);
                    em.rateOverDistance = 30f;

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.02f;

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    Gradient colGrad = new Gradient();
                    colGrad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.4f, 0.85f, 1f), 1f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                    );
                    col.color = colGrad;

                    var sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                }
            }
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
            _isInitialized = false;

            // 1. Immediately extinguish dynamic bullet light so it doesn't linger at impact point
            if (_bulletLight != null)
            {
                _bulletLight.enabled = false;
            }

            // 2. Spawn impact effect at exact hit location oriented along surface normal
            if (_hitEffectPrefab != null)
            {
                Quaternion rot = (_hitNormal != Vector3.zero) ? Quaternion.LookRotation(_hitNormal) : Quaternion.identity;
                GameObject effect = Instantiate(_hitEffectPrefab, _targetPosition, rot);

                var cleanup = effect.GetComponent<VFXAutoCleanup>();
                if (cleanup != null)
                {
                    cleanup.SetCharacterHit(_isCharacterHit);
                }
            }

            // 3. Smooth trail ribbon dissipation: stop emitting new points so existing vapor streak fades out gracefully
            float fadeTime = 0.35f;
            var trails = GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].emitting = false;
                if (trails[i].time > fadeTime) fadeTime = trails[i].time;
            }

            var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // 4. Immediately hide projectile head renderers
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!(renderers[i] is TrailRenderer) && !(renderers[i] is ParticleSystemRenderer))
                {
                    renderers[i].enabled = false;
                }
            }

            // 5. Destroy bullet GameObject after contrail ribbon completes its fade
            Destroy(gameObject, fadeTime + 0.05f);
        }
    }
}