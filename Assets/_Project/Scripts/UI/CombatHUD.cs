using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        [Header("Health UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;

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

            // Beginstaat instellen: Crosshair altijd aan
            if (hitmarker != null) hitmarker.SetActive(false);
            if (scopeOverlay != null) scopeOverlay.SetActive(false);
            if (hipCrosshair != null) hipCrosshair.SetActive(true);
        }

        public void SetScopeActive(bool isScoped)
        {
            // Schakel alleen de scope donkere rand in/uit; het richtpunt in het midden blijft altijd zichtbaar
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
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"{currentHealth} / {maxHealth} HP";
            }
        }
    }
}