using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float bulletSpeed;
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] public Vector2 moveDir;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private int damageAmount;

    // Update is called once per frame
    void Update()
    {
        theRB.linearVelocity = moveDir * bulletSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            collision.GetComponent<EnemyHealthController>()?.DamageEnemy(damageAmount);
        }

        if (collision.CompareTag("Boss"))
        {
            BossHealthController.instance.DamageBoss(damageAmount);
        }
        
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}