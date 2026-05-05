using System.Collections;
using UnityEngine;

// Attach to the enemy root (same object as EnemyHealthController).
// Bullet calls Apply(direction) after a successful hit.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour
{
    [Tooltip("How far (units) the enemy travels during knockback.")]
    [SerializeField] private float knockbackDistance = 1.5f;

    [Tooltip("Speed (units/s) at which the enemy is pushed during knockback.")]
    [SerializeField] private float knockbackSpeed = 10f;

    [Tooltip("Seconds the enemy stays frozen in place after reaching the knockback target.")]
    [SerializeField] private float stunDuration = 0.25f;

    private Rigidbody2D _rb;
    private Behaviour _mover;
    private Coroutine _routine;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = GetComponentInParent<Rigidbody2D>();

        // Search on self, parent, and children to handle varied prefab hierarchies.
        _mover = (Behaviour)GetComponent<EnemyPatroller>()
              ?? (Behaviour)GetComponentInParent<EnemyPatroller>()
              ?? (Behaviour)GetComponentInChildren<EnemyPatroller>()
              ?? (Behaviour)GetComponent<EnemyFlyerController>()
              ?? (Behaviour)GetComponentInParent<EnemyFlyerController>()
              ?? (Behaviour)GetComponentInChildren<EnemyFlyerController>();
    }

    // dir: normalised direction the bullet was travelling.
    public void Apply(Vector2 dir)
    {
        if (_rb == null) return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(KnockbackRoutine(dir.normalized));
    }

    IEnumerator KnockbackRoutine(Vector2 dir)
    {
        if (_mover != null) _mover.enabled = false;

        // --- Knockback travel phase ---
        float travelTime = knockbackDistance / Mathf.Max(0.01f, knockbackSpeed);
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            // Force velocity every physics step so the mover cannot override it even
            // if the component was not found / disabled correctly.
            _rb.linearVelocity = dir * knockbackSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- Stun phase: stand still ---
        _rb.linearVelocity = Vector2.zero;
        elapsed = 0f;

        while (elapsed < stunDuration)
        {
            _rb.linearVelocity = Vector2.zero;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (_mover != null) _mover.enabled = true;
        _routine = null;
    }
}
