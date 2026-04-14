using UnityEngine;

public class EnemyDrops : MonoBehaviour
{
    [Header("Health pickup")]
    [SerializeField] private bool shouldDropHealthPickup = true;
    [Range(0f, 1f)]
    [SerializeField] private float healthPickupChance;
    [SerializeField] private GameObject healthPickupPrefab;

    public void TrySpawnDrops(Vector3 position)
    {
        if (shouldDropHealthPickup && healthPickupPrefab != null && Random.value <= healthPickupChance)
            Instantiate(healthPickupPrefab, position, Quaternion.identity);
    }
}
