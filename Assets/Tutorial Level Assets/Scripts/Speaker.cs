using UnityEngine;

public class Speaker : MonoBehaviour
{
    [SerializeField] private float zOffset = 3f;
    [SerializeField] private float smoothTime = 0.3f;

    private Transform player;
    private float zVelocity;

    void Awake()
    {
        GameObject playerObject = GameObject.Find("Player Controller");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.z = Mathf.SmoothDamp(position.z, player.position.z + zOffset, ref zVelocity, smoothTime);
        transform.position = position;
    }
}
