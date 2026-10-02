using UnityEngine;

// Attach to the "OpenTheDoor" trigger; drops the OpenDoor panels and prompts the wallrun.
public class OpenTheDoorTrigger : MonoBehaviour
{
    [SerializeField] private string doorsParentName = "OpenDoor";
    [SerializeField] private string line = "Now wallrun";
    [SerializeField] private string clipResourcePath = "Voice/now_wallrun";

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || body.GetComponent<ParkourFPS.PlayerControllerScript>() == null) return;
        triggered = true;

        GameObject parent = GameObject.Find(doorsParentName);
        if (parent != null)
        {
            foreach (Transform door in parent.transform)
            {
                if (door.GetComponentInChildren<Collider>() == null)
                {
                    foreach (MeshFilter filter in door.GetComponentsInChildren<MeshFilter>())
                    {
                        MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                    }
                }

                if (door.GetComponent<Rigidbody>() == null) door.gameObject.AddComponent<Rigidbody>();
            }
        }

        SpeakerDialogue speaker = FindFirstObjectByType<SpeakerDialogue>();
        if (speaker != null) speaker.SayLine(line, clipResourcePath);
    }
}
