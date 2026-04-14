using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    [SerializeField] private int totalHealth;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private EnemyDrops enemyDrops;

    void Awake()
    {
        if (enemyDrops == null)
            enemyDrops = GetComponent<EnemyDrops>();
    }

    public void DamageEnemy(int damageAmount)
    {
        totalHealth -= damageAmount;

        if (totalHealth <= 0)
        {
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, transform.rotation);
            }

            if (enemyDrops != null)
                enemyDrops.TrySpawnDrops(transform.position);

            Destroy(gameObject);
        }
    }
}
