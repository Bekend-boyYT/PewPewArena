using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        [Header("Health UI Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Health Gradients")]
        [SerializeField] private Gradient aliveGradient;
        [SerializeField] private Color deadBackgroundColor = new Color(0.55f, 0.05f, 0.05f, 0.9f);
        [SerializeField] private Color aliveBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        [Header("Settings")]
        [SerializeField] private float hitmarkerDuration = 0.12f;

        private Coroutine _hitmarkerCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupDefaultGradients();

            if (hitmarker != null) hitmarker.SetActive(false);
            if (scopeOverlay != null) scopeOverlay.SetActive(false);
            if (hipCrosshair != null) hipCrosshair.SetActive(true);
        }

        private void Start()
        {
            // Initialiseer direct de health bar bij het inladen van de gameplay scene
            StartCoroutine(InitializeLocalHealthRoutine());
        }

        private IEnumerator InitializeLocalHealthRoutine()
        {
            // Wacht kort tot Netcode de lokale speler gekoppeld heeft in de nieuwe scene
            yield return new WaitForSeconds(0.1f);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (playerObj != null)
                {
                    var health = playerObj.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        UpdateHealth(health.CurrentHealth.Value, 100);
                    }
                }
            }
        }

        private void SetupDefaultGradients()
        {
            if (aliveGradient == null || aliveGradient.colorKeys.Length == 0)
            {
                aliveGradient = new Gradient();
                var colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(new Color(0.08f, 0.45f, 0.08f), 0.0f); // Donkergroen
                colorKeys[1] = new GradientColorKey(new Color(0.2f, 1.0f, 0.25f), 1.0f);  // Helder lichtgroen

                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                aliveGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void SetScopeActive(bool isScoped)
        {
            if (scopeOverlay != null) scopeOverlay.SetActive(isScoped);
            if (hipCrosshair != null) hipCrosshair.SetActive(true);
        }

        public void ShowHitmarker()
        {
            if (hitmarker == null) return;

            if (_hitmarkerCoroutine != null)
            {
                StopCoroutine(_hitmarkerCoroutine);
            }

            _hitmarkerCoroutine = StartCoroutine(HitmarkerFlashRoutine());
        }

        private IEnumerator HitmarkerFlashRoutine()
        {
            hitmarker.SetActive(true);
            yield return new WaitForSeconds(hitmarkerDuration);
            hitmarker.SetActive(false);
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
                // LEVEND: Groene Fill + Donkere Background
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
                // GEËLIMINEERD: Hele balk kleurt direct gevaarrood
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
        }
    }
}