using UnityEngine;

public class Speaker : MonoBehaviour
{
    private Transform player;

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
        position.z = player.position.z;
        transform.position = position;
    }
}
