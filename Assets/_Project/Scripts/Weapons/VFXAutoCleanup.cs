using System.Collections;
using UnityEngine;

namespace SniperGame.Weapons
{
    public class VFXAutoCleanup : MonoBehaviour
    {
        [Tooltip("Lifetime in seconds before this effect GameObject is destroyed.")]
        [SerializeField] private float lifetime = 1.5f;

        [Tooltip("Optional dynamic light attached to this effect.")]
        [SerializeField] private Light flashLight;

        [Tooltip("How long the dynamic light should illuminate before fading out.")]
        [SerializeField] private float lightDuration = 0.06f;

        [Tooltip("Optional particle systems for environmental dust or debris to suppress on character hits.")]
        [SerializeField] private ParticleSystem[] dustEmitters;

        private float _initialLightIntensity = 1f;

        private void Awake()
        {
            if (flashLight == null)
            {
                flashLight = GetComponentInChildren<Light>(true);
            }

            if (flashLight != null)
            {
                _initialLightIntensity = flashLight.intensity;
                StartCoroutine(FadeLightRoutine());
            }

            if (dustEmitters == null || dustEmitters.Length == 0)
            {
                var allPs = GetComponentsInChildren<ParticleSystem>(true);
                var dustList = new System.Collections.Generic.List<ParticleSystem>();
                for (int i = 0; i < allPs.Length; i++)
                {
                    string n = allPs[i].gameObject.name.ToLowerInvariant();
                    if (n.Contains("dust") || n.Contains("debris") || n.Contains("vapor") || n.Contains("puncture"))
                    {
                        dustList.Add(allPs[i]);
                    }
                }
                dustEmitters = dustList.ToArray();
            }

            Destroy(gameObject, Mathf.Max(0.1f, lifetime));
        }

        public void SetCharacterHit(bool isCharacterHit)
        {
            if (!isCharacterHit) return;

            // When hitting a player/character, suppress environmental stone/dust debris
            if (dustEmitters != null)
            {
                for (int i = 0; i < dustEmitters.Length; i++)
                {
                    if (dustEmitters[i] != null)
                    {
                        dustEmitters[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        dustEmitters[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private IEnumerator FadeLightRoutine()
        {
            if (flashLight == null) yield break;

            yield return new WaitForSeconds(lightDuration);

            float fadeDuration = 0.04f;
            float elapsed = 0f;

            while (elapsed < fadeDuration && flashLight != null)
            {
                elapsed += Time.deltaTime;
                flashLight.intensity = Mathf.Lerp(_initialLightIntensity, 0f, elapsed / fadeDuration);
                yield return null;
            }

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }
        }
    }
}
