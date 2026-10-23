using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Coffee.UIExtensions;
using SniperGame.Player;
using SniperGame.Gameplay;
using PewPewArena.UI;

namespace SniperGame.UI
{
    public class CombatHUD : MonoBehaviour
    {
        public static CombatHUD Instance { get; private set; }

        [Header("HUD Panels & Reticles")]
        [SerializeField] private GameObject hudContainer;
        [SerializeField] private GameObject hipCrosshair;
        [SerializeField] private GameObject scopeOverlay;
        [Tooltip("Attach the SniperScopeOverlay component from ScopeOverlay here")]
        [SerializeField] private SniperScopeOverlay sniperScopeOverlay;
        [SerializeField] private GameObject hitmarker;
        [SerializeField] private TextMeshProUGUI hitmarkerText;

        [Header("Wall Run Button Prompt")]
        [Tooltip("Drag the WallRunPrompt GameObject from CombatHUDCanvas here")]
        [SerializeField] private HoldKeyPrompt wallRunPrompt;

        [Header("Shoot / Reload Cooldown Bar")]
        [Tooltip("Parent container of the cooldown bar (auto-hides when ready to shoot)")]
        [SerializeField] private GameObject cooldownBarRoot;
        [Tooltip("Image with Image Type set to 'Filled'")]
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private Color shootCooldownColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color reloadCooldownColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        [Header("Damage Vignette Feedback")]
        [SerializeField] private CanvasGroup damageVignetteGroup;
        [SerializeField] private float vignetteFadeSpeed = 4.0f;

        [Header("Health UI Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Health Borders")]
        [Tooltip("Regular border image displayed when health is normal (> 30 HP)")]
        [SerializeField] private GameObject normalHealthBorder;

        [Tooltip("Warning border image displayed when health is low (<= 30 HP)")]
        [SerializeField] private GameObject lowHealthBorder;

        [Header("Health UI Particle Groups")]
        [Tooltip("UIParticle component on FX_NormalHealth")]
        [SerializeField] private UIParticle normalHealthUIParticle;

        [Tooltip("UIParticle component on FX_LowHealth")]
        [SerializeField] private UIParticle lowHealthUIParticle;

        [Tooltip("UIParticle component on FX_DeathExplosion")]
        [SerializeField] private UIParticle deathExplosionUIParticle;

        [Header("Stamina UI Elements")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFillImage;

        [Header("Ammo UI Elements")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI ammoGlowText;
        [SerializeField] private TextMeshProUGUI ammoMagLabel;
        [SerializeField] private TextMeshProUGUI ammoSubLabel;
        [SerializeField] private Image[] ammoBulletIcons;
        [SerializeField] private GameObject ammoRoot;

        [Header("Match & Round UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI youScoreText;
        [SerializeField] private TextMeshProUGUI youScoreGlowText;
        [SerializeField] private TextMeshProUGUI enemyScoreText;
        [SerializeField] private TextMeshProUGUI enemyScoreGlowText;
        [SerializeField] private TextMeshProUGUI roundBannerText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI timerGlowText;
        [SerializeField] private TextMeshProUGUI announcementText;
        [SerializeField] private TextMeshProUGUI announcementGlowText;
        [SerializeField] private TextMeshProUGUI announcementSubText;
        [SerializeField] private CanvasGroup announcementCanvasGroup;
        [SerializeField] private GameObject announcementRoot;
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TextMeshProUGUI matchEndTitle;
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI rematchStatusText;
        [SerializeField] private Button returnToMenuButton;

        [Header("Rematch Consent Prompt")]
        [SerializeField] private GameObject rematchPromptPanel;
        [SerializeField] private Button acceptRematchButton;
        [SerializeField] private Button declineRematchButton;

        [Header("Match End UI Customization")]
        [SerializeField] private Sprite hoverArrowSprite;
        [SerializeField] private Sprite hoverArrowRightSprite;

        [Header("Health Gradients")]
        [SerializeField] private Gradient aliveGradient;
        [SerializeField] private Gradient lowHealthGradient;
        [SerializeField] private Color deadBackgroundColor = new Color(0.55f, 0.05f, 0.05f, 0.9f);
        [SerializeField] private Color aliveBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        [Header("Settings")]
        [SerializeField] private float hitmarkerDuration = 0.12f;

        public bool IsMatchEndActive => (matchEndPanel != null && matchEndPanel.activeSelf) || 
                                        (rematchPromptPanel != null && rematchPromptPanel.activeSelf);

        private Coroutine _hitmarkerCoroutine;
        private Coroutine _announcementCoroutine;
        private Coroutine _damageFlashCoroutine;
        private Coroutine _matchEndFadeRoutine;
        private int _currentParticleState = -1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SetupDefaultGradients();

            // Ensure container and persistent HUD elements are active regardless of Inspector state
            if (hudContainer != null && !hudContainer.activeSelf) hudContainer.SetActive(true);
            if (healthSlider != null && !healthSlider.gameObject.activeSelf) healthSlider.gameObject.SetActive(true);
            if (staminaSlider != null && !staminaSlider.gameObject.activeSelf) staminaSlider.gameObject.SetActive(true);
            EnsureHUDHealthAndStaminaLayout();
            ResolveAmmoElements();
            if (ammoRoot != null && !ammoRoot.activeSelf) ammoRoot.SetActive(true);
            else if (ammoText != null && !ammoText.gameObject.activeSelf) ammoText.gameObject.SetActive(true);
            if (scoreText != null && !scoreText.gameObject.activeSelf) scoreText.gameObject.SetActive(true);
            ResolveScoreboardElements();
            if (timerText != null && !timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            if (damageVignetteGroup != null && !damageVignetteGroup.gameObject.activeSelf) damageVignetteGroup.gameObject.SetActive(true);

            // Auto-activate PauseMenuRoot if it was saved inactive in the Inspector
            var pauseMenu = GetComponentInChildren<PauseMenu>(true);
            if (pauseMenu != null && !pauseMenu.gameObject.activeSelf)
            {
                pauseMenu.gameObject.SetActive(true);
            }

            if (hitmarker != null) hitmarker.SetActive(false);
            if (hipCrosshair != null) hipCrosshair.SetActive(Settings.SettingsManager.ShowCrosshair);
            Settings.SettingsManager.OnCrosshairToggled += (show) =>
            {
                if (hipCrosshair != null && (scopeOverlay == null || !scopeOverlay.activeSelf))
                {
                    hipCrosshair.SetActive(show);
                }
            };
            ResolveAnnouncementElements();
            if (announcementRoot != null) announcementRoot.SetActive(false);
            else if (announcementText != null) announcementText.gameObject.SetActive(false);
            if (matchEndPanel != null) matchEndPanel.SetActive(false);
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);
            if (rematchStatusText != null) rematchStatusText.text = "";
            if (damageVignetteGroup != null) damageVignetteGroup.alpha = 0f;

            if (normalHealthBorder != null) normalHealthBorder.SetActive(true);
            if (lowHealthBorder != null) lowHealthBorder.SetActive(false);

            // Auto-resolve sniperScopeOverlay if left unassigned
            if (sniperScopeOverlay == null && scopeOverlay != null)
            {
                sniperScopeOverlay = scopeOverlay.GetComponent<SniperScopeOverlay>();
            }

            if (sniperScopeOverlay != null)
            {
                sniperScopeOverlay.SetScoped(false);
            }
            else if (scopeOverlay != null)
            {
                Animator anim = scopeOverlay.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("IsScoped", false);
                }
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            if (acceptRematchButton != null)
            {
                acceptRematchButton.onClick.AddListener(OnAcceptRematchClicked);
            }

            if (declineRematchButton != null)
            {
                declineRematchButton.onClick.AddListener(OnDeclineRematchClicked);
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            }

            ApplyMatchEndTheme();
            InitializeUIParticles();
        }

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Start()
        {
            EnsureHUDHealthAndStaminaLayout();
            StartCoroutine(InitializeLocalPlayerRoutine());
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                EnsureHUDHealthAndStaminaLayout();
            }

            // Keeps cursor free and visible whenever match end or rematch screen is open
            if (IsMatchEndActive)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            EnsureHUDHealthAndStaminaLayout();
        }

        public void EnsureHUDHealthAndStaminaLayout()
        {
            if (hudContainer == null) return;
            RectTransform containerRt = hudContainer.GetComponent<RectTransform>();
            if (containerRt == null) return;

            // In reference 1920x1080 canvas:
            // Center is (0, 0), bottom-left is (-960, -540).
            // HealthSliderMain was positioned at (-803.37335, -462.8268) -> exactly 156.6267 units right and 77.1732 units up from bottom-left.
            // StaminaSlider was anchored at bottom-left (0, 0) with anchoredPosition (90, 40).
            float blX = -containerRt.rect.width * 0.5f;
            float blY = -containerRt.rect.height * 0.5f;

            if (healthSlider != null && healthSlider.transform.parent != null)
            {
                Transform healthMain = healthSlider.transform.parent;
                healthMain.localPosition = new Vector3(blX + 156.6267f, blY + 77.1732f, healthMain.localPosition.z);
            }

            if (staminaSlider != null)
            {
                RectTransform staminaRt = staminaSlider.GetComponent<RectTransform>();
                if (staminaRt != null)
                {
                    staminaRt.anchorMin = Vector2.zero;
                    staminaRt.anchorMax = Vector2.zero;
                    staminaRt.pivot = Vector2.zero;
                    staminaRt.anchoredPosition = new Vector2(90f, 40f);
                    staminaRt.sizeDelta = new Vector2(200f, 20f);

                    Transform bg = staminaRt.Find("Background");
                    if (bg != null)
                    {
                        RectTransform bgRt = bg.GetComponent<RectTransform>();
                        if (bgRt != null)
                        {
                            bgRt.anchorMin = Vector2.zero;
                            bgRt.anchorMax = Vector2.one;
                            bgRt.sizeDelta = Vector2.zero;
                            bgRt.anchoredPosition = Vector2.zero;
                        }
                    }

                    Transform fillArea = staminaRt.Find("Fill Area");
                    if (fillArea != null)
                    {
                        RectTransform faRt = fillArea.GetComponent<RectTransform>();
                        if (faRt != null)
                        {
                            faRt.anchorMin = Vector2.zero;
                            faRt.anchorMax = Vector2.one;
                            faRt.sizeDelta = Vector2.zero;
                            faRt.anchoredPosition = Vector2.zero;
                        }
                    }
                }
            }
        }

        private void InitializeUIParticles()
        {
            if (normalHealthUIParticle != null)
            {
                normalHealthUIParticle.gameObject.SetActive(true);
                normalHealthUIParticle.RefreshParticles();
            }
            if (lowHealthUIParticle != null)
            {
                lowHealthUIParticle.gameObject.SetActive(true);
                lowHealthUIParticle.RefreshParticles();
            }
            if (deathExplosionUIParticle != null)
            {
                deathExplosionUIParticle.gameObject.SetActive(true);
                deathExplosionUIParticle.RefreshParticles();
            }

            SetParticleState(1);
        }

        private IEnumerator InitializeLocalPlayerRoutine()
        {
            while (true)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
                {
                    var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                    if (playerObj != null)
                    {
                        var health = playerObj.GetComponent<PlayerHealth>();
                        if (health != null)
                        {
                            UpdateHealth(health.CurrentHealth.Value, 100);
                            yield break;
                        }
                    }
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void SetupDefaultGradients()
        {
            if (aliveGradient == null || aliveGradient.colorKeys.Length == 0)
            {
                aliveGradient = new Gradient();
                var colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(new Color(0.08f, 0.45f, 0.08f), 0.0f);
                colorKeys[1] = new GradientColorKey(new Color(0.2f, 1.0f, 0.25f), 1.0f);

                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                aliveGradient.SetKeys(colorKeys, alphaKeys);
            }

            if (lowHealthGradient == null || lowHealthGradient.colorKeys.Length == 0)
            {
                lowHealthGradient = new Gradient();
                var colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(new Color(0.55f, 0.05f, 0.05f), 0.0f);
                colorKeys[1] = new GradientColorKey(new Color(1.0f, 0.15f, 0.15f), 1.0f);

                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                lowHealthGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

public void SetScopeActive(bool isScoped)
        {
            if (scopeOverlay != null && isScoped && !scopeOverlay.activeSelf)
            {
                scopeOverlay.SetActive(true);
            }

            if (sniperScopeOverlay != null)
            {
                sniperScopeOverlay.SetScoped(isScoped);
            }
            else if (scopeOverlay != null)
            {
                Animator anim = scopeOverlay.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("IsScoped", isScoped);
                }
            }

            if (hipCrosshair != null) hipCrosshair.SetActive(!isScoped && Settings.SettingsManager.ShowCrosshair);
        }

        public void TriggerScopeRecoil(float kick = 1f)
        {
            if (sniperScopeOverlay != null)
            {
                sniperScopeOverlay.TriggerFireRecoil(kick);
            }
        }

        public void SetWallRunPrompt(bool visible, float progress)
        {
            if (wallRunPrompt != null)
            {
                if (visible && !wallRunPrompt.gameObject.activeSelf)
                {
                    wallRunPrompt.gameObject.SetActive(true);
                }
                wallRunPrompt.SetPromptState(visible, progress);
            }
        }

        public void UpdateCooldownBar(float progress, bool isReloading)
        {
            if (cooldownBarRoot == null || cooldownFillImage == null) return;

            bool isCoolingDown = progress < 1.0f;

            if (cooldownBarRoot.activeSelf != isCoolingDown)
            {
                cooldownBarRoot.SetActive(isCoolingDown);
            }

            if (isCoolingDown)
            {
                cooldownFillImage.fillAmount = Mathf.Clamp01(progress);
                cooldownFillImage.color = isReloading ? reloadCooldownColor : shootCooldownColor;
            }
        }

        public void ShowHitmarker(HitboxType hitboxType)
        {
            if (hitmarker == null || !Settings.SettingsManager.ShowHitmarkers) return;

            if (_hitmarkerCoroutine != null) StopCoroutine(_hitmarkerCoroutine);
            _hitmarkerCoroutine = StartCoroutine(HitmarkerFlashRoutine(hitboxType));
        }

        private IEnumerator HitmarkerFlashRoutine(HitboxType hitboxType)
        {
            Color markerColor = (hitboxType == HitboxType.Head) ? new Color(1f, 0.15f, 0.15f, 1f) : Color.white;

            if (hitmarkerText != null)
            {
                hitmarkerText.color = markerColor;
            }
            else
            {
                var tmp = hitmarker.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.color = markerColor;
            }

            hitmarker.SetActive(true);
            yield return new WaitForSeconds(hitmarkerDuration);
            hitmarker.SetActive(false);
        }

        public void TriggerDamageFlash()
        {
            if (damageVignetteGroup == null) return;
            if (!damageVignetteGroup.gameObject.activeSelf) damageVignetteGroup.gameObject.SetActive(true);

            if (_damageFlashCoroutine != null) StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            damageVignetteGroup.alpha = 0.85f;

            while (damageVignetteGroup.alpha > 0.01f)
            {
                damageVignetteGroup.alpha = Mathf.MoveTowards(damageVignetteGroup.alpha, 0f, Time.deltaTime * vignetteFadeSpeed);
                yield return null;
            }

            damageVignetteGroup.alpha = 0f;
        }

        private void ResolveAnnouncementElements()
        {
            if (announcementRoot == null && announcementText != null)
            {
                announcementRoot = announcementText.gameObject;
            }

            if (announcementRoot != null)
            {
                if (announcementCanvasGroup == null)
                    announcementCanvasGroup = announcementRoot.GetComponent<CanvasGroup>();

                if (announcementSubText == null)
                {
                    Transform t = announcementRoot.transform.Find("AnnouncementSubText");
                    if (t != null) announcementSubText = t.GetComponent<TextMeshProUGUI>();
                }

                if (announcementGlowText == null)
                {
                    Transform t = announcementRoot.transform.Find("AnnouncementGlow");
                    if (t != null) announcementGlowText = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public void ShowAnnouncement(string text, Color color, float duration)
        {
            ResolveAnnouncementElements();
            if (announcementText == null && announcementRoot == null) return;
            if (_announcementCoroutine != null) StopCoroutine(_announcementCoroutine);
            _announcementCoroutine = StartCoroutine(AnnouncementRoutine(text, color, duration));
        }

        private IEnumerator AnnouncementRoutine(string text, Color color, float duration)
        {
            ResolveAnnouncementElements();

            string clean = string.IsNullOrEmpty(text) ? string.Empty : text.Trim();
            string sub = "[ MATCH NOTICE ]";
            Color mainColor = color;
            Color glowColor = color;
            float targetFontSize = 54f;

            if (clean == "3")
            {
                sub = "[ PREPARE ]";
                mainColor = new Color(0.2f, 0.95f, 1f); // Vibrant cyber-cyan
                glowColor = new Color(0.1f, 0.9f, 1f, 0.38f);
                targetFontSize = 76f;
            }
            else if (clean == "2")
            {
                sub = "[ READY ]";
                mainColor = new Color(1f, 0.82f, 0.2f); // Vibrant amber/gold
                glowColor = new Color(1f, 0.75f, 0.15f, 0.38f);
                targetFontSize = 76f;
            }
            else if (clean == "1")
            {
                sub = "[ STEADY ]";
                mainColor = new Color(1f, 0.45f, 0.18f); // Alert orange
                glowColor = new Color(1f, 0.38f, 0.15f, 0.38f);
                targetFontSize = 76f;
            }
            else if (clean.StartsWith("GO", System.StringComparison.OrdinalIgnoreCase))
            {
                clean = "GO!";
                sub = "[ ENGAGE TARGETS ]";
                mainColor = new Color(0.15f, 1f, 0.52f); // Neon green
                glowColor = new Color(0.12f, 1f, 0.48f, 0.42f);
                targetFontSize = 80f;
            }
            else if (clean.Contains("WON"))
            {
                sub = "[ ROUND VICTORY ]";
                mainColor = new Color(0.15f, 1f, 0.52f);
                glowColor = new Color(0.12f, 1f, 0.48f, 0.38f);
                targetFontSize = 54f;
            }
            else if (clean.Contains("LOST"))
            {
                sub = "[ ROUND DEFEAT ]";
                mainColor = new Color(1f, 0.24f, 0.24f);
                glowColor = new Color(1f, 0.18f, 0.18f, 0.38f);
                targetFontSize = 54f;
            }
            else if (clean.Contains("TIME"))
            {
                sub = "[ ROUND TERMINATED ]";
                mainColor = new Color(1f, 0.78f, 0.2f);
                glowColor = new Color(1f, 0.7f, 0.15f, 0.38f);
                targetFontSize = 54f;
            }
            else if (clean.Contains("INTERMISSION"))
            {
                sub = "[ SYSTEM RECHARGING ]";
                mainColor = new Color(0.35f, 0.9f, 1f);
                glowColor = new Color(0.3f, 0.85f, 1f, 0.38f);
                targetFontSize = 48f;
            }
            else
            {
                glowColor = new Color(color.r, color.g, color.b, 0.35f);
            }

            if (announcementText != null)
            {
                announcementText.text = clean;
                announcementText.color = mainColor;
                announcementText.fontSize = targetFontSize;
            }

            if (announcementGlowText != null)
            {
                announcementGlowText.text = clean;
                announcementGlowText.color = glowColor;
                announcementGlowText.fontSize = targetFontSize + 3f;
            }

            if (announcementSubText != null)
            {
                announcementSubText.text = sub;
                announcementSubText.color = new Color(0.35f, 0.95f, 1f, 0.92f);
                announcementSubText.fontSize = 16f;
            }

            GameObject targetRoot = announcementRoot != null ? announcementRoot : (announcementText != null ? announcementText.gameObject : null);
            if (targetRoot == null) yield break;

            Transform rootT = targetRoot.transform;
            if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = 0f;
            rootT.localScale = Vector3.one * 1.35f;
            targetRoot.SetActive(true);

            float punchInDuration = Mathf.Min(0.14f, duration * 0.28f);
            float fadeOutDuration = Mathf.Min(0.18f, duration * 0.32f);
            float holdDuration = Mathf.Max(0f, duration - punchInDuration - fadeOutDuration);

            // Punch In: scale 1.35 -> 1.0, alpha 0 -> 1
            float elapsed = 0f;
            while (elapsed < punchInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / punchInDuration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                rootT.localScale = Vector3.LerpUnclamped(Vector3.one * 1.35f, Vector3.one, easeOut);
                if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = easeOut;
                yield return null;
            }

            rootT.localScale = Vector3.one;
            if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = 1f;

            // Hold
            if (holdDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            // Fade Out: scale 1.0 -> 1.08, alpha 1 -> 0
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                rootT.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, t);
                if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            targetRoot.SetActive(false);
            rootT.localScale = Vector3.one;
            if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = 1f;
        }

        private void ResolveAmmoElements()
        {
            if (ammoRoot == null)
            {
                if (ammoText != null)
                {
                    if (ammoText.transform.parent != null && ammoText.transform.parent.name.Contains("Ammo"))
                        ammoRoot = ammoText.transform.parent.gameObject;
                    else
                        ammoRoot = ammoText.gameObject;
                }
            }

            if (ammoRoot != null)
            {
                if (ammoGlowText == null)
                {
                    Transform t = ammoRoot.transform.Find("AmmoValueGlow");
                    if (t != null) ammoGlowText = t.GetComponent<TextMeshProUGUI>();
                }
                if (ammoMagLabel == null)
                {
                    Transform t = ammoRoot.transform.Find("AmmoMagLabel");
                    if (t != null) ammoMagLabel = t.GetComponent<TextMeshProUGUI>();
                }
                if (ammoSubLabel == null)
                {
                    Transform t = ammoRoot.transform.Find("AmmoSubLabel");
                    if (t != null) ammoSubLabel = t.GetComponent<TextMeshProUGUI>();
                }
                if (ammoBulletIcons == null || ammoBulletIcons.Length == 0)
                {
                    Transform row = ammoRoot.transform.Find("BulletsRow");
                    if (row != null)
                    {
                        ammoBulletIcons = row.GetComponentsInChildren<Image>(true);
                    }
                }
            }
        }

        public void UpdateAmmo(int currentClip, int maxClip, bool isReloading)
        {
            ResolveAmmoElements();

            if (ammoText == null && ammoRoot == null) return;

            string formattedValue;
            Color textColor;
            Color glowColor;

            if (isReloading)
            {
                formattedValue = "RELOAD";
                textColor = new Color(1f, 0.82f, 0.2f, 1f); // Vibrant amber
                glowColor = new Color(1f, 0.75f, 0.15f, 0.35f);
            }
            else
            {
                if (currentClip == 0)
                {
                    textColor = new Color(1f, 0.25f, 0.25f, 1f); // Alert red
                    glowColor = new Color(1f, 0.2f, 0.2f, 0.38f);
                }
                else
                {
                    textColor = new Color(0.2f, 0.98f, 0.92f, 1f); // Sci-fi cyan
                    glowColor = new Color(0.1f, 0.9f, 0.85f, 0.35f);
                }
                formattedValue = $"{currentClip:D2} / {maxClip:D2}";
            }

            if (ammoText != null)
            {
                ammoText.text = formattedValue;
                ammoText.color = textColor;
            }

            if (ammoGlowText != null)
            {
                ammoGlowText.text = formattedValue;
                ammoGlowText.color = glowColor;
            }

            if (ammoBulletIcons != null && ammoBulletIcons.Length > 0)
            {
                Color loadedBullet = new Color(0.15f, 1f, 0.88f, 1f); // Glowing cyan
                Color spentBullet = new Color(0.12f, 0.42f, 0.45f, 0.22f); // Translucent empty
                Color reloadingBullet = new Color(1f, 0.82f, 0.2f, 0.45f); // Amber pulse

                for (int i = 0; i < ammoBulletIcons.Length; i++)
                {
                    if (ammoBulletIcons[i] == null) continue;

                    if (i < maxClip)
                    {
                        if (!ammoBulletIcons[i].gameObject.activeSelf)
                            ammoBulletIcons[i].gameObject.SetActive(true);

                        if (isReloading)
                        {
                            ammoBulletIcons[i].color = reloadingBullet;
                        }
                        else
                        {
                            ammoBulletIcons[i].color = (i < currentClip) ? loadedBullet : spentBullet;
                        }
                    }
                    else
                    {
                        if (ammoBulletIcons[i].gameObject.activeSelf)
                            ammoBulletIcons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void UpdateStamina(float currentStamina, float maxStamina)
        {
            if (staminaSlider != null)
            {
                staminaSlider.maxValue = maxStamina;
                staminaSlider.value = currentStamina;
            }
        }

        private void ResolveScoreboardElements()
        {
            if (scoreText == null) return;
            Transform root = scoreText.transform;
            if (youScoreText == null)
            {
                Transform t = root.Find("YouScoreText");
                if (t != null) youScoreText = t.GetComponent<TextMeshProUGUI>();
            }
            if (youScoreGlowText == null)
            {
                Transform t = root.Find("YouScoreGlow");
                if (t != null) youScoreGlowText = t.GetComponent<TextMeshProUGUI>();
            }
            if (enemyScoreText == null)
            {
                Transform t = root.Find("EnemyScoreText");
                if (t != null) enemyScoreText = t.GetComponent<TextMeshProUGUI>();
            }
            if (enemyScoreGlowText == null)
            {
                Transform t = root.Find("EnemyScoreGlow");
                if (t != null) enemyScoreGlowText = t.GetComponent<TextMeshProUGUI>();
            }
            if (roundBannerText == null)
            {
                Transform t = root.Find("RoundText");
                if (t != null) roundBannerText = t.GetComponent<TextMeshProUGUI>();
            }
            if (youScoreText != null || roundBannerText != null || enemyScoreText != null)
            {
                scoreText.text = string.Empty;
            }
        }

        public void UpdateMatchScore(int hostScore, int clientScore, int currentRound)
        {
            if (scoreText == null) return;

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            int myScore = isHost ? hostScore : clientScore;
            int enemyScore = isHost ? clientScore : hostScore;

            ResolveScoreboardElements();

            if (youScoreText != null || roundBannerText != null || enemyScoreText != null)
            {
                scoreText.text = string.Empty;
                string myStr = myScore.ToString();
                string enemyStr = enemyScore.ToString();
                if (youScoreText != null) youScoreText.text = myStr;
                if (youScoreGlowText != null) youScoreGlowText.text = myStr;
                if (enemyScoreText != null) enemyScoreText.text = enemyStr;
                if (enemyScoreGlowText != null) enemyScoreGlowText.text = enemyStr;
                if (roundBannerText != null) roundBannerText.text = $"ROUND {currentRound}";
                return;
            }

            scoreText.text = $"ROUND {currentRound}  |  <color=#00FF66>YOU: {myScore}</color> - <color=#FF4444>ENEMY: {enemyScore}</color>";
        }

        public void UpdateTimer(float timeRemaining)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            string timeStr = $"{minutes:00}:{seconds:00}";

            bool isWarning = timeRemaining <= 10f;
            Color normalColor = new Color(0.88f, 0.98f, 1f, 1f);
            Color warningColor = new Color(1f, 0.28f, 0.28f, 1f);

            timerText.text = timeStr;
            timerText.color = isWarning ? warningColor : normalColor;

            if (timerGlowText == null)
            {
                Transform glowT = timerText.transform.parent != null ? timerText.transform.parent.Find("TimerValueGlow") : null;
                if (glowT != null) timerGlowText = glowT.GetComponent<TextMeshProUGUI>();
            }

            if (timerGlowText != null)
            {
                timerGlowText.text = timeStr;
                timerGlowText.color = isWarning ? new Color(1f, 0.2f, 0.2f, 0.28f) : new Color(0.25f, 0.95f, 1f, 0.26f);
            }
        }

        public void ShowMatchEndScreen(bool isWinner)
        {
            if (matchEndPanel != null) matchEndPanel.SetActive(true);
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);
            if (rematchStatusText != null) rematchStatusText.text = "";
            if (restartButton != null) restartButton.interactable = true;

            ApplyMatchEndTheme(isWinner);

            if (_matchEndFadeRoutine != null) StopCoroutine(_matchEndFadeRoutine);
            if (gameObject.activeInHierarchy)
            {
                _matchEndFadeRoutine = StartCoroutine(FadeMatchEndRoutine());
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideMatchEndScreen()
        {
            if (_matchEndFadeRoutine != null)
            {
                StopCoroutine(_matchEndFadeRoutine);
                _matchEndFadeRoutine = null;
            }

            if (matchEndPanel != null) matchEndPanel.SetActive(false);
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);
            if (rematchStatusText != null) rematchStatusText.text = "";
            if (restartButton != null) restartButton.interactable = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnRestartButtonClicked()
        {
            if (restartButton != null)
            {
                restartButton.interactable = false;
                var effect = restartButton.GetComponent<MainMenuButtonEffect>();
                if (effect != null) effect.ApplyInstantIdleState();
            }

            if (rematchStatusText != null) rematchStatusText.text = "<color=#FFCC00>Waiting for opponent...</color>";

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RequestRematch();
            }
        }

        public void ShowRematchPrompt()
        {
            if (rematchPromptPanel != null)
            {
                rematchPromptPanel.SetActive(true);
                ApplyMatchEndTheme();
                var effects = rematchPromptPanel.GetComponentsInChildren<MainMenuButtonEffect>(true);
                for (int i = 0; i < effects.Length; i++)
                {
                    if (effects[i] != null) effects[i].ApplyInstantIdleState();
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnAcceptRematchClicked()
        {
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RespondRematch(true);
            }
        }

        private void OnDeclineRematchClicked()
        {
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RespondRematch(false);
            }
        }

        public void OnRematchDeclined()
        {
            if (rematchStatusText != null)
            {
                rematchStatusText.text = "<color=#FF4444>Opponent declined rematch.</color>";
            }

            if (restartButton != null)
            {
                restartButton.interactable = true;
                var effect = restartButton.GetComponent<MainMenuButtonEffect>();
                if (effect != null) effect.ApplyInstantIdleState();
            }
        }

        private void OnReturnToMenuClicked()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("01_MainMenu");
        }

        #region Match End AAA Visual Theme
        [ContextMenu("Apply Match End Theme")]
        public void ApplyMatchEndTheme(bool? isWinner = null)
        {
            if (matchEndPanel == null) return;

            // AAA Tactical Palette (matching PauseMenu, PlayPanel, LoadoutPanel & SettingsPanel)
            Color dimmerBg = new Color(0.015f, 0.025f, 0.045f, 0.88f);         // Fullscreen tactical dimmer
            Color cardSlateBg = new Color(0.043f, 0.059f, 0.090f, 0.95f);      // #0B0F17 Deep Obsidian Command Card
            Color cardBorderColor = new Color(0.149f, 0.231f, 0.329f, 0.85f);  // #263B54 Tactical Steel-Cyan Border
            Color neonCyan = new Color(0f, 0.898f, 1f, 0.90f);                 // #00E5FF Electric Cyan Accent
            Color neonCyanSubtle = new Color(0f, 0.898f, 1f, 0.55f);           // #00E5FF Laser Divider
            Color textIdleSilver = new Color(0.85f, 0.90f, 0.93f, 1f);         // #D9E6ED Signature Button Idle
            Color textHoverWhite = Color.white;                                // #FFFFFF Signature Button Hover
            Color textClickCyan = new Color(0.70f, 1.0f, 0.90f, 1f);           // #B3FFE6 Signature Button Click
            Color footerSlate = new Color(0.424f, 0.510f, 0.600f, 0.85f);      // #6C8299 Footer Hint

            // 1. Full-Screen Tactical Dimmer on MatchEndPanel
            var panelRt = matchEndPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = Vector2.zero;
                panelRt.localScale = Vector3.one;
            }

            var panelImg = matchEndPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.sprite = null;
                panelImg.color = dimmerBg;
                panelImg.raycastTarget = true;
            }

            var panelOutline = matchEndPanel.GetComponent<Outline>();
            if (panelOutline != null) panelOutline.enabled = false;

            // 2. Edge Atmosphere Radial Glows
            var leftGlow = GetOrCreateImage(matchEndPanel.transform, "LeftAtmoGlow", new Vector2(-460f, 0f), new Vector2(400f, 800f));
            leftGlow.sprite = JoinUISprites.AtmosphereGlow;
            leftGlow.color = new Color(0f, 0.85f, 1f, 0.08f);
            leftGlow.raycastTarget = false;
            leftGlow.transform.SetSiblingIndex(0);

            var rightGlow = GetOrCreateImage(matchEndPanel.transform, "RightAtmoGlow", new Vector2(460f, 0f), new Vector2(400f, 800f));
            rightGlow.sprite = JoinUISprites.AtmosphereGlow;
            rightGlow.color = new Color(0f, 0.85f, 1f, 0.08f);
            rightGlow.raycastTarget = false;
            rightGlow.transform.SetSiblingIndex(1);

            // 3. Centered Tactical Command Card ("MatchEndCardBg" 500x520)
            Transform oldCardTr = matchEndPanel.transform.Find("Image");
            string cardName = oldCardTr != null ? "Image" : "MatchEndCardBg";
            var cardBgImg = GetOrCreateImage(matchEndPanel.transform, cardName, Vector2.zero, new Vector2(500f, 520f));
            cardBgImg.name = "MatchEndCardBg";
            cardBgImg.sprite = JoinUISprites.CardFill;
            cardBgImg.type = Image.Type.Sliced;
            cardBgImg.color = cardSlateBg;
            cardBgImg.raycastTarget = true;
            cardBgImg.transform.SetSiblingIndex(2);

            var cardOutline = cardBgImg.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = cardBgImg.gameObject.AddComponent<Outline>();
            cardOutline.enabled = true;
            cardOutline.effectColor = cardBorderColor;
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Top & Bottom Laser Accents inside MatchEndCardBg
            var topAccent = GetOrCreateImage(cardBgImg.transform, "TopLaserAccent", new Vector2(0f, 258.5f), new Vector2(500f, 3f));
            topAccent.sprite = null;
            topAccent.color = neonCyan;
            topAccent.raycastTarget = false;

            var bottomAccent = GetOrCreateImage(cardBgImg.transform, "BottomLaserAccent", new Vector2(0f, -259f), new Vector2(180f, 2f));
            bottomAccent.sprite = null;
            bottomAccent.color = new Color(0f, 0.898f, 1f, 0.45f);
            bottomAccent.raycastTarget = false;

            // 4. Sub-Header ("pathetic opponent" / "pathetic loser")
            var subtitleTmp = GetOrCreateText(matchEndPanel.transform, "MatchEndSubtitle", new Vector2(0f, 195f), new Vector2(440f, 24f));
            if (isWinner.HasValue)
            {
                subtitleTmp.text = isWinner.Value ? "pathetic opponent" : "pathetic loser";
            }
            else
            {
                subtitleTmp.text = "pathetic opponent";
            }
            subtitleTmp.color = new Color(0f, 0.898f, 1f, 0.85f);
            subtitleTmp.fontSize = 14f;
            subtitleTmp.characterSpacing = 4f;
            subtitleTmp.fontStyle = FontStyles.Bold;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.raycastTarget = false;

            // 5. Hero Title ("VICTORY!" / "DEFEAT")
            if (matchEndTitle != null)
            {
                var titleRt = matchEndTitle.rectTransform;
                titleRt.anchoredPosition = new Vector2(0f, 142f);
                titleRt.sizeDelta = new Vector2(440f, 68f);
                matchEndTitle.fontSize = 54f;
                matchEndTitle.characterSpacing = 5f;
                matchEndTitle.fontStyle = FontStyles.Bold;
                matchEndTitle.alignment = TextAlignmentOptions.Center;
                matchEndTitle.raycastTarget = false;

                if (isWinner.HasValue)
                {
                    matchEndTitle.text = isWinner.Value
                        ? "<color=#00FFAA>VICTORY!</color>"
                        : "<color=#FF2E4C>DEFEAT</color>";
                }
            }

            // 6. Sleek Laser Divider Bar (340 x 2px)
            Transform oldDivTr = matchEndPanel.transform.Find("Image (1)");
            string divName = oldDivTr != null ? "Image (1)" : "MatchEndLaserDivider";
            var laserDividerImg = GetOrCreateImage(matchEndPanel.transform, divName, new Vector2(0f, 95f), new Vector2(340f, 2f));
            laserDividerImg.name = "MatchEndLaserDivider";
            laserDividerImg.sprite = null;
            laserDividerImg.color = neonCyanSubtle;
            laserDividerImg.raycastTarget = false;

            // 7. Signature AAA Bordered Buttons with HoverArrows & MainMenuButtonEffect
            StyleCombatButton(restartButton, new Vector2(0f, 25f), "REMATCH", textIdleSilver, textHoverWhite, textClickCyan, new Vector2(320f, 52f), 26f);
            StyleCombatButton(returnToMenuButton, new Vector2(0f, -45f), "MAIN MENU", textIdleSilver, textHoverWhite, textClickCyan, new Vector2(320f, 52f), 26f);

            // 8. RematchStatusText
            if (rematchStatusText != null)
            {
                var statusRt = rematchStatusText.rectTransform;
                statusRt.anchoredPosition = new Vector2(0f, -115f);
                statusRt.sizeDelta = new Vector2(420f, 32f);
                rematchStatusText.fontSize = 15f;
                rematchStatusText.characterSpacing = 2f;
                rematchStatusText.fontStyle = FontStyles.Bold;
                rematchStatusText.alignment = TextAlignmentOptions.Center;
            }

            // 9. Tactical Footer Status Hint
            var footerHintTmp = GetOrCreateText(matchEndPanel.transform, "MatchEndFooterHint", new Vector2(0f, -215f), new Vector2(400f, 24f));
            footerHintTmp.text = "[ COMBAT PROTOCOL TERMINATED ]";
            footerHintTmp.color = footerSlate;
            footerHintTmp.fontSize = 12f;
            footerHintTmp.characterSpacing = 3f;
            footerHintTmp.fontStyle = FontStyles.Bold;
            footerHintTmp.alignment = TextAlignmentOptions.Center;
            footerHintTmp.raycastTarget = false;

            // 10. Tactical Rematch Consent Prompt Panel
            if (rematchPromptPanel != null)
            {
                var promptRt = rematchPromptPanel.GetComponent<RectTransform>();
                if (promptRt != null)
                {
                    promptRt.anchorMin = new Vector2(0.5f, 0.5f);
                    promptRt.anchorMax = new Vector2(0.5f, 0.5f);
                    promptRt.pivot = new Vector2(0.5f, 0.5f);
                    promptRt.anchoredPosition = Vector2.zero;
                    promptRt.sizeDelta = new Vector2(500f, 240f);
                    promptRt.localScale = Vector3.one;
                }

                var promptImg = rematchPromptPanel.GetComponent<Image>();
                if (promptImg != null)
                {
                    promptImg.sprite = JoinUISprites.CardFill;
                    promptImg.type = Image.Type.Sliced;
                    promptImg.color = new Color(0.043f, 0.059f, 0.090f, 0.98f);
                    promptImg.raycastTarget = true;
                }

                var promptOutline = rematchPromptPanel.GetComponent<Outline>();
                if (promptOutline == null) promptOutline = rematchPromptPanel.AddComponent<Outline>();
                promptOutline.enabled = true;
                promptOutline.effectColor = cardBorderColor;
                promptOutline.effectDistance = new Vector2(1.5f, -1.5f);

                var promptTopAccent = GetOrCreateImage(rematchPromptPanel.transform, "PromptTopAccent", new Vector2(0f, 118.5f), new Vector2(500f, 3f));
                promptTopAccent.sprite = null;
                promptTopAccent.color = neonCyan;
                promptTopAccent.raycastTarget = false;

                // Hide legacy BackroundLines
                var bgLines = rematchPromptPanel.transform.Find("BackroundLines");
                if (bgLines != null) bgLines.gameObject.SetActive(false);

                // Subtitle
                var promptSub = GetOrCreateText(rematchPromptPanel.transform, "PromptSubtitle", new Vector2(0f, 75f), new Vector2(400f, 20f));
                promptSub.text = "You want to rematch?";
                promptSub.color = new Color(0f, 0.898f, 1f, 0.85f);
                promptSub.fontSize = 12f;
                promptSub.characterSpacing = 3f;
                promptSub.fontStyle = FontStyles.Bold;
                promptSub.alignment = TextAlignmentOptions.Center;
                promptSub.raycastTarget = false;

                // Title
                Transform titleTr = rematchPromptPanel.transform.Find("Titel");
                if (titleTr != null)
                {
                    var promptTitleRt = titleTr.GetComponent<RectTransform>();
                    if (promptTitleRt != null)
                    {
                        promptTitleRt.anchoredPosition = new Vector2(0f, 42f);
                        promptTitleRt.sizeDelta = new Vector2(440f, 40f);
                    }
                    var promptTitleTmp = titleTr.GetComponent<TextMeshProUGUI>();
                    if (promptTitleTmp != null)
                    {
                        promptTitleTmp.text = "REMATCH CHALLENGE";
                        promptTitleTmp.color = Color.white;
                        promptTitleTmp.fontSize = 28f;
                        promptTitleTmp.characterSpacing = 4f;
                        promptTitleTmp.fontStyle = FontStyles.Bold;
                        promptTitleTmp.alignment = TextAlignmentOptions.Center;
                    }
                }

                // Message
                var promptMsg = GetOrCreateText(rematchPromptPanel.transform, "PromptMessage", new Vector2(0f, 6f), new Vector2(440f, 24f));
                promptMsg.text = "OPPONENT HAS REQUESTED A REMATCH";
                promptMsg.color = new Color(0.85f, 0.90f, 0.93f, 0.90f);
                promptMsg.fontSize = 14f;
                promptMsg.characterSpacing = 2f;
                promptMsg.fontStyle = FontStyles.Bold;
                promptMsg.alignment = TextAlignmentOptions.Center;
                promptMsg.raycastTarget = false;

                StyleCombatButton(acceptRematchButton, new Vector2(-110f, -55f), "ACCEPT", textIdleSilver, textHoverWhite, textClickCyan, new Vector2(190f, 48f), 22f);
                StyleCombatButton(declineRematchButton, new Vector2(110f, -55f), "DECLINE", textIdleSilver, textHoverWhite, textClickCyan, new Vector2(190f, 48f), 22f);
            }
        }

        private Image GetOrCreateImage(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tr = parent.Find(name);
            Image img;
            if (tr == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                tr.gameObject.SetActive(true);
                img = tr.GetComponent<Image>();
                if (img == null) img = tr.gameObject.AddComponent<Image>();
            }

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            return img;
        }

        private TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tr = parent.Find(name);
            TextMeshProUGUI tmp;
            if (tr == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                tmp = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                tr.gameObject.SetActive(true);
                tmp = tr.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = tr.gameObject.AddComponent<TextMeshProUGUI>();
            }

            if (matchEndTitle != null && matchEndTitle.font != null)
            {
                tmp.font = matchEndTitle.font;
            }

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            return tmp;
        }

        private void StyleCombatButton(Button btn, Vector2 pos, string label, Color textNormal, Color textHover, Color textClick, Vector2? size = null, float fontSize = 26f)
        {
            if (btn == null) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = size.HasValue ? size.Value : new Vector2(320f, 52f);
                rt.localScale = Vector3.one;
            }

            var img = btn.targetGraphic as Image ?? btn.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }

            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            btn.transition = Selectable.Transition.None;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                if (matchEndTitle != null && matchEndTitle.font != null)
                {
                    tmp.font = matchEndTitle.font;
                }

                tmp.text = label;
                tmp.color = textNormal;
                tmp.fontSize = fontSize;
                tmp.characterSpacing = 3f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }

            if (hoverArrowSprite == null)
            {
                hoverArrowSprite = MainMenuButtonEffect.GetDefaultLeftArrow();
            }
            if (hoverArrowRightSprite == null)
            {
                hoverArrowRightSprite = MainMenuButtonEffect.GetDefaultRightArrow();
            }

            Transform leftArrowTr = btn.transform.Find("HoverArrow") ?? btn.transform.Find("Arrow");
            if (leftArrowTr != null && hoverArrowSprite != null)
            {
                var arrowImg = leftArrowTr.GetComponent<Image>();
                if (arrowImg != null && arrowImg.sprite == null)
                {
                    arrowImg.sprite = hoverArrowSprite;
                }
            }

            Transform rightArrowTr = btn.transform.Find("HoverArrowRight") ?? btn.transform.Find("RightArrow") ?? btn.transform.Find("ArrowRight");
            if (rightArrowTr != null && hoverArrowRightSprite != null)
            {
                var rightArrowImg = rightArrowTr.GetComponent<Image>();
                if (rightArrowImg != null && rightArrowImg.sprite == null)
                {
                    rightArrowImg.sprite = hoverArrowRightSprite;
                }
            }

            var effect = btn.GetComponent<MainMenuButtonEffect>();
            if (effect == null) effect = btn.gameObject.AddComponent<MainMenuButtonEffect>();

            if (hoverArrowSprite != null) effect.ArrowSprite = hoverArrowSprite;
            if (hoverArrowRightSprite != null) effect.RightArrowSprite = hoverArrowRightSprite;

            effect.InitializeComponents();
            effect.CalculateArrowPositions();
            effect.ApplyInstantIdleState();
        }

        private IEnumerator FadeMatchEndRoutine()
        {
            if (matchEndPanel == null) yield break;

            var cg = matchEndPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = matchEndPanel.AddComponent<CanvasGroup>();

            var cardBg = matchEndPanel.transform.Find("MatchEndCardBg") as RectTransform;
            if (cardBg == null) cardBg = matchEndPanel.transform.Find("Image") as RectTransform;
            var laserDivider = matchEndPanel.transform.Find("MatchEndLaserDivider") as RectTransform;
            if (laserDivider == null) laserDivider = matchEndPanel.transform.Find("Image (1)") as RectTransform;
            var titleTr = matchEndTitle != null ? matchEndTitle.rectTransform : null;
            var subtitleTr = matchEndPanel.transform.Find("MatchEndSubtitle") as RectTransform;

            RectTransform[] btnRects = new RectTransform[]
            {
                restartButton != null ? restartButton.GetComponent<RectTransform>() : null,
                returnToMenuButton != null ? returnToMenuButton.GetComponent<RectTransform>() : null
            };
            Vector2[] btnTargetPositions = new Vector2[]
            {
                new Vector2(0f, 25f),
                new Vector2(0f, -45f)
            };

            // Reset all button hover arrows immediately on open
            var effects = matchEndPanel.GetComponentsInChildren<MainMenuButtonEffect>(true);
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null) effects[i].ApplyInstantIdleState();
            }

            float duration = 0.24f;
            float elapsed = 0f;
            cg.alpha = 0f;

            Vector3 cardStartScale = Vector3.one * 0.95f;
            Vector3 cardTargetScale = Vector3.one;

            if (cardBg != null) cardBg.localScale = cardStartScale;
            if (laserDivider != null) laserDivider.localScale = new Vector3(0f, 1f, 1f);
            if (titleTr != null) titleTr.anchoredPosition = new Vector2(0f, 134f);
            if (subtitleTr != null) subtitleTr.anchoredPosition = new Vector2(0f, 189f);

            for (int i = 0; i < btnRects.Length; i++)
            {
                if (btnRects[i] != null)
                    btnRects[i].anchoredPosition = btnTargetPositions[i] - new Vector2(0f, 12f);
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, progress);

                cg.alpha = Mathf.Clamp01(progress * 1.35f);

                if (cardBg != null)
                    cardBg.localScale = Vector3.LerpUnclamped(cardStartScale, cardTargetScale, ease);

                if (laserDivider != null)
                {
                    float divT = Mathf.Clamp01((elapsed - 0.03f) / (duration * 0.85f));
                    laserDivider.localScale = new Vector3(Mathf.SmoothStep(0f, 1f, divT), 1f, 1f);
                }

                if (titleTr != null)
                    titleTr.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 134f), new Vector2(0f, 142f), ease);

                if (subtitleTr != null)
                    subtitleTr.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 189f), new Vector2(0f, 195f), ease);

                for (int i = 0; i < btnRects.Length; i++)
                {
                    if (btnRects[i] != null)
                    {
                        float delay = i * 0.05f;
                        float btnT = Mathf.Clamp01((elapsed - delay) / (duration * 0.80f));
                        float btnEase = Mathf.SmoothStep(0f, 1f, btnT);
                        Vector2 startPos = btnTargetPositions[i] - new Vector2(0f, 12f);
                        btnRects[i].anchoredPosition = Vector2.LerpUnclamped(startPos, btnTargetPositions[i], btnEase);
                    }
                }

                yield return null;
            }

            cg.alpha = 1f;
            if (cardBg != null) cardBg.localScale = cardTargetScale;
            if (laserDivider != null) laserDivider.localScale = Vector3.one;
            if (titleTr != null) titleTr.anchoredPosition = new Vector2(0f, 142f);
            if (subtitleTr != null) subtitleTr.anchoredPosition = new Vector2(0f, 195f);

            for (int i = 0; i < btnRects.Length; i++)
            {
                if (btnRects[i] != null)
                    btnRects[i].anchoredPosition = btnTargetPositions[i];
            }

            cg.interactable = true;
            cg.blocksRaycasts = true;

            _matchEndFadeRoutine = null;
        }
        #endregion

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (currentHealth > 0)
            {
                if (healthFillImage != null)
                {
                    if (currentHealth > 30 && aliveGradient != null)
                    {
                        healthFillImage.color = aliveGradient.Evaluate(healthRatio);
                    }
                    else if (currentHealth <= 30 && lowHealthGradient != null)
                    {
                        float lowHealthRatio = Mathf.Clamp01((float)currentHealth / 30f);
                        healthFillImage.color = lowHealthGradient.Evaluate(lowHealthRatio);
                    }
                }

                if (healthBackgroundImage != null)
                {
                    healthBackgroundImage.color = aliveBackgroundColor;
                }

                if (healthText != null)
                {
                    healthText.text = $"{currentHealth} / {maxHealth} HP";
                    healthText.color = currentHealth <= 30 ? new Color(1f, 0.4f, 0.4f) : Color.white;
                }
            }
            else
            {
                if (healthBackgroundImage != null)
                {
                    healthBackgroundImage.color = deadBackgroundColor;
                }

                if (healthFillImage != null)
                {
                    healthFillImage.color = new Color(1f, 0.1f, 0.1f, 1f);
                }

                if (healthText != null)
                {
                    healthText.text = "ELIMINATED (0 HP)";
                    healthText.color = new Color(1.0f, 0.3f, 0.3f);
                }
            }

            bool isNormalHealth = currentHealth > 30;
            if (normalHealthBorder != null) normalHealthBorder.SetActive(isNormalHealth);
            if (lowHealthBorder != null) lowHealthBorder.SetActive(!isNormalHealth);

            int nextState = currentHealth > 30 ? 1 : (currentHealth > 0 ? 2 : 3);
            SetParticleState(nextState);
        }

        private void SetParticleState(int state)
        {
            if (_currentParticleState == state && state != 3) return;
            _currentParticleState = state;

            switch (state)
            {
                case 1:
                    if (lowHealthUIParticle != null)
                    {
                        lowHealthUIParticle.StopEmission();
                        lowHealthUIParticle.Clear();
                    }
                    if (deathExplosionUIParticle != null)
                    {
                        deathExplosionUIParticle.StopEmission();
                        deathExplosionUIParticle.Clear();
                    }
                    if (normalHealthUIParticle != null)
                    {
                        normalHealthUIParticle.StartEmission();
                        normalHealthUIParticle.Play();
                    }
                    break;

                case 2:
                    if (normalHealthUIParticle != null)
                    {
                        normalHealthUIParticle.StopEmission();
                        normalHealthUIParticle.Clear();
                    }
                    if (deathExplosionUIParticle != null)
                    {
                        deathExplosionUIParticle.StopEmission();
                        deathExplosionUIParticle.Clear();
                    }
                    if (lowHealthUIParticle != null)
                    {
                        lowHealthUIParticle.StartEmission();
                        lowHealthUIParticle.Play();
                    }
                    break;

                case 3:
                    if (normalHealthUIParticle != null)
                    {
                        normalHealthUIParticle.StopEmission();
                        normalHealthUIParticle.Clear();
                    }
                    if (lowHealthUIParticle != null)
                    {
                        lowHealthUIParticle.StopEmission();
                        lowHealthUIParticle.Clear();
                    }
                    if (deathExplosionUIParticle != null)
                    {
                        deathExplosionUIParticle.Clear();
                        deathExplosionUIParticle.StartEmission();
                        deathExplosionUIParticle.Play();
                    }
                    break;
            }
        }
    }
}