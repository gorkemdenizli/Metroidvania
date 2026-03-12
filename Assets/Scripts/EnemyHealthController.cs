using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    [SerializeField] private int totalHealth;
    [SerializeField] private GameObject deathEffect;
    [Header("Drops")]
    [SerializeField] private GameObject healthPickupPrefab;
    [Range(0f, 1f)]
    [SerializeField] private float healthPickupChance = 0.25f;

    public void DamageEnemy(int damageAmount)
    {
        totalHealth -= damageAmount;

        if (totalHealth <= 0)
        {
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, transform.rotation);
            }

            if (healthPickupPrefab != null && Random.value <= healthPickupChance)
            {
                Instantiate(healthPickupPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
