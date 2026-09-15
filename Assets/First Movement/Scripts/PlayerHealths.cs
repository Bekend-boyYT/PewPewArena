using UnityEngine;

public class PlayerHealths : MonoBehaviour
{
    public void Die()
    {
        Debug.Log(gameObject.name + " died!");

        gameObject.SetActive(false);
    }
}