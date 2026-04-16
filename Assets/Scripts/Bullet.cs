using UnityEngine;

// Straight-line projectile; Weapon calls Initialize, legacy prefabs use Start + serialized speed/dir.
public class Bullet : MonoBehaviour
{
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] private GameObject impactEffect;

    [Header("Legacy prefab (boss etc.)")]
    [SerializeField] private float bulletSpeed;
    [SerializeField] public Vector2 moveDir;
    [SerializeField] private int damageAmount;

    private Vector2 _dir;
    private float _speed;
    private int _damage;
    private bool _initialized;

    // Sets direction, speed, damage from weapon; applies rotation and velocity.
    public void Initialize(Vector2 direction, float speed, int damage)
    {
        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _speed = speed;
        _damage = Mathf.Max(1, damage);
        _initialized = true;

        float z = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, z);

        if (theRB != null)
            theRB.linearVelocity = _dir * _speed;
    }

    // Boss / old prefabs without Initialize: use inspector bulletSpeed + moveDir.
    void Start()
    {
        if (_initialized)
            return;
        if (bulletSpeed > 0.001f && theRB != null)
        {
            Vector2 d = moveDir.sqrMagnitude > 1e-6f ? moveDir.normalized : Vector2.right;
            Initialize(d, bulletSpeed, Mathf.Max(1, damageAmount));
        }
    }

    void FixedUpdate()
    {
        if (!_initialized || theRB == null)
            return;
        theRB.linearVelocity = _dir * _speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
            collision.GetComponent<EnemyHealthController>()?.DamageEnemy(_damage);

        if (collision.CompareTag("Boss") && BossHealthController.instance != null)
            BossHealthController.instance.DamageBoss(_damage);

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
