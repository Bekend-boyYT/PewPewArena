using UnityEngine;
using System.IO;

public class CameraRender : MonoBehaviour
{
    public Camera mainCamera;

    public int width = 1920;
    public int height = 1080;

    private bool rendered = false;

    void Start()
    {
        if (rendered)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Geen Main Camera gevonden!");
            return;
        }

        rendered = true;

        RenderCamera();
    }

    void RenderCamera()
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);

        mainCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        mainCamera.Render();

        image.ReadPixels(
            new Rect(0, 0, width, height),
            0,
            0
        );

        image.Apply();

        mainCamera.targetTexture = null;
        RenderTexture.active = null;

        byte[] png = image.EncodeToPNG();

        string path = Path.Combine(
            Application.dataPath,
            "MainCameraRender.png"
        );

        File.WriteAllBytes(path, png);

        Destroy(renderTexture);
        Destroy(image);

        Debug.Log("Render gemaakt!");
        Debug.Log("Opgeslagen in: " + path);
    }
}