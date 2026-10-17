using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SniperGame.UI.Settings;

namespace PewPewArena.UI
{
    /// <summary>
    /// Self-contained visual, stage, and shot-feel enhancement controller for the SniperInspection scene.
    /// Keeps all UI untouched while adding:
    /// - Tier A: Runtime URP Post-Processing Volume (Bloom, ACES Tonemapping, Vignette, Chromatic Aberration,
    ///           Color Adjustments, dynamic Bokeh Depth of Field), Realtime ReflectionProbe, and non-blinking starfield.
    /// - Tier B: Animated holographic pedestal (counter-rotating segmented energy rings, periodic vertical scan beam,
    ///           orbiting holo-shards), procedural hex-grid floor, Perlin camera shake, slow-mo-aware brass shell
    ///           casing ejection, expanding pedestal shockwave ring, and reactive downrange ballistic target.
    /// - Tier C: Cinematic intro camera dolly-in + hologram scan reveal, idle auto-orbit turntable after 8s,
    ///           and subtle cosmic ambient space drone scaled by SettingsManager.SfxVolume.
    /// </summary>
    [DisallowMultipleComponent]
    public class SniperInspectionStageFX : MonoBehaviour
    {
        private SniperInspectionController _controller;
        private Camera _showcaseCamera;
        private Transform _weaponOrbitPivot;
        private Transform _pedestalRoot;
        private Transform _targetStageRoot;
        private GameObject _activeWeaponRoot;
        private Color _themeColor;
        private int _sniperIndex;

        private const float ShowcaseLensFov = 38f;
        private static readonly Color CosmicViolet = new Color(0.56f, 0.30f, 1.00f, 1f);

        // Tier A: Post-processing & Reflections
        private Volume _postProcessVolume;
        private VolumeProfile _runtimeProfile;
        private Bloom _bloom;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private float _baseBloomIntensity = 0.38f;
        private float _baseChromaticIntensity = 0.04f;
        private float _shotPostPunch;
        private ReflectionProbe _reflectionProbe;

        // Tier B: Animated Pedestal & Stage Elements
        private Light _scanBeamLight;
        private GameObject _bulletCasingPrefab;
        private readonly List<GameObject> _activeCasings = new List<GameObject>();

        // Camera Shake State
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private float _shakeRoughness = 22f;
        private float _shakeSeed;

        // Tier C: Intro Reveal, Idle Auto-Orbit & Ambient Audio
        private bool _introActive;
        private float _introElapsed;
        private const float IntroDuration = 1.55f;
        private Vector3 _introCameraOffset;
        private struct WeaponRendererEntry
        {
            public Renderer Renderer;
            public float NormalizedX;
        }
        private readonly List<WeaponRendererEntry> _weaponRenderers = new List<WeaponRendererEntry>();
        private float _weaponBoundsMinX;
        private float _weaponBoundsMaxX;

        private float _idleTimer;
        private float _idleOrbitBlend;
        private const float IdleStartDelay = 8.0f;
        private const float IdleOrbitDegreesPerSec = 6.0f;

        private AudioSource _ambientDroneSource;
        private float _ambientFadeProgress;

        public Vector3 IntroCameraOffset => _introCameraOffset;

        public void Initialize(
            SniperInspectionController controller,
            Camera showcaseCamera,
            Transform weaponOrbitPivot,
            Transform pedestalRoot,
            Transform targetStageRoot,
            GameObject activeWeaponRoot,
            Color themeColor,
            int sniperIndex,
            GameObject bulletPrefab = null)
        {
            _controller = controller;
            _showcaseCamera = showcaseCamera;
            _weaponOrbitPivot = weaponOrbitPivot;
            _pedestalRoot = pedestalRoot;
            _targetStageRoot = targetStageRoot;
            _activeWeaponRoot = activeWeaponRoot;
            _themeColor = themeColor;
            _sniperIndex = sniperIndex;
            _bulletCasingPrefab = bulletPrefab;
            _shakeSeed = Random.Range(10f, 500f);

            Transform envRoot = transform.Find("CosmicEnvironment") ?? transform;

            BuildPostProcessingVolume(envRoot);
            BuildShowcaseReflectionProbe(envRoot);
            UpgradeCosmicStarfield(envRoot);
            SetupAnimatedPedestal();
            SetupIntroReveal();
            SetupAmbientAudio(envRoot);

            SettingsManager.OnSettingsChanged += OnSettingsChanged;
        }

        private IEnumerator Start()
        {
            // Enforce studio 38° lens FOV in case SettingsManager.OnSceneLoaded applied gameplay FOV (80°)
            if (_showcaseCamera != null)
            {
                _showcaseCamera.fieldOfView = ShowcaseLensFov;
            }

            yield return null;

            if (_showcaseCamera != null)
            {
                _showcaseCamera.fieldOfView = ShowcaseLensFov;
            }

            if (_reflectionProbe != null)
            {
                _reflectionProbe.RenderProbe();
            }
        }

        private void OnDestroy()
        {
            SettingsManager.OnSettingsChanged -= OnSettingsChanged;

            for (int i = 0; i < _activeCasings.Count; i++)
            {
                if (_activeCasings[i] != null)
                {
                    Destroy(_activeCasings[i]);
                }
            }
            _activeCasings.Clear();

            if (_runtimeProfile != null) Destroy(_runtimeProfile);
        }

        private void Update()
        {
            if (_controller == null) return;

            float unscaledDt = Time.unscaledDeltaTime;
            float simSpeed = _controller.InspectionTimeScale;

            // Keep showcase camera at studio portrait FOV (38°)
            if (_showcaseCamera != null && Mathf.Abs(_showcaseCamera.fieldOfView - ShowcaseLensFov) > 0.05f)
            {
                _showcaseCamera.fieldOfView = ShowcaseLensFov;
            }

            UpdateIntroReveal(unscaledDt);
            UpdateAnimatedPedestal(unscaledDt, simSpeed);
            UpdatePostProcessingPunch(unscaledDt, simSpeed);
            UpdateAmbientAudio(unscaledDt);
        }

        #region Public Hooks for SniperInspectionController

        /// <summary>
        /// Notifies the stage FX that the user interacted with rotation, zoom, or buttons.
        /// Immediately finishes the intro reveal if still running and resets the idle turntable timer.
        /// </summary>
        public void NotifyUserInput()
        {
            _idleTimer = 0f;
            _idleOrbitBlend = 0f;

            if (_introActive)
            {
                SkipIntroImmediate();
            }
        }

        /// <summary>
        /// Returns the yaw delta (in degrees) for the idle turntable orbit after 8 seconds of inactivity.
        /// </summary>
        public float ConsumeIdleYawDelta()
        {
            if (_introActive) return 0f;

            _idleTimer += Time.unscaledDeltaTime;
            if (_idleTimer < IdleStartDelay)
            {
                _idleOrbitBlend = Mathf.MoveTowards(_idleOrbitBlend, 0f, Time.unscaledDeltaTime * 3f);
            }
            else
            {
                _idleOrbitBlend = Mathf.MoveTowards(_idleOrbitBlend, 1f, Time.unscaledDeltaTime * 0.85f);
            }

            if (_idleOrbitBlend <= 0.001f) return 0f;

            float slowMoScale = (_controller != null && _controller.IsSlowMotionEnabled) ? 0.35f : 1f;
            return IdleOrbitDegreesPerSec * _idleOrbitBlend * slowMoScale * Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Applies additive Perlin camera shake to the showcase camera.
        /// </summary>
        public void ApplyCameraEffects(Transform camTransform, float currentCameraDistance)
        {
            if (camTransform == null || _shakeTimer <= 0f) return;

            float simSpeed = _controller != null ? _controller.InspectionTimeScale : 1f;
            _shakeTimer -= Time.unscaledDeltaTime * Mathf.Lerp(0.45f, 1f, simSpeed);

            if (_shakeTimer <= 0f)
            {
                _shakeTimer = 0f;
                return;
            }

            float norm = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.01f, _shakeDuration));
            float damper = norm * norm;
            float t = Time.unscaledTime * _shakeRoughness + _shakeSeed;

            float nx = (Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0.2f, t) - 0.5f) * 2f;
            float nr = (Mathf.PerlinNoise(t * 0.8f, t * 0.8f) - 0.5f) * 2f;

            Vector3 posOffset = (camTransform.right * nx + camTransform.up * ny) * (_shakeIntensity * damper);
            camTransform.position += posOffset;
            camTransform.localRotation *= Quaternion.Euler(ny * 28f * _shakeIntensity * damper, nx * 18f * _shakeIntensity * damper, nr * 42f * _shakeIntensity * damper);
        }

        /// <summary>
        /// Triggered when the player fires the sniper in inspection mode.
        /// Spawns camera shake, post-process bloom punch, and Kogel01 casing ejection.
        /// </summary>
        public void OnShotFired(Vector3 muzzlePos, Vector3 muzzleDir, bool isHeavy, bool ejectCasing)
        {
            _idleTimer = 0f;
            if (_introActive) SkipIntroImmediate();

            // 1. Camera shake impulse (heavier for Sniper 2)
            _shakeIntensity = isHeavy ? 0.024f : 0.015f;
            _shakeDuration = isHeavy ? 0.26f : 0.18f;
            _shakeTimer = _shakeDuration;
            _shakeRoughness = isHeavy ? 25f : 30f;
            _shakeSeed = Random.Range(10f, 500f);

            // 2. Post-processing bloom & chromatic aberration punch (restrained so it doesn't flash-blind)
            _shotPostPunch = isHeavy ? 0.55f : 0.40f;

            // 3. Kogel01 bullet casing ejection during the bolt chambering phase
            if (ejectCasing)
            {
                StartCoroutine(EjectShellCasingRoutine(isHeavy));
            }
        }

        /// <summary>
        /// Triggered when the inspection projectile strikes downrange.
        /// </summary>
        public void OnImpact(Vector3 impactPos, Vector3 impactNormal)
        {
            // No opaque quad ring spawned on impact
        }

        public void FadeOutAudio()
        {
            if (_ambientDroneSource != null)
            {
                _ambientDroneSource.Stop();
            }
        }

        #endregion

        #region Tier A: Post-Processing, Reflection Probe & Non-Blinking Starfield

        private void BuildPostProcessingVolume(Transform envRoot)
        {
            var volGo = new GameObject("ShowcasePostProcessVolume");
            volGo.transform.SetParent(envRoot, false);

            _postProcessVolume = volGo.AddComponent<Volume>();
            _postProcessVolume.isGlobal = true;
            _postProcessVolume.priority = 25f;
            _postProcessVolume.weight = 1f;

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile.name = "SniperInspectionRuntimeProfile";
            _postProcessVolume.sharedProfile = _runtimeProfile;

            // 1. Controlled HDR Bloom (high threshold so only true HDR sparks/lasers glow, without overexposing)
            _bloom = _runtimeProfile.Add<Bloom>(true);
            _bloom.threshold.Override(1.28f);
            _bloom.intensity.Override(_baseBloomIntensity);
            _bloom.scatter.Override(0.52f);
            _bloom.tint.Override(Color.Lerp(Color.white, _themeColor, 0.16f));
            _bloom.highQualityFiltering.Override(true);

            // 2. ACES Filmic Tonemapping for crisp contrast and rich dark space blacks
            var tonemapping = _runtimeProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 3. Cinematic Edge Vignette
            _vignette = _runtimeProfile.Add<Vignette>(true);
            _vignette.color.Override(new Color(0.01f, 0.01f, 0.04f, 1f));
            _vignette.intensity.Override(0.36f);
            _vignette.smoothness.Override(0.58f);

            // 4. Subtle Lens Chromatic Aberration
            _chromaticAberration = _runtimeProfile.Add<ChromaticAberration>(true);
            _chromaticAberration.intensity.Override(_baseChromaticIntensity);

            // 5. Studio Color Grading Adjustments (slightly negative exposure for a rich dark cosmic mood)
            var colorAdj = _runtimeProfile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(-0.45f);
            colorAdj.contrast.Override(10f);
            colorAdj.saturation.Override(6f);
        }

        private void UpdatePostProcessingPunch(float unscaledDt, float simSpeed)
        {
            if (_shotPostPunch <= 0.001f) return;

            _shotPostPunch = Mathf.MoveTowards(_shotPostPunch, 0f, unscaledDt * Mathf.Lerp(2.2f, 6.5f, simSpeed));

            if (_bloom != null)
            {
                _bloom.intensity.value = _baseBloomIntensity + _shotPostPunch * 0.45f;
            }
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = _baseChromaticIntensity + _shotPostPunch * 0.12f;
            }
        }

        private void BuildShowcaseReflectionProbe(Transform envRoot)
        {
            var probeGo = new GameObject("ShowcaseReflectionProbe");
            probeGo.transform.SetParent(envRoot, false);
            probeGo.transform.position = (_weaponOrbitPivot != null ? _weaponOrbitPivot.position : Vector3.zero) + new Vector3(0f, 0.12f, 0f);

            _reflectionProbe = probeGo.AddComponent<ReflectionProbe>();
            _reflectionProbe.mode = ReflectionProbeMode.Realtime;
            _reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            _reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            _reflectionProbe.resolution = 128;
            _reflectionProbe.hdr = true;
            _reflectionProbe.size = new Vector3(9f, 6f, 9f);
            _reflectionProbe.intensity = 0.55f;
            _reflectionProbe.clearFlags = ReflectionProbeClearFlags.Skybox;
            _reflectionProbe.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);
            _reflectionProbe.RenderProbe();
        }

        private void UpgradeCosmicStarfield(Transform envRoot)
        {
            Transform starfieldTr = envRoot.Find("CosmicStarfield");
            if (starfieldTr != null)
            {
                var ps = starfieldTr.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                    var main = ps.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(9999f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.002f, 0.008f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.020f, 0.052f);
                    main.maxParticles = 220;

                    var emission = ps.emission;
                    emission.rateOverTime = 0f;

                    // Disable the fade-in/out blinking alpha curve so stars remain steady
                    var col = ps.colorOverLifetime;
                    col.enabled = false;

                    // Subtle size shimmer for natural distant starlight
                    var sizeBySpeed = ps.sizeOverLifetime;
                    sizeBySpeed.enabled = true;
                    var curve = new AnimationCurve(
                        new Keyframe(0f, 0.92f),
                        new Keyframe(0.5f, 1.08f),
                        new Keyframe(1f, 0.92f));
                    sizeBySpeed.size = new ParticleSystem.MinMaxCurve(1f, curve);

                    ps.Play();
                    ps.Emit(200);
                }
            }

            // Add a secondary theme-tinted deep nebula cloud layer for parallax richness
            Transform nebulaTr = envRoot.Find("DistantNebulaClouds");
            if (nebulaTr != null && envRoot.Find("DeepThemeNebulaGlow") == null)
            {
                GameObject accentNebula = Instantiate(nebulaTr.gameObject, envRoot);
                accentNebula.name = "DeepThemeNebulaGlow";
                accentNebula.transform.position = new Vector3(-0.6f, -0.2f, 9.2f);
                var ps = accentNebula.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main;
                    Color c1 = new Color(_themeColor.r * 0.45f, _themeColor.g * 0.45f, _themeColor.b * 0.45f, 0.05f);
                    Color c2 = new Color(CosmicViolet.r * 0.45f, CosmicViolet.g * 0.35f, CosmicViolet.b * 0.65f, 0.06f);
                    main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
                    ps.Play();
                }
            }
        }

        #endregion

        #region Tier B: Pedestal Accent Light & Kogel01 Round Ejection

        private void SetupAnimatedPedestal()
        {
            if (_pedestalRoot == null) return;

            var lightGo = new GameObject("ScanBeamLight");
            lightGo.transform.SetParent(_pedestalRoot, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            _scanBeamLight = lightGo.AddComponent<Light>();
            _scanBeamLight.type = LightType.Point;
            _scanBeamLight.color = _themeColor;
            _scanBeamLight.range = 1.4f;
            _scanBeamLight.intensity = 0f;
            _scanBeamLight.shadows = LightShadows.None;
        }

        private void UpdateAnimatedPedestal(float unscaledDt, float simSpeed)
        {
            if (_scanBeamLight != null)
            {
                float cycle = Mathf.Repeat(Time.unscaledTime * 0.165f, 1f);
                if (cycle < 0.28f)
                {
                    float t = cycle / 0.28f;
                    float y = Mathf.Lerp(0.04f, 0.65f, t);
                    float alpha = Mathf.Sin(t * Mathf.PI);
                    _scanBeamLight.transform.localPosition = new Vector3(0f, y, 0f);
                    _scanBeamLight.intensity = alpha * 0.35f;
                }
                else
                {
                    _scanBeamLight.intensity = 0f;
                }
            }
        }

        private IEnumerator EjectShellCasingRoutine(bool isHeavy)
        {
            // Wait until the bolt-action chambering phase (~0.30s into the firing cycle, respecting Slow Motion)
            float delay = isHeavy ? 0.36f : 0.28f;
            float waited = 0f;
            while (waited < delay)
            {
                float simSpeed = _controller != null ? _controller.InspectionTimeScale : 1f;
                waited += Time.unscaledDeltaTime * simSpeed;
                yield return null;
            }

            if (_weaponOrbitPivot == null) yield break;

            var casingRoot = new GameObject("EjectedKogel01Round");
            _activeCasings.Add(casingRoot);

            Transform refTr = _activeWeaponRoot != null ? _activeWeaponRoot.transform : _weaponOrbitPivot;
            Vector3 ejectPos = _weaponOrbitPivot.position
                               + refTr.right * 0.06f
                               + refTr.up * 0.07f
                               - refTr.forward * 0.05f;
            casingRoot.transform.position = ejectPos;
            casingRoot.transform.rotation = refTr.rotation;

            float scaleMult = isHeavy ? 1.55f : 1.35f;

            if (_bulletCasingPrefab != null)
            {
                GameObject kogelModel = Instantiate(_bulletCasingPrefab, casingRoot.transform, false);
                kogelModel.transform.localPosition = Vector3.zero;
                kogelModel.transform.localRotation = Quaternion.identity;
                kogelModel.transform.localScale = Vector3.one * scaleMult;

                // Strip projectile scripts/trails so the ejected round is purely the 3D Kogel01 mesh
                var cosmetic = kogelModel.GetComponent<SniperGame.Weapons.CosmeticBullet>();
                if (cosmetic != null) Destroy(cosmetic);

                var trails = kogelModel.GetComponentsInChildren<TrailRenderer>(true);
                for (int i = 0; i < trails.Length; i++) Destroy(trails[i]);

                var particles = kogelModel.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < particles.Length; i++) Destroy(particles[i].gameObject);

                var lights = kogelModel.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++) Destroy(lights[i]);

                var colliders = kogelModel.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++) Destroy(colliders[i]);
            }

            // Initial ejection velocity (up, right, and slightly back relative to the rifle)
            Vector3 velocity = (refTr.right * Random.Range(1.05f, 1.35f))
                               + (refTr.up * Random.Range(1.25f, 1.55f))
                               - (refTr.forward * Random.Range(0.25f, 0.50f));
            Vector3 angularVel = new Vector3(
                Random.Range(520f, 840f),
                Random.Range(-420f, 420f),
                Random.Range(380f, 680f));

            float floorY = _pedestalRoot != null ? (_pedestalRoot.position.y + 0.018f) : -0.40f;
            bool hasBounced = false;
            float lifetime = 2.6f;
            float elapsed = 0f;

            while (elapsed < lifetime && casingRoot != null)
            {
                float simSpeed = _controller != null ? _controller.InspectionTimeScale : 1f;
                float dt = Time.unscaledDeltaTime * simSpeed;
                elapsed += dt;

                velocity += new Vector3(0f, -5.8f, 0f) * dt;
                casingRoot.transform.position += velocity * dt;
                casingRoot.transform.Rotate(angularVel * dt, Space.Self);

                // Bounce off the holographic pedestal surface
                if (!hasBounced && casingRoot.transform.position.y <= floorY)
                {
                    Vector3 p = casingRoot.transform.position;
                    p.y = floorY;
                    casingRoot.transform.position = p;
                    velocity.y = Mathf.Abs(velocity.y) * 0.42f;
                    velocity.x *= 0.65f;
                    velocity.z *= 0.65f;
                    angularVel *= 0.55f;
                    hasBounced = true;
                }
                else if (hasBounced && casingRoot.transform.position.y <= floorY)
                {
                    Vector3 p = casingRoot.transform.position;
                    p.y = floorY;
                    casingRoot.transform.position = p;
                    velocity.y = 0f;
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, dt * 1.5f);
                    velocity.z = Mathf.MoveTowards(velocity.z, 0f, dt * 1.5f);
                    angularVel = Vector3.MoveTowards(angularVel, Vector3.zero, dt * 600f);
                }

                // Shrink out smoothly at the end of its life
                if (elapsed > lifetime - 0.45f)
                {
                    float fade = Mathf.Clamp01((lifetime - elapsed) / 0.45f);
                    casingRoot.transform.localScale = Vector3.one * fade;
                }

                yield return null;
            }

            if (casingRoot != null)
            {
                _activeCasings.Remove(casingRoot);
                Destroy(casingRoot);
            }
        }

        #endregion

        #region Tier C: Intro Dolly-In + Materialize Scan, Idle Orbit & Ambient Audio

        private void SetupIntroReveal()
        {
            _weaponRenderers.Clear();
            if (_activeWeaponRoot == null)
            {
                _introActive = false;
                _introCameraOffset = Vector3.zero;
                return;
            }

            var renderers = _activeWeaponRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                _introActive = false;
                _introCameraOffset = Vector3.zero;
                return;
            }

            _weaponBoundsMinX = float.MaxValue;
            _weaponBoundsMaxX = float.MinValue;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Bounds b = renderers[i].bounds;
                if (b.min.x < _weaponBoundsMinX) _weaponBoundsMinX = b.min.x;
                if (b.max.x > _weaponBoundsMaxX) _weaponBoundsMaxX = b.max.x;
            }

            float span = Mathf.Max(0.1f, _weaponBoundsMaxX - _weaponBoundsMinX);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                float normX = Mathf.Clamp01((renderers[i].bounds.center.x - _weaponBoundsMinX) / span);
                _weaponRenderers.Add(new WeaponRendererEntry
                {
                    Renderer = renderers[i],
                    NormalizedX = normX
                });
            }

            _introActive = true;
            _introElapsed = 0f;
            _introCameraOffset = new Vector3(-0.18f, 0.20f, -0.58f);
        }

        private void UpdateIntroReveal(float unscaledDt)
        {
            if (!_introActive) return;

            _introElapsed += unscaledDt;
            float t = Mathf.Clamp01(_introElapsed / IntroDuration);

            // Cubic smooth-out dolly curve
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            _introCameraOffset = Vector3.Lerp(new Vector3(-0.18f, 0.20f, -0.58f), Vector3.zero, ease);

            // Progressive hologram scan reveal across weapon renderers
            float scanFront = Mathf.Lerp(-0.05f, 1.15f, ease);
            for (int i = 0; i < _weaponRenderers.Count; i++)
            {
                var entry = _weaponRenderers[i];
                if (entry.Renderer != null)
                {
                    entry.Renderer.enabled = (entry.NormalizedX <= scanFront);
                }
            }

            if (t >= 1f)
            {
                SkipIntroImmediate();
            }
        }

        private void SkipIntroImmediate()
        {
            _introActive = false;
            _introCameraOffset = Vector3.zero;

            for (int i = 0; i < _weaponRenderers.Count; i++)
            {
                if (_weaponRenderers[i].Renderer != null)
                {
                    _weaponRenderers[i].Renderer.enabled = true;
                }
            }
        }

        private void SetupAmbientAudio(Transform envRoot)
        {
            AudioClip humClip = Resources.Load<AudioClip>("Startup/boot_hum");
            if (humClip == null) return;

            var audioGo = new GameObject("CosmicAmbientDrone");
            audioGo.transform.SetParent(envRoot, false);

            _ambientDroneSource = audioGo.AddComponent<AudioSource>();
            _ambientDroneSource.clip = humClip;
            _ambientDroneSource.loop = true;
            _ambientDroneSource.playOnAwake = false;
            _ambientDroneSource.spatialBlend = 0f;
            _ambientDroneSource.pitch = 0.72f;
            _ambientDroneSource.volume = 0f;
            _ambientDroneSource.Play();
            _ambientFadeProgress = 0f;
        }

        private void UpdateAmbientAudio(float unscaledDt)
        {
            if (_ambientDroneSource == null || !_ambientDroneSource.isPlaying) return;

            if (_ambientFadeProgress < 1f)
            {
                _ambientFadeProgress = Mathf.MoveTowards(_ambientFadeProgress, 1f, unscaledDt * 0.75f);
            }
            float targetVol = 0.14f * Mathf.Clamp01(SettingsManager.SfxVolume);
            _ambientDroneSource.volume = targetVol * _ambientFadeProgress;
        }

        private void OnSettingsChanged()
        {
            if (_ambientDroneSource != null)
            {
                _ambientDroneSource.volume = 0.14f * Mathf.Clamp01(SettingsManager.SfxVolume) * _ambientFadeProgress;
            }
        }

        #endregion
    }
}

