using UnityEngine;

public class BombController : MonoBehaviour
{
    [SerializeField] private float timeToExplode = .5f;
    [SerializeField] GameObject explosion;

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
        }
    }
}
