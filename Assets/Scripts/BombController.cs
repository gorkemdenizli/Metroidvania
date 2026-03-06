using UnityEngine;

public class BombController : MonoBehaviour
{
    [SerializeField] private float timeToExplode = .75f;
    [SerializeField] GameObject explosion;
    [SerializeField] private float bombBlastRadius = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeToExplode -= Time.deltaTime;

        if (timeToExplode <= 0)
        {
            if (explosion != null)
            {
                Instantiate(explosion, transform.position, transform.rotation);
            }

            Destroy(gameObject);
            
            Collider2D[] objectsToDamage = Physics2D.OverlapCircleAll(transform.position, bombBlastRadius);
        }
    }
}
