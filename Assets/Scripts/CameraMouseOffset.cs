using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraMouseOffset : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private CinemachinePositionComposer composer;

    [Header("Dead Zone")]
    [SerializeField] private float deadZoneRadius = 2f;

    [Header("Offset")]
    [SerializeField] private float maxOffsetX = 3f;
    [SerializeField] private float maxOffsetY = 2f;

    [Header("Smoothing")]
    [SerializeField] private float smooth = 6f;

    void Update()
    {
        if (player == null) return;

        Vector2 playerPos = player.transform.position;
        Vector2 mousePos = player.mouseWorldPos;

        Vector2 toMouse = mousePos - playerPos;
        float distance = toMouse.magnitude;

        Vector3 targetOffset = Vector3.zero;

        // If the mouse is outside the dead zone
        if (distance > deadZoneRadius)
        {
            float excessDistance = distance - deadZoneRadius;

            // Normalized direction
            Vector2 dir = toMouse.normalized;

            targetOffset = new Vector3(
                dir.x * Mathf.Clamp(excessDistance, 0, maxOffsetX),
                dir.y * Mathf.Clamp(excessDistance, 0, maxOffsetY) * 0.5f,
                0f
            );
        }

        composer.TargetOffset = Vector3.Lerp(
            composer.TargetOffset,
            targetOffset,
            Time.deltaTime * smooth
        );
    }
}
