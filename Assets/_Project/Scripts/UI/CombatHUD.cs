using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Coffee.UIExtensions;
using SniperGame.Player;

namespace SniperGame.UI
{
    public class CombatHUD : MonoBehaviour
    {
        public static CombatHUD Instance { get; private set; }

        [Header("HUD Panels & Reticles")]
        [SerializeField] private GameObject hudContainer;
        [SerializeField] private GameObject hipCrosshair;
        [SerializeField] private GameObject scopeOverlay;
        [SerializeField] private GameObject hitmarker;
        [SerializeField] private TextMeshProUGUI hitmarkerText;

        [Header("Damage Vignette Feedback")]
        [SerializeField] private CanvasGroup damageVignetteGroup;
        [SerializeField] private float vignetteFadeSpeed = 4.0f;

        [Header("Health UI Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Health UI Particle Groups")]
        [Tooltip("UIParticle component op FX_NormalHealth")]
        [SerializeField] private UIParticle normalHealthUIParticle;

        [Tooltip("UIParticle component op FX_LowHealth")]
        [SerializeField] private UIParticle lowHealthUIParticle;

        [Tooltip("UIParticle component op FX_DeathExplosion")]
        [SerializeField] private UIParticle deathExplosionUIParticle;

        [Header("Stamina UI Elements")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFillImage;

        [Header("Ammo UI Elements")]
        [SerializeField] private TextMeshProUGUI ammoText;

        [Header("Match & Round UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI announcementText;
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TextMeshProUGUI matchEndTitle;
        [SerializeField] private Button returnToMenuButton;

        [Header("Health Gradients")]
        [SerializeField] private Gradient aliveGradient;
        [SerializeField] private Color deadBackgroundColor = new Color(0.55f, 0.05f, 0.05f, 0.9f);
        [SerializeField] private Color aliveBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        [Header("Settings")]
        [SerializeField] private float hitmarkerDuration = 0.12f;

        private Coroutine _hitmarkerCoroutine;
        private Coroutine _announcementCoroutine;
        private Coroutine _damageFlashCoroutine;
        private int _currentParticleState = -1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SetupDefaultGradients();

            if (hitmarker != null) hitmarker.SetActive(false);
            if (scopeOverlay != null) scopeOverlay.SetActive(false);
            if (hipCrosshair != null) hipCrosshair.SetActive(true);
            if (announcementText != null) announcementText.gameObject.SetActive(false);
            if (matchEndPanel != null) matchEndPanel.SetActive(false);
            if (damageVignetteGroup != null) damageVignetteGroup.alpha = 0f;

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
        }

        public void SetScopeActive(bool isScoped)
        {
            if (scopeOverlay != null) scopeOverlay.SetActive(isScoped);
            
            // Crosshair verdwijnt bij richten en verschijnt weer bij heupschot
            if (hipCrosshair != null) hipCrosshair.SetActive(!isScoped);
        }

        public void ShowHitmarker(HitboxType hitboxType)
        {
            if (hitmarker == null) return;

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

        public void ShowAnnouncement(string text, Color color, float duration)
        {
            if (announcementText == null) return;
            if (_announcementCoroutine != null) StopCoroutine(_announcementCoroutine);
            _announcementCoroutine = StartCoroutine(AnnouncementRoutine(text, color, duration));
        }

        private IEnumerator AnnouncementRoutine(string text, Color color, float duration)
        {
            announcementText.text = text;
            announcementText.color = color;
            announcementText.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            announcementText.gameObject.SetActive(false);
        }

        public void UpdateAmmo(int currentClip, int maxClip, bool isReloading)
        {
            if (ammoText == null) return;

            if (isReloading)
            {
                ammoText.text = "<color=#FFCC00>HERLADEN...</color>";
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

        public void UpdateMatchScore(int hostScore, int clientScore, int currentRound)
        {
            if (scoreText == null) return;

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            int myScore = isHost ? hostScore : clientScore;
            int enemyScore = isHost ? clientScore : hostScore;

            scoreText.text = $"RONDE {currentRound}  |  <color=#00FF66>JIJ: {myScore}</color> - <color=#FF4444>VIJAND: {enemyScore}</color>";
        }

        public void UpdateTimer(float timeRemaining)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = $"{minutes:00}:{seconds:00}";
            timerText.color = (timeRemaining <= 10f) ? new Color(1f, 0.25f, 0.25f) : Color.white;
        }

        public void ShowMatchEndScreen(bool isWinner)
        {
            if (matchEndPanel != null) matchEndPanel.SetActive(true);

            if (matchEndTitle != null)
            {
                matchEndTitle.text = isWinner ? "<color=#00FF66>VICTORY!</color>" : "<color=#FF2222>DEFEAT</color>";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
                if (healthFillImage != null && aliveGradient != null)
                {
                    healthFillImage.color = aliveGradient.Evaluate(healthRatio);
                }

                if (healthBackgroundImage != null)
                {
                    healthBackgroundImage.color = aliveBackgroundColor;
                }

                if (healthText != null)
                {
                    healthText.text = $"{currentHealth} / {maxHealth} HP";
                    healthText.color = Color.white;
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
                    healthText.text = "GEËLIMINEERD (0 HP)";
                    healthText.color = new Color(1.0f, 0.3f, 0.3f);
                }
            }

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