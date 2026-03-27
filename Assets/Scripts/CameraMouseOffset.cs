using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraMouseOffset : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private CinemachinePositionComposer composer;

    [Header("Dead Zone")]
    [SerializeField] private float deadZoneRadius;

    [Header("Offset")]
    [SerializeField] private float maxOffsetX;
    [SerializeField] private float maxOffsetY;

    [Header("Smoothing")]
    [SerializeField] private float smooth;

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player == null)
                return;
        }

        if (composer == null)
            return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // Viewport (0-1)
        Vector2 viewport = Camera.main.ScreenToViewportPoint(mouseScreen);

        // Clamp → dışarı çıksa bile 0-1 arası kalır
        viewport.x = Mathf.Clamp01(viewport.x);
        viewport.y = Mathf.Clamp01(viewport.y);

        // -1 ile +1 arası merkez bazlı değer
        Vector2 centered = (viewport - new Vector2(0.5f, 0.5f)) * 2f;

        // Dead zone (opsiyonel ama öneririm)
        if (centered.magnitude < deadZoneRadius)
            centered = Vector2.zero;

        Vector3 targetOffset = new Vector3(
            centered.x * maxOffsetX,
            centered.y * maxOffsetY * 0.5f,
            0f
        );

        // 🔥 Smooth'u düzgün hissettiren kısım
        composer.TargetOffset = Vector3.Lerp(
            composer.TargetOffset,
            targetOffset,
            1 - Mathf.Exp(-smooth * Time.deltaTime)
        );
    }
}
