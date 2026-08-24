using UnityEngine;

public class Sniper : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform weaponTransform;

    [Header("Shooting")]
    public float shootDistance = 1000f;

    [Header("Aiming")]
    public float normalFOV = 60f;
    public float aimingFOV = 20f;
    public float zoomSpeed = 10f;

    [Header("Weapon Positions")]
    public Vector3 hipPosition = new Vector3(0.25f, -0.25f, 0.6f);
    public Vector3 aimPosition = new Vector3(0f, -0.05f, 0.45f);

    [Header("Weapon Rotation")]
    public Vector3 hipRotation = Vector3.zero;
    public Vector3 aimRotation = Vector3.zero;

    private bool isAiming;

    void Update()
    {
        HandleAiming();
        HandleShooting();
    }

    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1);

        float targetFOV = isAiming ? aimingFOV : normalFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            zoomSpeed * Time.deltaTime
        );

        Vector3 targetPosition = isAiming
            ? aimPosition
            : hipPosition;

        Vector3 targetRotation = isAiming
            ? aimRotation
            : hipRotation;

        weaponTransform.localPosition = Vector3.Lerp(
            weaponTransform.localPosition,
            targetPosition,
            zoomSpeed * Time.deltaTime
        );

        weaponTransform.localRotation = Quaternion.Lerp(
            weaponTransform.localRotation,
            Quaternion.Euler(targetRotation),
            zoomSpeed * Time.deltaTime
        );
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            Debug.Log("You shot: " + hit.collider.name);

            PlayerHealth enemy =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (enemy != null && enemy.gameObject != gameObject)
            {
                enemy.Die();
            }
        }
        else
        {
            Debug.Log("You missed.");
        }
    }
}