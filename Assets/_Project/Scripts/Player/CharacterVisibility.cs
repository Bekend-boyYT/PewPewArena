using System.Collections.Generic;
using UnityEngine;

namespace SniperGame.Player
{
    /// <summary>
    /// Manages character lighting and opponent outline visibility.
    /// Ensures dynamic characters are clearly visible in baked and shadowed maps
    /// without affecting baked environment lighting (via URP Light Layers).
    /// </summary>
    public class CharacterVisibility : MonoBehaviour
    {
        [Header("Dedicated Character Light (Pillar 1)")]
        [Tooltip("Enables a dedicated fill light that only illuminates character meshes using Light Layer 1.")]
        [SerializeField] private bool enableCharacterLight = true;
        [SerializeField] private Color lightColor = new Color(1.0f, 0.98f, 0.95f, 1.0f);
        [SerializeField, Range(0.1f, 5.0f)] private float lightIntensity = 1.35f;
        [SerializeField, Range(0.5f, 10.0f)] private float lightRange = 3.5f;
        [SerializeField] private Vector3 lightOffset = new Vector3(0f, 1.35f, 0.35f);

        [Header("Enemy Outline (Pillar 3 - Option A)")]
        [Tooltip("Outline material using Inverted Hull shader.")]
        [SerializeField] private Material outlineMaterial;
        [Tooltip("Color of the outline for opponents.")]
        [SerializeField] private Color enemyOutlineColor = new Color(1.0f, 0.2f, 0.2f, 1.0f);
        [SerializeField, Range(0.5f, 5.0f)] private float outlineWidth = 2.0f;

        [Header("Visual References")]
        [Tooltip("The root GameObject containing the 3D soldier model (auto-detected if empty).")]
        [SerializeField] private GameObject visualsRoot;

        private Light characterLight;
        private bool isConfigured = false;
        private readonly List<Renderer> characterRenderers = new List<Renderer>();

        // URP Light Layer 1 bitmask: (1 << 1) = 2. Default is (1 << 0) = 1.
        private const int DefaultLightLayerInt = 1 << 0;
        private const int CharacterLightLayerInt = 1 << 1;
        private const int CombinedLightLayersInt = DefaultLightLayerInt | CharacterLightLayerInt;
        private const uint CombinedLightLayersUInt = (uint)CombinedLightLayersInt;

        private void Awake()
        {
            FindVisualsAndRenderers();

            if (outlineMaterial == null)
            {
                outlineMaterial = Resources.Load<Material>("EnemyOutline");
                if (outlineMaterial == null)
                {
                    // Fallback to loading by shader if resource not present
                    Shader outlineShader = Shader.Find("PewPew/InvertedHullOutline");
                    if (outlineShader != null)
                    {
                        outlineMaterial = new Material(outlineShader)
                        {
                            name = "Runtime_EnemyOutline"
                        };
                    }
                }
            }

            SetupCharacterLight();
        }

        private void Start()
        {
            // If running offline or singleplayer (not network spawned), configure default visibility
            if (!isConfigured)
            {
                Configure(false);
            }
        }

        private void FindVisualsAndRenderers()
        {
            characterRenderers.Clear();

            if (visualsRoot == null)
            {
                foreach (Transform child in transform)
                {
                    string childName = child.name.ToLower();
                    if (childName.Contains("camera") || childName.Contains("weapon") || childName.Contains("hitbox"))
                        continue;

                    if (child.GetComponentInChildren<Renderer>() != null)
                    {
                        visualsRoot = child.gameObject;
                        break;
                    }
                }
            }

            if (visualsRoot != null)
            {
                var renderers = visualsRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null && !r.name.ToLower().Contains("weapon"))
                    {
                        characterRenderers.Add(r);
                    }
                }
            }
        }

        private void SetupCharacterLight()
        {
            if (!enableCharacterLight) return;

            if (characterLight == null)
            {
                GameObject lightObj = new GameObject("CharacterFillLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = lightOffset;

                characterLight = lightObj.AddComponent<Light>();
                characterLight.type = LightType.Point;
                characterLight.range = lightRange;
                characterLight.color = lightColor;
                characterLight.intensity = lightIntensity;
                characterLight.shadows = LightShadows.None;

                // Restrict this light STRICTLY to Light Layer 1 so it cannot illuminate the environment
                characterLight.renderingLayerMask = CharacterLightLayerInt;
            }

            // Assign character renderers to receive both Default and Character light layers
            ApplyRenderingLayersToCharacter();
        }

        private void ApplyRenderingLayersToCharacter()
        {
            for (int i = 0; i < characterRenderers.Count; i++)
            {
                Renderer r = characterRenderers[i];
                if (r != null)
                {
                    r.renderingLayerMask = CombinedLightLayersUInt;
                }
            }
        }

        /// <summary>
        /// Configures character visibility based on whether this player instance is the local owner or remote opponent.
        /// </summary>
        public void Configure(bool isOwner)
        {
            FindVisualsAndRenderers();
            ApplyRenderingLayersToCharacter();

            if (isOwner)
            {
                // Local player: remove any outline so 1st person / shadows remain clean
                RemoveOutline();
            }
            else
            {
                // Opponent: apply high-visibility enemy outline
                ApplyEnemyOutline();
            }

            isConfigured = true;
        }

        private void ApplyEnemyOutline()
        {
            if (outlineMaterial == null) return;

            // Create an instance of the outline material with configured color & width
            Material activeOutlineMat = new Material(outlineMaterial);
            activeOutlineMat.SetColor("_OutlineColor", enemyOutlineColor);
            activeOutlineMat.SetFloat("_OutlineWidth", outlineWidth);

            for (int i = 0; i < characterRenderers.Count; i++)
            {
                Renderer r = characterRenderers[i];
                if (r == null) continue;

                Material[] currentMats = r.sharedMaterials;
                bool alreadyHasOutline = false;
                for (int m = 0; m < currentMats.Length; m++)
                {
                    if (currentMats[m] != null && currentMats[m].shader != null && 
                        currentMats[m].shader.name == "PewPew/InvertedHullOutline")
                    {
                        alreadyHasOutline = true;
                        break;
                    }
                }

                if (!alreadyHasOutline)
                {
                    Material[] newMats = new Material[currentMats.Length + 1];
                    for (int m = 0; m < currentMats.Length; m++)
                    {
                        newMats[m] = currentMats[m];
                    }
                    newMats[currentMats.Length] = activeOutlineMat;
                    r.sharedMaterials = newMats;
                }
            }
        }

        private void RemoveOutline()
        {
            for (int i = 0; i < characterRenderers.Count; i++)
            {
                Renderer r = characterRenderers[i];
                if (r == null) continue;

                Material[] currentMats = r.sharedMaterials;
                List<Material> cleanedMats = new List<Material>();
                for (int m = 0; m < currentMats.Length; m++)
                {
                    if (currentMats[m] != null && currentMats[m].shader != null && 
                        currentMats[m].shader.name == "PewPew/InvertedHullOutline")
                    {
                        continue;
                    }
                    cleanedMats.Add(currentMats[m]);
                }

                if (cleanedMats.Count != currentMats.Length)
                {
                    r.sharedMaterials = cleanedMats.ToArray();
                }
            }
        }

        private void OnValidate()
        {
            if (characterLight != null)
            {
                characterLight.enabled = enableCharacterLight;
                characterLight.color = lightColor;
                characterLight.intensity = lightIntensity;
                characterLight.range = lightRange;
                characterLight.transform.localPosition = lightOffset;
                characterLight.renderingLayerMask = CharacterLightLayerInt;
            }
        }
    }
}
