using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public void Die()
    {
        Debug.Log(gameObject.name + " died!");

        gameObject.SetActive(false);
    }
}