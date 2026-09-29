using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MatrixEntityMarker : MonoBehaviour
{
    private readonly List<(GameObject target, int layer)> previousLayers = new List<(GameObject, int)>();

    private void OnEnable()
    {
        int matrixLayer = LayerMask.NameToLayer("MatrixEntity");
        if (matrixLayer < 0)
        {
            Debug.LogWarning("MatrixEntity layer is not configured.", this);
            return;
        }

        previousLayers.Clear();
        Transform[] hierarchy = GetComponentsInChildren<Transform>(true);
        foreach (Transform item in hierarchy)
        {
            previousLayers.Add((item.gameObject, item.gameObject.layer));
            item.gameObject.layer = matrixLayer;
        }
    }

    private void OnDisable()
    {
        foreach ((GameObject target, int layer) in previousLayers)
        {
            if (target != null) target.layer = layer;
        }

        previousLayers.Clear();
    }
}
