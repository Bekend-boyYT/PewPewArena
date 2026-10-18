using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Coffee.UIExtensions;
using SniperGame.Player;
using SniperGame.Gameplay;

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
            if (ammoText != null && !ammoText.gameObject.activeSelf) ammoText.gameObject.SetActive(true);
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

            InitializeUIParticles();
        }

        private void Start()
        {
            StartCoroutine(InitializeLocalPlayerRoutine());
        }

        private void Update()
        {
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

        public void UpdateAmmo(int currentClip, int maxClip, bool isReloading)
        {
            if (ammoText == null) return;

            if (isReloading)
            {
                ammoText.text = "<color=#FFCC00>RELOADING...</color>";
            }
            else
            {
                string ammoColor = (currentClip == 0) ? "<color=#FF3333>" : "<color=#FFFFFF>";
                ammoText.text = $"{ammoColor}{currentClip}</color> / {maxClip}";
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

            if (matchEndTitle != null)
            {
                matchEndTitle.text = isWinner ? "<color=#00FF66>VICTORY!</color>" : "<color=#FF2222>DEFEAT</color>";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideMatchEndScreen()
        {
            if (matchEndPanel != null) matchEndPanel.SetActive(false);
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(false);
            if (rematchStatusText != null) rematchStatusText.text = "";
            if (restartButton != null) restartButton.interactable = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnRestartButtonClicked()
        {
            if (restartButton != null) restartButton.interactable = false;
            if (rematchStatusText != null) rematchStatusText.text = "<color=#FFCC00>Waiting for opponent...</color>";

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RequestRematch();
            }
        }

        public void ShowRematchPrompt()
        {
            if (rematchPromptPanel != null) rematchPromptPanel.SetActive(true);
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