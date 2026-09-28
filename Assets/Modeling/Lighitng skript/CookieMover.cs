using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CookieMover: MonoBehaviour
{
    [Tooltip("Sleep hier je Light-object of component in.")]
    public UniversalAdditionalLightData lightData;

    [Tooltip("De snelheid waarmee de X-offset verandert.")]
    public float snelheid = 2f;

    [Tooltip("De sterkte/afstand van de beweging.")]
    public float amplitude = 1f;

    private float startOffsetX;

    void Start()
    {
        if (lightData != null)
        {
            // Sla de begin X-offset op
            startOffsetX = lightData.lightCookieOffset.x;
        }
        else
        {
            Debug.LogError("Vergeet niet om de Universal Additional Light Data in te slepen in het script!");
        }
    }

    void Update()
    {
        if (lightData != null)
        {
            // Bereken de nieuwe X-offset via een sinusbeweging
            float nieuweX = startOffsetX + Mathf.Sin(Time.time * snelheid) * amplitude;

            // Pas de Cookie Offset X aan
            lightData.lightCookieOffset = new Vector2(nieuweX, lightData.lightCookieOffset.y);
        }
    }
}