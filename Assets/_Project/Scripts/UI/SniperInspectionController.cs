using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SniperGame.Weapons;
using SniperGame.UI;

namespace PewPewArena.UI
{
    /// <summary>
    /// Controls the dedicated "SniperInspection" dark cosmic weapon showcase scene:
    /// - Loads Sniper 1 or Sniper 2 based on the selection from LoadoutPanel.
    /// - Builds and manages the dark cosmic space showcase environment (starfield, nebulae,
    ///   3-point rim/key/fill studio lighting, holographic pedestal, and ballistic target stage).
    /// - Interactive 360° horizontal & clamped vertical 3D weapon rotation + safe zoom + Reset View.
    /// - Inspection-safe Shoot presentation (recoil & bolt animation, gunshot audio, muzzle flash,
    ///   tracer projectile, and target impact VFX) with zero multiplayer or gameplay side effects.
    /// - Dedicated "Inspect VFX" drawer allowing individual on-demand playback & camera framing
    ///   for Muzzle Flash, Projectile/Tracer, Impact Sparks, or Full Sequence.
    /// - Dedicated "Slow Motion" toggle (0.20x) that slows animation playback, particle simulation,
    ///   light decay, and tracer flight without altering global Time.timeScale or freezing the UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class SniperInspectionController : MonoBehaviour
    {
        public const string InspectionSceneName = "SniperInspection";
        public const string MainMenuSceneName = "01_MainMenu";

        public static int SelectedSniperIndex { get; private set; } = 0;

        [Header("Scene & Stage References")]
        [SerializeField] private Camera showcaseCamera;
        [SerializeField] private Transform weaponOrbitPivot;
        [SerializeField] private GameObject sniper1SceneInstance;
        [SerializeField] private GameObject sniper2SceneInstance;
        [SerializeField] private Material cosmicSkyboxMaterial;

        [Header("Weapon Prefab Fallbacks")]
        [SerializeField] private GameObject sniper1HolderPrefab;
        [SerializeField] private GameObject sniper1ModelPrefab;
        [SerializeField] private GameObject sniper2ModelPrefab;

        [Header("Sniper 1 VFX & Audio")]
        [SerializeField] private GameObject muzzleFlashPrefab1;
        [SerializeField] private GameObject bulletPrefab1;
        [SerializeField] private GameObject hitEffectPrefab1;
        [SerializeField] private AudioClip gunshotClip1;

        [Header("Sniper 2 VFX & Audio")]
        [SerializeField] private GameObject muzzleFlashPrefab2;
        [SerializeField] private GameObject bulletPrefab2;
        [SerializeField] private GameObject hitEffectPrefab2;
        [SerializeField] private AudioClip gunshotClip2;

        [Header("Rotation & Framing Settings")]
        [SerializeField] private float defaultYaw = 68f;
        [SerializeField] private float defaultPitch = 5f;
        [SerializeField] private float minPitch = -52f;
        [SerializeField] private float maxPitch = 52f;
        [SerializeField] private float dragSensitivity = 0.32f;
        [SerializeField] private float rotationSmoothing = 14f;
        [SerializeField] private float defaultCameraDistance = 1.58f;
        [SerializeField] private float minCameraDistance = 1.08f;
        [SerializeField] private float maxCameraDistance = 2.25f;

        [Header("Slow Motion Settings")]
        [SerializeField] private float slowMotionMultiplier = 0.20f;

        // Colours matching LoadoutPanelTheme + Dark Cosmic palette
        private static readonly Color CyanTheme = new Color(0.00f, 0.83f, 1.00f, 1f);
        private static readonly Color AmberTheme = new Color(1.00f, 0.71f, 0.00f, 1f);
        private static readonly Color CosmicViolet = new Color(0.56f, 0.30f, 1.00f, 1f);
        private static readonly Color DarkPanelBg = new Color(0.03f, 0.05f, 0.10f, 0.88f);
        private static readonly Color MutedText = new Color(0.74f, 0.82f, 0.90f, 1f);

        private Transform _floatPivot;
        private Transform _recoilPivot;
        private Transform _centerOffsetPivot;
        private GameObject _activeWeaponRoot;
        private Transform _activeFirePoint;
        private Animator _activeAnimator;
        private AudioSource _weaponAudioSource;
        private AudioSource _uiAudioSource;

        // Environment & Lighting
        private Light _keyLight;
        private Light _rimLight;
        private Light _underGlowLight;
        private Light _muzzleFlashLight;
        private Transform _pedestalRoot;
        private Transform _targetStageRoot;
        private Renderer[] _themeRingRenderers;

        // Interactive Rotation & Camera State
        private float _currentYaw;
        private float _currentPitch;
        private float _targetYaw;
        private float _targetPitch;
        private float _currentCameraDist;
        private float _targetCameraDist;
        private Vector3 _defaultStagePosition = new Vector3(-0.14f, 0.03f, 0f);
        private Vector3 _currentCameraLookOffset = Vector3.zero;
        private Vector3 _targetCameraLookOffset = Vector3.zero;
        private bool _isDraggingWeapon;

        // Firing, VFX & Slow Motion State
        private bool _isSlowMotion;
        private bool _isVfxDrawerOpen;
        private Coroutine _firingAnimCoroutine;
        private Coroutine _muzzleLightCoroutine;
        private readonly List<GameObject> _spawnedVfxObjects = new List<GameObject>();

        // UI Elements
        private TMP_FontAsset _titleFont;
        private TMP_FontAsset _bodyFont;
        private Canvas _hudCanvas;
        private TextMeshProUGUI _slowMoButtonLabel;
        private Image _slowMoButtonBg;
        private Image _slowMoButtonFrame;
        private Image _slowMoStatusDot;
        private TextMeshProUGUI _inspectVfxButtonLabel;
        private Image _inspectVfxButtonBg;
        private Image _inspectVfxButtonFrame;
        private GameObject _vfxDrawerPanel;
        private TextMeshProUGUI _activeModeBannerText;
        private CanvasGroup _activeModeBannerGroup;

        private ParticleSystem _sharedVfxParticleTemplate;
        private Material _sharedAdditiveParticleMat;

        public float InspectionTimeScale => _isSlowMotion ? slowMotionMultiplier : 1f;
        public bool IsSlowMotionEnabled => _isSlowMotion;

        /// <summary>
        /// Sets the selected sniper index (0 = Sniper 1, 1 = Sniper 2) and transitions into SniperInspection.
        /// Preserves the player's equipped loadout selection in RelayNetworkUI.
        /// </summary>
        public static void OpenInspection(int sniperIndex)
        {
            SelectedSniperIndex = Mathf.Clamp(sniperIndex, 0, 1);
            RelayNetworkUI.ReturnToLoadoutFromInspection = true;
            Time.timeScale = 1f;

            Debug.Log($"[SniperInspection] Launching '{InspectionSceneName}' for Sniper {SelectedSniperIndex + 1} (index {SelectedSniperIndex}).");
            SceneManager.LoadScene(InspectionSceneName);
        }

        public static void SetSelectedSniperForInspection(int sniperIndex)
        {
            SelectedSniperIndex = Mathf.Clamp(sniperIndex, 0, 1);
        }

        private void Awake()
        {
            // Always keep global Time.timeScale at 1 so UI and menus never freeze
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SelectedSniperIndex = Mathf.Clamp(SelectedSniperIndex, 0, 1);

            _titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
            _bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");

            EnsureEventSystem();
            EnsureAudioSources();
            SetupCameraAndPivots();
            SetupCosmicEnvironment();
            LoadSelectedSniper(SelectedSniperIndex);
            BuildInspectionUI();

            ResetViewImmediate();
        }

        private void OnDestroy()
        {
            CleanupSpawnedVfx();
            Time.timeScale = 1f;
        }

        #region Initialization & Cosmic Showcase Environment

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            esGo.transform.SetParent(transform, false);
        }

        private void EnsureAudioSources()
        {
            _weaponAudioSource = gameObject.GetComponent<AudioSource>();
            if (_weaponAudioSource == null)
            {
                _weaponAudioSource = gameObject.AddComponent<AudioSource>();
            }
            _weaponAudioSource.playOnAwake = false;
            _weaponAudioSource.spatialBlend = 0f;
            _weaponAudioSource.volume = 0.92f;

            var uiAudioGo = new GameObject("UIAudioSource", typeof(AudioSource));
            uiAudioGo.transform.SetParent(transform, false);
            _uiAudioSource = uiAudioGo.GetComponent<AudioSource>();
            _uiAudioSource.playOnAwake = false;
            _uiAudioSource.spatialBlend = 0f;
            _uiAudioSource.volume = 0.55f;
        }

        private void SetupCameraAndPivots()
        {
            if (showcaseCamera == null)
            {
                showcaseCamera = Camera.main;
            }
            if (showcaseCamera == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                showcaseCamera = camGo.GetComponent<Camera>();
            }

            showcaseCamera.clearFlags = cosmicSkyboxMaterial != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            showcaseCamera.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);
            showcaseCamera.fieldOfView = 38f;
            showcaseCamera.nearClipPlane = 0.04f;
            showcaseCamera.farClipPlane = 500f;
            showcaseCamera.cullingMask = ~0;

            var urpCam = showcaseCamera.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam == null)
            {
                urpCam = showcaseCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            urpCam.renderPostProcessing = true;

            if (weaponOrbitPivot == null)
            {
                Transform existingOrbit = transform.Find("WeaponOrbitPivot");
                if (existingOrbit != null)
                {
                    weaponOrbitPivot = existingOrbit;
                }
                else
                {
                    var orbitGo = new GameObject("WeaponOrbitPivot");
                    orbitGo.transform.SetParent(transform, false);
                    weaponOrbitPivot = orbitGo.transform;
                }
            }
            weaponOrbitPivot.position = _defaultStagePosition;

            _floatPivot = GetOrCreateChild(weaponOrbitPivot, "WeaponFloatPivot");
            _recoilPivot = GetOrCreateChild(_floatPivot, "WeaponRecoilPivot");
            _centerOffsetPivot = GetOrCreateChild(_recoilPivot, "WeaponCenterOffset");
        }

        private void SetupCosmicEnvironment()
        {
            Color themeColor = GetThemeColor(SelectedSniperIndex);

            // 1. Skybox & Deep Cosmic Ambient Lighting
            if (cosmicSkyboxMaterial != null)
            {
                RenderSettings.skybox = cosmicSkyboxMaterial;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.08f, 0.12f, 0.24f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.14f, 0.28f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.11f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.08f, 1f);
            RenderSettings.fogDensity = 0.015f;

            Transform envRoot = GetOrCreateChild(transform, "CosmicEnvironment");

            // 2. Three-Point Cinematic Rim / Key / Fill Lighting Rig
            _keyLight = CreateShowcaseLight(envRoot, "StudioKeyLight", LightType.Spot,
                new Vector3(-0.95f, 1.15f, -1.35f),
                Quaternion.LookRotation(_defaultStagePosition - new Vector3(-0.95f, 1.15f, -1.35f)),
                new Color(0.95f, 0.97f, 1.00f, 1f), 4.2f, 8f, 58f);
            _keyLight.shadows = LightShadows.Soft;

            _rimLight = CreateShowcaseLight(envRoot, "CosmicRimLight", LightType.Spot,
                new Vector3(0.45f, 1.10f, 1.45f),
                Quaternion.LookRotation(_defaultStagePosition - new Vector3(0.45f, 1.10f, 1.45f)),
                new Color(0.48f, 0.36f, 1.00f, 1f), 5.5f, 8f, 65f);

            _underGlowLight = CreateShowcaseLight(envRoot, "PedestalUnderGlowLight", LightType.Point,
                _defaultStagePosition + new Vector3(0f, -0.36f, -0.15f),
                Quaternion.identity,
                Color.Lerp(themeColor, CosmicViolet, 0.25f), 2.6f, 3.2f, 45f);

            _muzzleFlashLight = CreateShowcaseLight(envRoot, "DynamicMuzzleLight", LightType.Point,
                _defaultStagePosition + new Vector3(0.55f, 0.05f, 0f),
                Quaternion.identity,
                themeColor, 0f, 4.5f, 45f);
            _muzzleFlashLight.enabled = false;

            // 3. Holographic Presentation Pedestal beneath the sniper
            BuildHolographicPedestal(envRoot, themeColor);

            // 4. Downrange Holographic Target Stage for projectile/impact showcase
            BuildBallisticTargetStage(envRoot, themeColor);

            // 5. Cosmic Starfield, Distant Nebulae, and Floating Zero-G Dust Particles
            BuildCosmicParticleAtmosphere(envRoot, themeColor);
        }

        private Light CreateShowcaseLight(Transform parent, string name, LightType type, Vector3 pos, Quaternion rot,
            Color color, float intensity, float range, float spotAngle)
        {
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = LightShadows.None;
            return light;
        }

        private void BuildHolographicPedestal(Transform envRoot, Color themeColor)
        {
            _pedestalRoot = GetOrCreateChild(envRoot, "HoloPedestal");
            _pedestalRoot.position = _defaultStagePosition + new Vector3(0f, -0.44f, 0f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material darkMetalMat = new Material(litShader)
            {
                color = new Color(0.05f, 0.07f, 0.12f, 1f)
            };
            if (darkMetalMat.HasProperty("_Smoothness")) darkMetalMat.SetFloat("_Smoothness", 0.78f);
            if (darkMetalMat.HasProperty("_Metallic")) darkMetalMat.SetFloat("_Metallic", 0.85f);

            Material glowRingMat = CreateEmissiveMaterial(themeColor, 2.4f);
            Material violetRingMat = CreateEmissiveMaterial(CosmicViolet, 1.8f);

            // Base metallic disc
            CreateCylinderPart(_pedestalRoot, "PedestalBase", new Vector3(0f, -0.04f, 0f), new Vector3(1.35f, 0.025f, 1.35f), darkMetalMat);
            CreateCylinderPart(_pedestalRoot, "PedestalInnerStep", new Vector3(0f, -0.01f, 0f), new Vector3(0.96f, 0.018f, 0.96f), darkMetalMat);

            // Glowing concentric holographic emitter rings
            var outerRing = CreateCylinderPart(_pedestalRoot, "OuterEnergyRing", new Vector3(0f, 0.008f, 0f), new Vector3(1.12f, 0.004f, 1.12f), glowRingMat);
            var innerRing = CreateCylinderPart(_pedestalRoot, "InnerVioletRing", new Vector3(0f, 0.012f, 0f), new Vector3(0.68f, 0.004f, 0.68f), violetRingMat);

            _themeRingRenderers = new Renderer[] { outerRing, innerRing };
        }

        private void BuildBallisticTargetStage(Transform envRoot, Color themeColor)
        {
            _targetStageRoot = GetOrCreateChild(envRoot, "BallisticTargetStage");
            // Default position downrange to the right-front where the barrel points
            _targetStageRoot.position = new Vector3(1.55f, 0.06f, 0.72f);
            _targetStageRoot.LookAt(_defaultStagePosition);

            Material targetRingMat = CreateEmissiveMaterial(Color.Lerp(themeColor, Color.white, 0.2f), 2.2f);
            Material coreMat = CreateEmissiveMaterial(new Color(themeColor.r, themeColor.g, themeColor.b, 0.35f), 1.4f);

            CreateCylinderPart(_targetStageRoot, "TargetDiscOuter", Vector3.zero, new Vector3(0.46f, 0.006f, 0.46f), targetRingMat)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreateCylinderPart(_targetStageRoot, "TargetDiscCore", new Vector3(0f, 0f, 0.01f), new Vector3(0.22f, 0.005f, 0.22f), coreMat)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private Renderer CreateCylinderPart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = name;
                go.transform.SetParent(parent, false);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return renderer;
        }

        private Material CreateEmissiveMaterial(Color color, float intensity)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader != null)
            {
                var mat = new Material(litShader);
                Color baseCol = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), color.a);
                Color hdrEmission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
                mat.color = baseCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", hdrEmission);
                return mat;
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Unlit/Color")
                                 ?? Shader.Find("Sprites/Default");
            var fallback = new Material(unlitShader);
            Color c = new Color(Mathf.Clamp01(color.r * intensity), Mathf.Clamp01(color.g * intensity), Mathf.Clamp01(color.b * intensity), color.a);
            fallback.color = c;
            if (fallback.HasProperty("_BaseColor")) fallback.SetColor("_BaseColor", c);
            return fallback;
        }

        private void BuildCosmicParticleAtmosphere(Transform envRoot, Color themeColor)
        {
            // Extract a working particle material from existing VFX prefabs so particles render cleanly in builds
            if (_sharedAdditiveParticleMat == null)
            {
                GameObject sampleVfx = muzzleFlashPrefab1 != null ? muzzleFlashPrefab1 : muzzleFlashPrefab2;
                if (sampleVfx != null)
                {
                    var samplePsr = sampleVfx.GetComponentInChildren<ParticleSystemRenderer>(true);
                    if (samplePsr != null && samplePsr.sharedMaterial != null)
                    {
                        _sharedAdditiveParticleMat = samplePsr.sharedMaterial;
                    }
                }
            }

            // 1. Deep Space Starfield
            CreateCosmicParticleLayer(envRoot, "CosmicStarfield",
                position: new Vector3(0f, 0.2f, 6f),
                boxSize: new Vector3(18f, 10f, 12f),
                maxParticles: 180,
                emissionRate: 28f,
                lifetimeMin: 4.5f, lifetimeMax: 8.0f,
                sizeMin: 0.025f, sizeMax: 0.065f,
                speedMin: 0.01f, speedMax: 0.04f,
                colorA: new Color(0.75f, 0.88f, 1.0f, 0.90f),
                colorB: new Color(0.62f, 0.45f, 1.0f, 0.75f));

            // 2. Subtle Floating Cosmic Dust Motes around the weapon
            CreateCosmicParticleLayer(envRoot, "CosmicFloatingDust",
                position: _defaultStagePosition + new Vector3(0.1f, 0.05f, 0.2f),
                boxSize: new Vector3(3.2f, 1.8f, 2.2f),
                maxParticles: 55,
                emissionRate: 10f,
                lifetimeMin: 3.5f, lifetimeMax: 6.0f,
                sizeMin: 0.012f, sizeMax: 0.028f,
                speedMin: 0.02f, speedMax: 0.06f,
                colorA: new Color(themeColor.r, themeColor.g, themeColor.b, 0.55f),
                colorB: new Color(0.58f, 0.38f, 1.0f, 0.45f));

            // 3. Distant Blue/Violet Nebula Glow Clouds
            CreateCosmicParticleLayer(envRoot, "DistantNebulaClouds",
                position: new Vector3(0.2f, 0.3f, 8.5f),
                boxSize: new Vector3(14f, 6.5f, 4f),
                maxParticles: 24,
                emissionRate: 3.5f,
                lifetimeMin: 6f, lifetimeMax: 10f,
                sizeMin: 2.4f, sizeMax: 4.2f,
                speedMin: 0.01f, speedMax: 0.03f,
                colorA: new Color(0.08f, 0.22f, 0.58f, 0.11f),
                colorB: new Color(0.32f, 0.10f, 0.55f, 0.10f));
        }

        private void CreateCosmicParticleLayer(Transform parent, string name, Vector3 position, Vector3 boxSize,
            int maxParticles, float emissionRate, float lifetimeMin, float lifetimeMax,
            float sizeMin, float sizeMax, float speedMin, float speedMax, Color colorA, Color colorB)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);

            var emission = ps.emission;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                if (_sharedAdditiveParticleMat != null)
                {
                    psr.sharedMaterial = _sharedAdditiveParticleMat;
                }
                else
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
                    var m = new Material(s);
                    m.mainTexture = LoadoutUISprites.RadialSoft.texture;
                    psr.sharedMaterial = m;
                }
                psr.shadowCastingMode = ShadowCastingMode.Off;
                psr.receiveShadows = false;
            }

            ps.Play();
        }

        #endregion

        #region Weapon Loading & Centering

        /// <summary>
        /// Initializes and centers the selected sniper variant (0 = Sniper 1, 1 = Sniper 2),
        /// ensuring no duplicate weapon instances are ever left active.
        /// </summary>
        public void LoadSelectedSniper(int sniperIndex)
        {
            SelectedSniperIndex = Mathf.Clamp(sniperIndex, 0, 1);

            // Deactivate both scene instances first
            if (sniper1SceneInstance != null) sniper1SceneInstance.SetActive(false);
            if (sniper2SceneInstance != null) sniper2SceneInstance.SetActive(false);

            // Clear any dynamically created children under _centerOffsetPivot that aren't the scene instances
            for (int i = _centerOffsetPivot.childCount - 1; i >= 0; i--)
            {
                Transform child = _centerOffsetPivot.GetChild(i);
                if (child.gameObject != sniper1SceneInstance && child.gameObject != sniper2SceneInstance)
                {
                    Destroy(child.gameObject);
                }
            }

            GameObject targetInstance = (SelectedSniperIndex == 0) ? sniper1SceneInstance : sniper2SceneInstance;

            // Fallback instantiation if scene references were not wired
            if (targetInstance == null)
            {
                targetInstance = CreateWeaponInstanceFallback(SelectedSniperIndex);
            }

            if (targetInstance == null)
            {
                Debug.LogError($"[SniperInspection] Failed to load weapon model for Sniper {SelectedSniperIndex + 1}!");
                return;
            }

            _activeWeaponRoot = targetInstance;
            _activeWeaponRoot.transform.SetParent(_centerOffsetPivot, false);
            _activeWeaponRoot.transform.localPosition = Vector3.zero;
            _activeWeaponRoot.transform.localRotation = Quaternion.identity;
            _activeWeaponRoot.transform.localScale = Vector3.one;
            _activeWeaponRoot.SetActive(true);

            // Pause Animator root position curve ("Idle Animation.anim" moves WeaponHolder to (0.216, -0.18, 0.145))
            // so our centering and procedural inspection recoil have full authority over the transform.
            _activeAnimator = _activeWeaponRoot.GetComponentInChildren<Animator>(true);
            if (_activeAnimator != null)
            {
                _activeAnimator.enabled = false;
            }

            // Set all renderers to Default layer (0) so studio lights and showcase camera render every detail
            SetLayerRecursively(_activeWeaponRoot.transform, 0);

            var renderers = _activeWeaponRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
                renderers[i].shadowCastingMode = ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }

            // Center the weapon based on its combined mesh bounds so 3D rotation orbits around the rifle's true center
            CenterWeaponByRendererBounds(_activeWeaponRoot);

            // Locate or create the barrel FirePoint
            _activeFirePoint = _activeWeaponRoot.transform.Find("FirePoint");
            if (_activeFirePoint == null)
            {
                foreach (Transform t in _activeWeaponRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.IndexOf("FirePoint", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _activeFirePoint = t;
                        break;
                    }
                }
            }

            if (_activeFirePoint == null)
            {
                var fpGo = new GameObject("FirePoint");
                fpGo.transform.SetParent(_activeWeaponRoot.transform, false);
                fpGo.transform.localPosition = new Vector3(0.0068f, 0.0754f, SelectedSniperIndex == 0 ? 1.1668f : 0.893f);
                _activeFirePoint = fpGo.transform;
            }

            // Update pedestal & under-glow theme accent color
            Color theme = GetThemeColor(SelectedSniperIndex);
            if (_underGlowLight != null) _underGlowLight.color = Color.Lerp(theme, CosmicViolet, 0.25f);
            if (_muzzleFlashLight != null) _muzzleFlashLight.color = theme;
        }

        private GameObject CreateWeaponInstanceFallback(int sniperIndex)
        {
            if (sniperIndex == 0)
            {
                if (sniper1HolderPrefab != null)
                {
                    return Instantiate(sniper1HolderPrefab, _centerOffsetPivot, false);
                }
                if (sniper1ModelPrefab != null)
                {
                    return Instantiate(sniper1ModelPrefab, _centerOffsetPivot, false);
                }
            }
            else
            {
                if (sniper2ModelPrefab != null)
                {
                    var holder = new GameObject("WeaponHolder 2");
                    holder.transform.SetParent(_centerOffsetPivot, false);
                    var model = Instantiate(sniper2ModelPrefab, holder.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    return holder;
                }
            }
            return null;
        }

        private void CenterWeaponByRendererBounds(GameObject weaponRoot)
        {
            _centerOffsetPivot.localPosition = Vector3.zero;
            _centerOffsetPivot.localRotation = Quaternion.identity;
            weaponOrbitPivot.rotation = Quaternion.identity;

            var meshRenderers = weaponRoot.GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers == null || meshRenderers.Length == 0) return;

            Bounds combined = meshRenderers[0].bounds;
            for (int i = 1; i < meshRenderers.Length; i++)
            {
                combined.Encapsulate(meshRenderers[i].bounds);
            }

            Vector3 worldOffset = combined.center - weaponOrbitPivot.position;
            _centerOffsetPivot.localPosition = -worldOffset;

            // Normalize visual scale so both Sniper 1 and Sniper 2 fill the cosmic stage heroically (~1.35m length)
            float maxDimension = Mathf.Max(combined.size.x, Mathf.Max(combined.size.y, combined.size.z));
            if (maxDimension > 0.01f)
            {
                float targetLength = 1.38f;
                float scaleFactor = Mathf.Clamp(targetLength / maxDimension, 0.65f, 1.85f);
                _centerOffsetPivot.localScale = Vector3.one * scaleFactor;

                // Recompute center after scale adjustment
                combined = meshRenderers[0].bounds;
                for (int i = 1; i < meshRenderers.Length; i++)
                {
                    combined.Encapsulate(meshRenderers[i].bounds);
                }
                worldOffset = combined.center - weaponOrbitPivot.position;
                _centerOffsetPivot.localPosition -= worldOffset;
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        #endregion

        #region Interactive 3D Rotation, Zoom & Camera Framing

        private void Update()
        {
            HandleRotationAndZoomInput();
            UpdateWeaponTransformAndCamera();
            CleanupNullVfxEntries();
        }

        private void HandleRotationAndZoomInput()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var touch = Touchscreen.current;

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 1. Mouse Drag Rotation (Left-click outside UI or Right-click anywhere)
            if (mouse != null)
            {
                if ((mouse.leftButton.wasPressedThisFrame && !pointerOverUi) || mouse.rightButton.wasPressedThisFrame)
                {
                    _isDraggingWeapon = true;
                }
                if (mouse.leftButton.wasReleasedThisFrame && mouse.rightButton.wasReleasedThisFrame)
                {
                    _isDraggingWeapon = false;
                }
                if (!mouse.leftButton.isPressed && !mouse.rightButton.isPressed)
                {
                    _isDraggingWeapon = false;
                }

                if (_isDraggingWeapon)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _targetYaw -= delta.x * dragSensitivity;
                    _targetPitch = Mathf.Clamp(_targetPitch + delta.y * dragSensitivity * 0.75f, minPitch, maxPitch);
                }

                // Scroll wheel zoom within safe non-clipping bounds
                if (!pointerOverUi)
                {
                    float scrollY = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scrollY) > 0.01f)
                    {
                        float zoomDelta = -Mathf.Sign(scrollY) * 0.11f;
                        _targetCameraDist = Mathf.Clamp(_targetCameraDist + zoomDelta, minCameraDistance, maxCameraDistance);
                    }
                }
            }

            // 2. Touch Drag Support
            if (touch != null && touch.primaryTouch.press.isPressed && !pointerOverUi)
            {
                Vector2 touchDelta = touch.primaryTouch.delta.ReadValue();
                _targetYaw -= touchDelta.x * dragSensitivity * 0.7f;
                _targetPitch = Mathf.Clamp(_targetPitch + touchDelta.y * dragSensitivity * 0.5f, minPitch, maxPitch);
            }

            // 3. Controller Right Stick Rotation
            if (gamepad != null)
            {
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.02f)
                {
                    _targetYaw -= stick.x * 110f * Time.unscaledDeltaTime;
                    _targetPitch = Mathf.Clamp(_targetPitch + stick.y * 75f * Time.unscaledDeltaTime, minPitch, maxPitch);
                }
            }

            // 4. Keyboard Rotation & Shortcuts
            if (keyboard != null)
            {
                float yawKey = 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) yawKey += 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) yawKey -= 1f;

                float pitchKey = 0f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) pitchKey += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) pitchKey -= 1f;

                if (Mathf.Abs(yawKey) > 0f || Mathf.Abs(pitchKey) > 0f)
                {
                    _targetYaw += yawKey * 85f * Time.unscaledDeltaTime;
                    _targetPitch = Mathf.Clamp(_targetPitch + pitchKey * 60f * Time.unscaledDeltaTime, minPitch, maxPitch);
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    OnResetViewClicked();
                }
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    OnBackClicked();
                }
            }
        }

        private void UpdateWeaponTransformAndCamera()
        {
            float dt = Time.unscaledDeltaTime;

            // Smooth orbital rotation interpolation
            _currentYaw = Mathf.Lerp(_currentYaw, _targetYaw, dt * rotationSmoothing);
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, dt * rotationSmoothing);

            if (weaponOrbitPivot != null)
            {
                weaponOrbitPivot.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            }

            // Subtle zero-G cosmic floating motion
            if (_floatPivot != null)
            {
                float floatTime = Time.unscaledTime * (_isSlowMotion ? 0.45f : 1.35f);
                float bobY = Mathf.Sin(floatTime) * 0.012f;
                float rollZ = Mathf.Cos(floatTime * 0.7f) * 0.6f;
                _floatPivot.localPosition = new Vector3(0f, bobY, 0f);
                _floatPivot.localRotation = Quaternion.Euler(0f, 0f, rollZ);
            }

            // Smooth camera distance & focus offset
            _currentCameraDist = Mathf.Lerp(_currentCameraDist, _targetCameraDist, dt * 10f);
            _currentCameraLookOffset = Vector3.Lerp(_currentCameraLookOffset, _targetCameraLookOffset, dt * 8f);

            if (showcaseCamera != null)
            {
                Vector3 focusPoint = _defaultStagePosition + _currentCameraLookOffset;
                Vector3 camPos = focusPoint + new Vector3(0f, 0.06f, -_currentCameraDist);
                showcaseCamera.transform.position = camPos;
                showcaseCamera.transform.LookAt(focusPoint);
            }
        }

        public void ResetViewImmediate()
        {
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _currentYaw = defaultYaw;
            _currentPitch = defaultPitch;
            _targetCameraDist = defaultCameraDistance;
            _currentCameraDist = defaultCameraDistance;
            _targetCameraLookOffset = Vector3.zero;
            _currentCameraLookOffset = Vector3.zero;

            if (weaponOrbitPivot != null)
            {
                weaponOrbitPivot.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            }
            UpdateWeaponTransformAndCamera();
        }

        /// <summary>
        /// Smoothly restores the weapon and camera to their default showcase angle and framing.
        /// </summary>
        public void OnResetViewClicked()
        {
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _targetCameraDist = defaultCameraDistance;
            _targetCameraLookOffset = Vector3.zero;
            ShowModeStatusBanner("VIEW RESET TO DEFAULT ANGLE");
        }

        #endregion

        #region Inspection-Safe Shoot, VFX Inspection & Slow Motion

        /// <summary>
        /// Triggers the inspection-safe firing sequence for the selected sniper:
        /// recoil & bolt animation, gunshot audio, muzzle flash VFX, tracer projectile, and target impact VFX.
        /// </summary>
        public void OnShootClicked()
        {
            // Ensure full weapon framing if camera was zoomed into impact stage
            _targetCameraLookOffset = Vector3.zero;

            TriggerInspectionShot(spawnMuzzleFlash: true, spawnProjectileAndImpact: true, playAudio: true, playRecoilAnim: true);
        }

        /// <summary>
        /// Core inspection-safe firing implementation. Reuses the project's actual muzzle flash,
        /// CosmeticBullet tracer, impact VFX prefabs, and sniper gunshot audio clips without any
        /// network, damage, or gameplay side effects.
        /// </summary>
        private void TriggerInspectionShot(bool spawnMuzzleFlash, bool spawnProjectileAndImpact, bool playAudio, bool playRecoilAnim)
        {
            float simSpeed = InspectionTimeScale;

            // 1. Firing & Bolt-Action Recoil Animation
            if (playRecoilAnim)
            {
                if (_firingAnimCoroutine != null)
                {
                    StopCoroutine(_firingAnimCoroutine);
                }
                _firingAnimCoroutine = StartCoroutine(PlayFiringAnimationRoutine());
            }

            // 2. Gunshot Audio
            if (playAudio && _weaponAudioSource != null)
            {
                AudioClip clip = (SelectedSniperIndex == 0) ? gunshotClip1 : (gunshotClip2 != null ? gunshotClip2 : gunshotClip1);
                if (clip != null)
                {
                    _weaponAudioSource.pitch = _isSlowMotion ? 0.46f : 1.0f;
                    _weaponAudioSource.PlayOneShot(clip, 0.92f);
                }
            }

            Vector3 muzzlePos = _activeFirePoint != null ? _activeFirePoint.position : (_defaultStagePosition + new Vector3(0.55f, 0.05f, 0f));
            Vector3 muzzleDir = GetMuzzleForwardDirection();
            Quaternion muzzleRot = Quaternion.LookRotation(muzzleDir);

            // 3. Muzzle Flash VFX + Dynamic Light Pulse
            if (spawnMuzzleFlash)
            {
                GameObject flashPrefab = (SelectedSniperIndex == 0) ? muzzleFlashPrefab1 : muzzleFlashPrefab2;
                if (flashPrefab != null)
                {
                    Transform flashParent = _activeFirePoint != null ? _activeFirePoint : weaponOrbitPivot;
                    GameObject flashGo = Instantiate(flashPrefab, muzzlePos, muzzleRot, flashParent);
                    RegisterAndConfigureVfx(flashGo, simSpeed);
                }

                if (_muzzleLightCoroutine != null) StopCoroutine(_muzzleLightCoroutine);
                _muzzleLightCoroutine = StartCoroutine(PulseMuzzleLightRoutine(muzzlePos));
            }

            // 4. Projectile / Tracer & Target Impact VFX
            if (spawnProjectileAndImpact)
            {
                Vector3 impactPos = muzzlePos + muzzleDir * 2.15f;

                // Align the holographic target stage at the impact point so the shot lands on the energy target
                if (_targetStageRoot != null)
                {
                    _targetStageRoot.position = impactPos;
                    _targetStageRoot.rotation = Quaternion.LookRotation(-muzzleDir);
                }

                GameObject bulletPrefab = (SelectedSniperIndex == 0) ? bulletPrefab1 : bulletPrefab2;
                GameObject impactPrefab = (SelectedSniperIndex == 0) ? hitEffectPrefab1 : hitEffectPrefab2;

                if (bulletPrefab != null)
                {
                    GameObject bulletGo = Instantiate(bulletPrefab, muzzlePos, muzzleRot);
                    var cosmeticBullet = bulletGo.GetComponent<CosmeticBullet>();
                    if (cosmeticBullet != null)
                    {
                        // Calibrated showcase speed so tracer flight is clearly visible across the cosmic stage
                        float showcaseSpeed = (SelectedSniperIndex == 0) ? 18f : 22f;
                        cosmeticBullet.Initialize(impactPos, -muzzleDir, impactPrefab, showcaseSpeed, isCharacterHit: false);
                        cosmeticBullet.SetSimulationSpeed(simSpeed);
                        cosmeticBullet.SetImpactCallback(impactInstance =>
                        {
                            RegisterAndConfigureVfx(impactInstance, InspectionTimeScale);
                        });
                    }
                    RegisterAndConfigureVfx(bulletGo, simSpeed);
                }
                else if (impactPrefab != null)
                {
                    GameObject impactGo = Instantiate(impactPrefab, impactPos, Quaternion.LookRotation(-muzzleDir));
                    RegisterAndConfigureVfx(impactGo, simSpeed);
                }
            }
        }

        private Vector3 GetMuzzleForwardDirection()
        {
            if (_activeWeaponRoot != null)
            {
                return _activeWeaponRoot.transform.forward;
            }
            if (_activeFirePoint != null)
            {
                return _activeFirePoint.forward;
            }
            return Vector3.right;
        }

        private IEnumerator PlayFiringAnimationRoutine()
        {
            if (_recoilPivot == null) yield break;

            bool isHeavy = (SelectedSniperIndex == 1);
            Vector3 recoilDirLocal = _activeFirePoint != null
                ? _recoilPivot.InverseTransformDirection(-GetMuzzleForwardDirection())
                : Vector3.back;

            float kickBackDist = isHeavy ? 0.145f : 0.095f;
            float pitchKick = isHeavy ? -11.5f : -7.5f;
            float rollKick = isHeavy ? 3.2f : 1.8f;

            // Phase 1: Sharp firing impulse
            float kickDuration = 0.055f;
            float elapsed = 0f;
            while (elapsed < kickDuration)
            {
                elapsed += Time.unscaledDeltaTime * InspectionTimeScale;
                float t = Mathf.Clamp01(elapsed / kickDuration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                _recoilPivot.localPosition = recoilDirLocal * (kickBackDist * easeOut);
                _recoilPivot.localRotation = Quaternion.Euler(pitchKick * easeOut, 0f, rollKick * easeOut);
                yield return null;
            }

            // Phase 2: Primary recoil recovery with slight barrel harmonic oscillation
            float recoverDuration = isHeavy ? 0.36f : 0.26f;
            elapsed = 0f;
            while (elapsed < recoverDuration)
            {
                elapsed += Time.unscaledDeltaTime * InspectionTimeScale;
                float t = Mathf.Clamp01(elapsed / recoverDuration);
                float damper = 1f - Mathf.SmoothStep(0f, 1f, t);
                float harmonic = Mathf.Cos(t * Mathf.PI * 2.2f) * damper;

                _recoilPivot.localPosition = recoilDirLocal * (kickBackDist * damper);
                _recoilPivot.localRotation = Quaternion.Euler(pitchKick * harmonic, 0f, rollKick * damper);
                yield return null;
            }

            // Phase 3: Bolt-action chambering pulse
            float boltDuration = 0.24f;
            elapsed = 0f;
            while (elapsed < boltDuration)
            {
                elapsed += Time.unscaledDeltaTime * InspectionTimeScale;
                float t = Mathf.Clamp01(elapsed / boltDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);

                _recoilPivot.localPosition = recoilDirLocal * (0.025f * pulse);
                _recoilPivot.localRotation = Quaternion.Euler(-2.0f * pulse, 0f, -2.8f * pulse);
                yield return null;
            }

            _recoilPivot.localPosition = Vector3.zero;
            _recoilPivot.localRotation = Quaternion.identity;
            _firingAnimCoroutine = null;
        }

        private IEnumerator PulseMuzzleLightRoutine(Vector3 worldPos)
        {
            if (_muzzleFlashLight == null) yield break;

            _muzzleFlashLight.transform.position = worldPos;
            _muzzleFlashLight.intensity = (SelectedSniperIndex == 1) ? 8.5f : 6.5f;
            _muzzleFlashLight.enabled = true;

            float duration = 0.14f;
            float elapsed = 0f;
            float startIntensity = _muzzleFlashLight.intensity;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime * InspectionTimeScale;
                float t = Mathf.Clamp01(elapsed / duration);
                _muzzleFlashLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                yield return null;
            }

            _muzzleFlashLight.intensity = 0f;
            _muzzleFlashLight.enabled = false;
            _muzzleLightCoroutine = null;
        }

        /// <summary>
        /// Toggles the dedicated "Inspect VFX" drawer so the player can trigger and inspect
        /// individual weapon effects (Muzzle Flash, Projectile/Tracer, Impact Sparks, or Full Cycle) on demand.
        /// </summary>
        public void OnInspectVfxToggleClicked()
        {
            _isVfxDrawerOpen = !_isVfxDrawerOpen;
            if (_vfxDrawerPanel != null)
            {
                _vfxDrawerPanel.SetActive(_isVfxDrawerOpen);
            }

            RefreshInspectVfxButtonVisuals();

            if (_isVfxDrawerOpen)
            {
                ShowModeStatusBanner("VFX INSPECTION LAB ACTIVE — SELECT AN EFFECT TO DEMONSTRATE");
            }
            else
            {
                _targetCameraLookOffset = Vector3.zero;
                _targetCameraDist = defaultCameraDistance;
            }
        }

        /// <summary>
        /// Demonstrates the selected sniper's Muzzle Flash & Barrel Discharge VFX up close on demand.
        /// </summary>
        public void OnInspectMuzzleFlashClicked()
        {
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _targetCameraDist = 1.18f;

            Vector3 muzzleOffset = (_activeFirePoint != null)
                ? (_activeFirePoint.position - _defaultStagePosition) * 0.55f
                : new Vector3(0.32f, 0.04f, 0f);
            _targetCameraLookOffset = muzzleOffset;

            TriggerInspectionShot(spawnMuzzleFlash: true, spawnProjectileAndImpact: false, playAudio: true, playRecoilAnim: true);
            ShowModeStatusBanner(_isSlowMotion
                ? "INSPECTING: MUZZLE FLASH & BARREL BLAST (SLOW MOTION 0.2x)"
                : "INSPECTING: MUZZLE FLASH & BARREL BLAST");
        }

        /// <summary>
        /// Demonstrates the selected sniper's Projectile, Contrail Gradient & Wake Sparks on demand.
        /// </summary>
        public void OnInspectProjectileTracerClicked()
        {
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _targetCameraDist = 1.68f;
            _targetCameraLookOffset = new Vector3(0.45f, 0.02f, 0.15f);

            GameObject bulletPrefab = (SelectedSniperIndex == 0) ? bulletPrefab1 : bulletPrefab2;
            if (bulletPrefab != null)
            {
                Vector3 startPos = _activeFirePoint != null ? _activeFirePoint.position : (_defaultStagePosition + new Vector3(0.45f, 0.05f, 0f));
                Vector3 dir = GetMuzzleForwardDirection();
                Vector3 endPos = startPos + dir * 2.35f;

                if (_targetStageRoot != null)
                {
                    _targetStageRoot.position = endPos;
                    _targetStageRoot.rotation = Quaternion.LookRotation(-dir);
                }

                GameObject bulletGo = Instantiate(bulletPrefab, startPos, Quaternion.LookRotation(dir));
                var cosmeticBullet = bulletGo.GetComponent<CosmeticBullet>();
                if (cosmeticBullet != null)
                {
                    // Slower showcase velocity so the player can closely inspect the contrail & wake particles
                    cosmeticBullet.Initialize(endPos, -dir, null, 7.5f, isCharacterHit: false);
                    cosmeticBullet.SetSimulationSpeed(InspectionTimeScale);
                }
                RegisterAndConfigureVfx(bulletGo, InspectionTimeScale);
            }

            ShowModeStatusBanner(_isSlowMotion
                ? "INSPECTING: PROJECTILE, CONTRAIL & WAKE SPARKS (SLOW MOTION 0.2x)"
                : "INSPECTING: PROJECTILE, CONTRAIL & WAKE SPARKS");
        }

        /// <summary>
        /// Demonstrates the selected sniper's Impact Shockwave, Sparks, Smoke & Energy Burst up close on demand.
        /// </summary>
        public void OnInspectImpactEffectClicked()
        {
            Vector3 impactStagePos = _defaultStagePosition + new Vector3(0.55f, 0.04f, -0.15f);
            if (_targetStageRoot != null)
            {
                _targetStageRoot.position = impactStagePos;
                _targetStageRoot.rotation = Quaternion.LookRotation(showcaseCamera.transform.position - impactStagePos);
            }

            _targetCameraLookOffset = new Vector3(0.42f, 0.03f, -0.10f);
            _targetCameraDist = 1.25f;

            GameObject impactPrefab = (SelectedSniperIndex == 0) ? hitEffectPrefab1 : hitEffectPrefab2;
            if (impactPrefab != null)
            {
                Vector3 normal = (showcaseCamera.transform.position - impactStagePos).normalized;
                GameObject impactGo = Instantiate(impactPrefab, impactStagePos, Quaternion.LookRotation(normal));
                impactGo.transform.localScale = Vector3.one * 1.25f;
                RegisterAndConfigureVfx(impactGo, InspectionTimeScale);
            }

            ShowModeStatusBanner(_isSlowMotion
                ? "INSPECTING: BALLISTIC IMPACT, SPARKS & ENERGY BURST (SLOW MOTION 0.2x)"
                : "INSPECTING: BALLISTIC IMPACT, SPARKS & ENERGY BURST");
        }

        /// <summary>
        /// Toggles Slow Motion mode on/off and immediately updates any active VFX and animations.
        /// Does not modify global Time.timeScale so UI and camera controls remain 100% responsive.
        /// </summary>
        public void OnSlowMotionToggleClicked()
        {
            _isSlowMotion = !_isSlowMotion;
            float simSpeed = InspectionTimeScale;

            // Update all currently active spawned VFX instances immediately
            for (int i = _spawnedVfxObjects.Count - 1; i >= 0; i--)
            {
                GameObject vfx = _spawnedVfxObjects[i];
                if (vfx == null)
                {
                    _spawnedVfxObjects.RemoveAt(i);
                    continue;
                }
                ApplySimulationSpeedToObject(vfx, simSpeed);
            }

            RefreshSlowMotionButtonVisuals();
            ShowModeStatusBanner(_isSlowMotion
                ? "SLOW MOTION ENABLED (0.20x) — PRESS SHOOT OR INSPECT VFX"
                : "NORMAL SPEED RESTORED (1.0x)");
        }

        private void RegisterAndConfigureVfx(GameObject vfxInstance, float simSpeed)
        {
            if (vfxInstance == null) return;
            _spawnedVfxObjects.Add(vfxInstance);
            ApplySimulationSpeedToObject(vfxInstance, simSpeed);
        }

        private static void ApplySimulationSpeedToObject(GameObject vfxInstance, float simSpeed)
        {
            if (vfxInstance == null) return;

            var cleanup = vfxInstance.GetComponent<VFXAutoCleanup>();
            if (cleanup != null)
            {
                cleanup.SetSimulationSpeed(simSpeed);
            }

            var bullet = vfxInstance.GetComponent<CosmeticBullet>();
            if (bullet != null)
            {
                bullet.SetSimulationSpeed(simSpeed);
            }

            var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null)
                {
                    var main = particleSystems[i].main;
                    main.simulationSpeed = simSpeed;
                }
            }
        }

        private void CleanupNullVfxEntries()
        {
            for (int i = _spawnedVfxObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedVfxObjects[i] == null)
                {
                    _spawnedVfxObjects.RemoveAt(i);
                }
            }
        }

        private void CleanupSpawnedVfx()
        {
            for (int i = 0; i < _spawnedVfxObjects.Count; i++)
            {
                if (_spawnedVfxObjects[i] != null)
                {
                    Destroy(_spawnedVfxObjects[i]);
                }
            }
            _spawnedVfxObjects.Clear();
        }

        /// <summary>
        /// Returns the player cleanly back to 01_MainMenu directly into LoadoutPanel,
        /// preserving their equipped sniper selection.
        /// </summary>
        public void OnBackClicked()
        {
            CleanupSpawnedVfx();
            Time.timeScale = 1f;
            RelayNetworkUI.ReturnToLoadoutFromInspection = true;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        #endregion

        #region Cosmic Inspection UI Builder

        private void BuildInspectionUI()
        {
            Transform existingCanvas = transform.Find("InspectionHUDCanvas");
            if (existingCanvas != null)
            {
                Destroy(existingCanvas.gameObject);
            }

            var canvasGo = new GameObject("InspectionHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5; // UI
            canvasGo.transform.SetParent(transform, false);

            _hudCanvas = canvasGo.GetComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRt = canvasGo.GetComponent<RectTransform>();
            Color theme = GetThemeColor(SelectedSniperIndex);
            bool isSniper1 = (SelectedSniperIndex == 0);

            // 1. Top-Left Weapon Identity & Telemetry Header
            BuildWeaponHeaderUI(rootRt, isSniper1, theme);

            // 2. Top-Center Rotation & Zoom Hint Pill
            BuildRotationHintUI(rootRt, theme);

            // 3. Top-Right Back Button (returns to LoadoutPanel)
            BuildBackButtonUI(rootRt, theme);

            // 4. Status Toast Banner (above bottom controls)
            BuildStatusBannerUI(rootRt, theme);

            // 5. Inspect VFX Sub-Drawer (above bottom control deck, initially hidden)
            BuildVfxInspectionDrawerUI(rootRt, theme);

            // 6. Bottom Primary Control Deck (Shoot, Inspect VFX, Slow Motion, Reset View)
            BuildBottomControlDeckUI(rootRt, theme);
        }

        private void BuildWeaponHeaderUI(RectTransform rootRt, bool isSniper1, Color theme)
        {
            var headerPanel = CreateUiElement("WeaponHeaderPanel", rootRt, new Vector2(0f, 1f), new Vector2(48f, -38f), new Vector2(560f, 195f), new Vector2(0f, 1f));

            var bg = headerPanel.gameObject.AddComponent<Image>();
            bg.sprite = LoadoutUISprites.CardFill;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.04f, 0.09f, 0.80f);
            bg.raycastTarget = false;

            var frame = CreateUiImage("HeaderFrame", headerPanel, LoadoutUISprites.CardFrame, new Color(theme.r, theme.g, theme.b, 0.72f), true);
            StretchRect(frame.rectTransform);

            var corner = CreateUiImage("CornerAccent", headerPanel, LoadoutUISprites.CornerTriangle, theme, false);
            SetRect(corner.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(18f, 18f), new Vector2(0f, 1f));

            // Tag slashes + Classification Tag
            var slashes = CreateUiImage("TagSlashes", headerPanel, LoadoutUISprites.TagSlashes, theme, false);
            SetRect(slashes.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -26f), new Vector2(34f, 13f), new Vector2(0f, 0.5f));

            string classTag = isSniper1 ? "MEDIUM SNIPER BOLT  //  .338 LAPUA" : "HEAVY SNIPER BOLT  //  .50 BMG";
            var tagTmp = CreateUiText("ClassTagText", headerPanel, classTag, _bodyFont, 16f, 2.2f, theme, TextAlignmentOptions.MidlineLeft);
            SetRect(tagTmp.rectTransform, new Vector2(0f, 1f), new Vector2(68f, -26f), new Vector2(460f, 24f), new Vector2(0f, 0.5f));

            // Weapon Title (clearly identifying Sniper 1 or Sniper 2)
            string weaponName = isSniper1 ? "SNIPER 01" : "SNIPER 02";
            var titleTmp = CreateUiText("WeaponNameText", headerPanel, weaponName, _titleFont, 54f, 2.5f, Color.white, TextAlignmentOptions.MidlineLeft);
            titleTmp.fontStyle = FontStyles.Italic;
            SetRect(titleTmp.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -72f), new Vector2(340f, 58f), new Vector2(0f, 0.5f));

            string subName = isSniper1 ? "VORTEX TACTICAL PRECISION RIFLE" : "TITAN ANTI-MATERIEL CANNON";
            var subTmp = CreateUiText("WeaponSubtitleText", headerPanel, subName, _bodyFont, 14f, 3.0f, MutedText, TextAlignmentOptions.MidlineLeft);
            SetRect(subTmp.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -112f), new Vector2(480f, 20f), new Vector2(0f, 0.5f));

            // Divider line
            var div = CreateUiImage("HeaderDivider", headerPanel, LoadoutUISprites.White, new Color(theme.r, theme.g, theme.b, 0.30f), false);
            SetRect(div.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -130f), new Vector2(512f, 1.5f), new Vector2(0f, 0.5f));

            // Compact Stat Badges Row
            string[] labels = { "DAMAGE", "FIRE RATE", "CAPACITY", "RECOIL" };
            string[] values = isSniper1
                ? new[] { "70 MED", "85 BOLT", "5 RNDS", "LIGHT" }
                : new[] { "100 MAX", "36 BOLT", "3 RNDS", "HEAVY" };

            for (int i = 0; i < labels.Length; i++)
            {
                float xOffset = 24f + i * 130f;
                var statKey = CreateUiText("StatKey_" + i, headerPanel, labels[i], _bodyFont, 11f, 1.5f, MutedText, TextAlignmentOptions.MidlineLeft);
                SetRect(statKey.rectTransform, new Vector2(0f, 1f), new Vector2(xOffset, -150f), new Vector2(120f, 16f), new Vector2(0f, 0.5f));

                var statVal = CreateUiText("StatVal_" + i, headerPanel, values[i], _bodyFont, 16f, 1.2f, theme, TextAlignmentOptions.MidlineLeft);
                SetRect(statVal.rectTransform, new Vector2(0f, 1f), new Vector2(xOffset, -171f), new Vector2(120f, 20f), new Vector2(0f, 0.5f));
            }
        }

        private void BuildRotationHintUI(RectTransform rootRt, Color theme)
        {
            var hintPill = CreateUiElement("RotationHintPill", rootRt, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(470f, 40f), new Vector2(0.5f, 1f));

            var bg = hintPill.gameObject.AddComponent<Image>();
            bg.sprite = LoadoutUISprites.ButtonFill;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.03f, 0.06f, 0.12f, 0.72f);
            bg.raycastTarget = false;

            var frame = CreateUiImage("HintFrame", hintPill, LoadoutUISprites.ButtonFrame, new Color(theme.r, theme.g, theme.b, 0.42f), true);
            StretchRect(frame.rectTransform);

            var icon = CreateUiImage("HintIcon", hintPill, LoadoutUISprites.IconCrosshair, theme, false);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-196f, 0f), new Vector2(18f, 18f));

            var hintText = CreateUiText("HintText", hintPill, "DRAG TO ROTATE 360°   •   SCROLL TO ZOOM", _bodyFont, 15f, 2.5f, MutedText, TextAlignmentOptions.Center);
            SetRect(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(420f, 30f));
        }

        private void BuildBackButtonUI(RectTransform rootRt, Color theme)
        {
            CreateCosmicButton(rootRt, "BackToLoadoutButton", "BACK TO LOADOUT",
                anchor: new Vector2(1f, 1f),
                pos: new Vector2(-48f, -40f),
                size: new Vector2(250f, 54f),
                pivot: new Vector2(1f, 1f),
                accentColor: new Color(0.75f, 0.85f, 0.96f, 1f),
                isPrimaryFill: false,
                iconSprite: null,
                onClick: OnBackClicked);
        }

        private void BuildStatusBannerUI(RectTransform rootRt, Color theme)
        {
            var bannerRt = CreateUiElement("ModeStatusBanner", rootRt, new Vector2(0.5f, 0f), new Vector2(0f, 198f), new Vector2(720f, 34f), new Vector2(0.5f, 0.5f));
            _activeModeBannerGroup = bannerRt.gameObject.AddComponent<CanvasGroup>();
            _activeModeBannerGroup.alpha = 0f;
            _activeModeBannerGroup.interactable = false;
            _activeModeBannerGroup.blocksRaycasts = false;

            var bg = bannerRt.gameObject.AddComponent<Image>();
            bg.sprite = LoadoutUISprites.ButtonFill;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.05f, 0.11f, 0.85f);
            bg.raycastTarget = false;

            var frame = CreateUiImage("BannerFrame", bannerRt, LoadoutUISprites.ButtonFrame, new Color(theme.r, theme.g, theme.b, 0.55f), true);
            StretchRect(frame.rectTransform);

            _activeModeBannerText = CreateUiText("BannerText", bannerRt, "", _bodyFont, 14f, 2.5f, theme, TextAlignmentOptions.Center);
            StretchRect(_activeModeBannerText.rectTransform);
        }

        private void BuildVfxInspectionDrawerUI(RectTransform rootRt, Color theme)
        {
            var drawerRt = CreateUiElement("VfxInspectionDrawer", rootRt, new Vector2(0.5f, 0f), new Vector2(0f, 138f), new Vector2(980f, 74f), new Vector2(0.5f, 0.5f));
            _vfxDrawerPanel = drawerRt.gameObject;

            var bg = drawerRt.gameObject.AddComponent<Image>();
            bg.sprite = LoadoutUISprites.CardFill;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.04f, 0.09f, 0.92f);

            var frame = CreateUiImage("DrawerFrame", drawerRt, LoadoutUISprites.CardFrame, new Color(CosmicViolet.r, CosmicViolet.g, CosmicViolet.b, 0.85f), true);
            StretchRect(frame.rectTransform);

            var title = CreateUiText("DrawerLabel", drawerRt, "VFX LAB //", _bodyFont, 15f, 2.2f, CosmicViolet, TextAlignmentOptions.MidlineLeft);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-462f, 0f), new Vector2(110f, 36f), new Vector2(0f, 0.5f));

            // 4 individual on-demand VFX trigger buttons
            CreateCosmicButton(drawerRt, "VfxMuzzleBtn", "1. MUZZLE FLASH",
                new Vector2(0.5f, 0.5f), new Vector2(-245f, 0f), new Vector2(195f, 48f), new Vector2(0.5f, 0.5f),
                theme, false, LoadoutUISprites.IconDamage, OnInspectMuzzleFlashClicked);

            CreateCosmicButton(drawerRt, "VfxTracerBtn", "2. TRACER & TRAIL",
                new Vector2(0.5f, 0.5f), new Vector2(-35f, 0f), new Vector2(195f, 48f), new Vector2(0.5f, 0.5f),
                theme, false, LoadoutUISprites.IconFireRate, OnInspectProjectileTracerClicked);

            CreateCosmicButton(drawerRt, "VfxImpactBtn", "3. IMPACT SPARKS",
                new Vector2(0.5f, 0.5f), new Vector2(175f, 0f), new Vector2(195f, 48f), new Vector2(0.5f, 0.5f),
                theme, false, LoadoutUISprites.IconCrosshair, OnInspectImpactEffectClicked);

            CreateCosmicButton(drawerRt, "VfxFullShotBtn", "4. FULL SEQUENCE",
                new Vector2(0.5f, 0.5f), new Vector2(380f, 0f), new Vector2(190f, 48f), new Vector2(0.5f, 0.5f),
                CosmicViolet, false, null, () =>
                {
                    _targetCameraLookOffset = Vector3.zero;
                    _targetCameraDist = defaultCameraDistance;
                    OnShootClicked();
                    ShowModeStatusBanner(_isSlowMotion
                        ? "INSPECTING: FULL FIRING SEQUENCE (SLOW MOTION 0.2x)"
                        : "INSPECTING: FULL FIRING SEQUENCE");
                });

            _vfxDrawerPanel.SetActive(false);
        }

        private void BuildBottomControlDeckUI(RectTransform rootRt, Color theme)
        {
            var deckRt = CreateUiElement("BottomControlDeck", rootRt, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(1060f, 76f), new Vector2(0.5f, 0.5f));

            var deckBg = deckRt.gameObject.AddComponent<Image>();
            deckBg.sprite = LoadoutUISprites.CardFill;
            deckBg.type = Image.Type.Sliced;
            deckBg.color = DarkPanelBg;
            deckBg.raycastTarget = false;

            var deckFrame = CreateUiImage("DeckFrame", deckRt, LoadoutUISprites.CardFrame, new Color(theme.r, theme.g, theme.b, 0.65f), true);
            StretchRect(deckFrame.rectTransform);

            // 1. SHOOT Button (Primary highlighted action)
            CreateCosmicButton(deckRt, "ShootButton", "SHOOT",
                new Vector2(0.5f, 0.5f), new Vector2(-375f, 0f), new Vector2(235f, 54f), new Vector2(0.5f, 0.5f),
                theme, isPrimaryFill: true, iconSprite: LoadoutUISprites.IconCrosshair, onClick: OnShootClicked);

            // 2. INSPECT VFX Button
            var vfxBtnRefs = CreateCosmicButton(deckRt, "InspectVfxButton", "INSPECT VFX",
                new Vector2(0.5f, 0.5f), new Vector2(-120f, 0f), new Vector2(245f, 54f), new Vector2(0.5f, 0.5f),
                CosmicViolet, isPrimaryFill: false, iconSprite: LoadoutUISprites.IconDamage, onClick: OnInspectVfxToggleClicked);
            _inspectVfxButtonBg = vfxBtnRefs.Bg;
            _inspectVfxButtonFrame = vfxBtnRefs.Frame;
            _inspectVfxButtonLabel = vfxBtnRefs.Label;

            // 3. SLOW MOTION Toggle Button
            var slowMoRefs = CreateCosmicButton(deckRt, "SlowMotionToggle", "SLOW MOTION: OFF",
                new Vector2(0.5f, 0.5f), new Vector2(145f, 0f), new Vector2(255f, 54f), new Vector2(0.5f, 0.5f),
                CyanTheme, isPrimaryFill: false, iconSprite: null, onClick: OnSlowMotionToggleClicked);
            _slowMoButtonBg = slowMoRefs.Bg;
            _slowMoButtonFrame = slowMoRefs.Frame;
            _slowMoButtonLabel = slowMoRefs.Label;

            // Status indicator dot on Slow Motion button
            _slowMoStatusDot = CreateUiImage("SlowMoDot", slowMoRefs.Root, LoadoutUISprites.IconSquare, new Color(0.45f, 0.55f, 0.65f, 0.8f), false);
            SetRect(_slowMoStatusDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(14f, 14f));
            _slowMoButtonLabel.rectTransform.anchoredPosition = new Vector2(10f, 0f);

            // 4. RESET VIEW Button
            CreateCosmicButton(deckRt, "ResetViewButton", "RESET VIEW",
                new Vector2(0.5f, 0.5f), new Vector2(395f, 0f), new Vector2(210f, 54f), new Vector2(0.5f, 0.5f),
                new Color(0.75f, 0.84f, 0.94f, 1f), isPrimaryFill: false, iconSprite: null, onClick: OnResetViewClicked);
        }

        private void RefreshSlowMotionButtonVisuals()
        {
            if (_slowMoButtonLabel == null) return;

            if (_isSlowMotion)
            {
                _slowMoButtonLabel.text = "SLOW MOTION: ON";
                _slowMoButtonLabel.color = new Color(0.02f, 0.08f, 0.12f, 1f);
                if (_slowMoButtonBg != null) _slowMoButtonBg.color = CyanTheme;
                if (_slowMoButtonFrame != null) _slowMoButtonFrame.color = Color.white;
                if (_slowMoStatusDot != null) _slowMoStatusDot.color = new Color(0.02f, 0.08f, 0.12f, 1f);
            }
            else
            {
                _slowMoButtonLabel.text = "SLOW MOTION: OFF";
                _slowMoButtonLabel.color = Color.white;
                if (_slowMoButtonBg != null) _slowMoButtonBg.color = new Color(0.08f, 0.14f, 0.22f, 0.90f);
                if (_slowMoButtonFrame != null) _slowMoButtonFrame.color = new Color(CyanTheme.r, CyanTheme.g, CyanTheme.b, 0.75f);
                if (_slowMoStatusDot != null) _slowMoStatusDot.color = new Color(0.45f, 0.55f, 0.65f, 0.85f);
            }
        }

        private void RefreshInspectVfxButtonVisuals()
        {
            if (_inspectVfxButtonLabel == null) return;

            if (_isVfxDrawerOpen)
            {
                _inspectVfxButtonLabel.text = "CLOSE VFX LAB";
                _inspectVfxButtonLabel.color = Color.white;
                if (_inspectVfxButtonBg != null) _inspectVfxButtonBg.color = new Color(CosmicViolet.r * 0.75f, CosmicViolet.g * 0.75f, CosmicViolet.b * 0.85f, 0.95f);
                if (_inspectVfxButtonFrame != null) _inspectVfxButtonFrame.color = Color.white;
            }
            else
            {
                _inspectVfxButtonLabel.text = "INSPECT VFX";
                _inspectVfxButtonLabel.color = Color.white;
                if (_inspectVfxButtonBg != null) _inspectVfxButtonBg.color = new Color(0.08f, 0.14f, 0.22f, 0.90f);
                if (_inspectVfxButtonFrame != null) _inspectVfxButtonFrame.color = new Color(CosmicViolet.r, CosmicViolet.g, CosmicViolet.b, 0.80f);
            }
        }

        private Coroutine _bannerCoroutine;
        private void ShowModeStatusBanner(string message)
        {
            if (_activeModeBannerText == null || _activeModeBannerGroup == null) return;
            _activeModeBannerText.text = message;

            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(AnimateStatusBannerRoutine());
        }

        private IEnumerator AnimateStatusBannerRoutine()
        {
            _activeModeBannerGroup.alpha = 1f;
            float hold = 2.4f;
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float fade = 0.45f;
            elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _activeModeBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
                yield return null;
            }
            _activeModeBannerGroup.alpha = 0f;
            _bannerCoroutine = null;
        }

        private struct CosmicButtonRefs
        {
            public RectTransform Root;
            public Image Bg;
            public Image Frame;
            public TextMeshProUGUI Label;
        }

        private CosmicButtonRefs CreateCosmicButton(RectTransform parent, string name, string text,
            Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot,
            Color accentColor, bool isPrimaryFill, Sprite iconSprite, UnityEngine.Events.UnityAction onClick)
        {
            var rt = CreateUiElement(name, parent, anchor, pos, size, pivot);

            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = LoadoutUISprites.ButtonFill;
            bg.type = Image.Type.Sliced;
            bg.color = isPrimaryFill ? accentColor : new Color(0.08f, 0.14f, 0.22f, 0.90f);
            bg.raycastTarget = true;

            var frame = CreateUiImage("Frame", rt, LoadoutUISprites.ButtonFrame,
                isPrimaryFill ? Color.Lerp(accentColor, Color.white, 0.35f) : new Color(accentColor.r, accentColor.g, accentColor.b, 0.78f), true);
            StretchRect(frame.rectTransform);

            Color labelColor = isPrimaryFill ? new Color(0.03f, 0.05f, 0.09f, 1f) : Color.white;
            float labelOffsetX = 0f;

            if (iconSprite != null)
            {
                var icon = CreateUiImage("Icon", rt, iconSprite, labelColor, false);
                SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-size.x * 0.28f, 0f), new Vector2(20f, 20f));
                labelOffsetX = 12f;
            }

            var label = CreateUiText("Label", rt, text, _bodyFont, 18f, 2.2f, labelColor, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(labelOffsetX, 0f), new Vector2(size.x - 24f, size.y));

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.90f, 0.95f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            return new CosmicButtonRefs
            {
                Root = rt,
                Bg = bg,
                Frame = frame,
                Label = label
            };
        }

        #endregion

        #region UI & Transform Helpers

        private static Color GetThemeColor(int sniperIndex)
        {
            return (sniperIndex == 1) ? AmberTheme : CyanTheme;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static RectTransform CreateUiElement(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            SetRect(rt, anchor, pos, size, pivot);
            return rt;
        }

        private static Image CreateUiImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateUiText(string name, Transform parent, string text, TMP_FontAsset font,
            float fontSize, float charSpacing, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.characterSpacing = charSpacing;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        #endregion
    }
}
