using UnityEngine;

// Attach to "ShootingSectionTrigger"; introduces the shooting section and starts the cube spawner.
public class ShootingSectionTrigger : MonoBehaviour
{
    [SerializeField] private ShootingCubeSpawner spawner;
    [SerializeField] private string spawnerObjectName = "ShootingCubesSpawn";
    [SerializeField] private string line = "So congrats, you can move. Now lets see if you can shoot";
    [SerializeField] private string clipResourcePath = "Voice/shoot_intro";

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || body.GetComponent<ParkourFPS.PlayerControllerScript>() == null) return;
        triggered = true;

        if (spawner == null)
        {
            GameObject target = GameObject.Find(spawnerObjectName);
            if (target != null) spawner = target.GetComponent<ShootingCubeSpawner>();
        }

        SpeakerDialogue speaker = FindFirstObjectByType<SpeakerDialogue>();
        if (speaker != null)
        {
            speaker.SayLine(line, clipResourcePath, StartSpawner);
        }
        else
        {
            StartSpawner();
        }
    }

    private void StartSpawner()
    {
        if (spawner != null) spawner.StartSection();
    }
}
