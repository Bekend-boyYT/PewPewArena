using UnityEngine;

// Attach to the "Death" trigger; teleports the player to "DeathRespawn" and has the speaker insult them.
public class DeathZone : MonoBehaviour
{
    [SerializeField] private string respawnObjectName = "DeathRespawn";
    [SerializeField] private string line = "Stupid";
    [SerializeField] private string clipResourcePath = "Voice/stupid";

    private Transform respawn;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || body.GetComponent<ParkourFPS.PlayerControllerScript>() == null) return;

        if (respawn == null)
        {
            GameObject target = GameObject.Find(respawnObjectName);
            if (target == null) return;
            respawn = target.transform;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = respawn.position;
        body.transform.position = respawn.position;

        SpeakerDialogue speaker = FindFirstObjectByType<SpeakerDialogue>();
        if (speaker != null) speaker.SayLine(line, clipResourcePath);
    }
}
