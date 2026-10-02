using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to "ShootingCubesSpawn"; its BoxCollider defines the area where aim-trainer cubes appear.
public class ShootingCubeSpawner : MonoBehaviour
{
    [SerializeField, Min(1)] private int targetKills = 5;
    [SerializeField, Min(0.1f)] private float relocateInterval = 2.5f;
    [SerializeField, Min(0.05f)] private float cubeSize = 0.6f;
    [SerializeField] private float maxShootDistance = 500f;
    [SerializeField] private string outroLine = "Wow you're not completely incompetent, good luck, you're gonna need it";
    [SerializeField] private string outroClipResourcePath = "Voice/shoot_outro";
    [SerializeField] private string mainMenuScene = "01_MainMenu";

    private BoxCollider area;
    private Transform cube;
    private Camera playerCamera;
    private bool active;
    private int kills;
    private float relocateTimer;

    private void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true;
    }

    public void StartSection()
    {
        if (active) return;
        active = true;
        kills = 0;
        SpawnCube();
    }

    private void Update()
    {
        if (!active) return;

        relocateTimer -= Time.deltaTime;
        if (relocateTimer <= 0f) MoveCube();

        if (Input.GetMouseButtonDown(0)) TryShoot();
    }

    private void TryShoot()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, ~0, QueryTriggerInteraction.Ignore)) return;
        if (hit.transform != cube) return;

        Destroy(cube.gameObject);
        cube = null;
        kills++;

        if (kills >= targetKills)
        {
            Finish();
            return;
        }

        SpawnCube();
    }

    private void Finish()
    {
        active = false;
        SpeakerDialogue speaker = FindFirstObjectByType<SpeakerDialogue>();
        if (speaker != null)
        {
            speaker.SayLine(outroLine, outroClipResourcePath, () => SceneManager.LoadScene(mainMenuScene));
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    private void SpawnCube()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ShootingTarget";
        go.transform.localScale = Vector3.one * cubeSize;

        Renderer cubeRenderer = go.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = new Color(1f, 0.2f, 0.2f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(2f, 0.2f, 0.2f));
        cubeRenderer.material = material;

        cube = go.transform;
        MoveCube();
    }

    private void MoveCube()
    {
        relocateTimer = relocateInterval;
        if (cube == null) return;

        Vector3 local = area.center + new Vector3(
            Random.Range(-0.5f, 0.5f) * area.size.x,
            Random.Range(-0.5f, 0.5f) * area.size.y,
            Random.Range(-0.5f, 0.5f) * area.size.z);
        cube.position = transform.TransformPoint(local);
    }
}
