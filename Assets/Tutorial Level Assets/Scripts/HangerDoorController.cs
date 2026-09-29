using System.Collections;
using UnityEngine;

// Attach to the "HangerDoor" parent so the whole prefab (mesh + collider + audio) moves together.
public class HangerDoorController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float travelDistance = 7.51937f;
    [SerializeField, Min(0.01f)] private float moveDuration = 3f;

    [Header("Trigger")]
    [SerializeField, Min(0f)] private float postLineDelay = 1f;

    [Header("Confirmation Line")]
    [SerializeField, TextArea(2, 4)]
    private string confirmationLine = "Good job, you can walk, now lets see if you can run AND jump at the same time. Use SPACE to jump.";
    [SerializeField] private string confirmationClipResourcePath = "Voice/run_jump_confirmation";

    [Header("Door Audio")]
    [SerializeField] private string doorClipResourcePath = "Door/hangar_door_move";

    private AudioSource audioSource;
    private float startLocalY;
    private float endLocalY;
    private bool triggered;

    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        startLocalY = transform.localPosition.y;
        endLocalY = startLocalY - travelDistance;
    }

    private void OnEnable()
    {
        SpeakerDialogue.OnGreetingFinished += HandleGreetingFinished;
    }

    private void OnDisable()
    {
        SpeakerDialogue.OnGreetingFinished -= HandleGreetingFinished;
    }

    private void HandleGreetingFinished()
    {
        if (triggered) return;
        StartCoroutine(WaitForWasdThenOpen());
    }

    private IEnumerator WaitForWasdThenOpen()
    {
        if (postLineDelay > 0f) yield return new WaitForSeconds(postLineDelay);

        bool pressedW = false, pressedA = false, pressedS = false, pressedD = false;
        while (!(pressedW && pressedA && pressedS && pressedD))
        {
            pressedW |= Input.GetKeyDown(KeyCode.W);
            pressedA |= Input.GetKeyDown(KeyCode.A);
            pressedS |= Input.GetKeyDown(KeyCode.S);
            pressedD |= Input.GetKeyDown(KeyCode.D);
            yield return null;
        }

        if (triggered) yield break;
        triggered = true;

        StartCoroutine(MoveDoor());
        SpeakerDialogue speaker = FindFirstObjectByType<SpeakerDialogue>();
        if (speaker != null) speaker.SayLine(confirmationLine, confirmationClipResourcePath);
    }

    private IEnumerator MoveDoor()
    {
        if (audioSource != null)
        {
            if (audioSource.clip == null) audioSource.clip = Resources.Load<AudioClip>(doorClipResourcePath);
            audioSource.Play();
        }

        Vector3 localPosition = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            localPosition.y = Mathf.Lerp(startLocalY, endLocalY, elapsed / moveDuration);
            transform.localPosition = localPosition;
            yield return null;
        }

        localPosition.y = endLocalY;
        transform.localPosition = localPosition;
        if (audioSource != null) audioSource.Stop();
    }
}
