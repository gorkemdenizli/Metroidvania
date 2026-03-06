using UnityEngine;

public class EnemyPatroller : MonoBehaviour
{
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float waitAtPoints;
    [SerializeField] private float jumpForce;
    [SerializeField] private Transform[] patrolPoints;

    private int currentPatrolPoint;
    private float waitCounter;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waitCounter = waitAtPoints;
        
        foreach (Transform pPoint in patrolPoints)
        {
            pPoint.SetParent(null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(transform.position.x - patrolPoints[currentPatrolPoint].position.x) > 0.2f)
        {
            if (transform.position.x < patrolPoints[currentPatrolPoint].position.x)
            {
                theRB.linearVelocity = new Vector2(moveSpeed, theRB.linearVelocity.y);
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                theRB.linearVelocity = new Vector2(-moveSpeed, theRB.linearVelocity.y);
                transform.localScale = Vector3.one;
            }
        }
        else
        {
            theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y);
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                waitCounter = waitAtPoints;
                currentPatrolPoint++;

                if (currentPatrolPoint >= patrolPoints.Length)
                {
                    currentPatrolPoint = 0;
                }
            }
        }
    }
}
