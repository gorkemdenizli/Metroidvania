using UnityEngine;

public class BossBattle : MonoBehaviour
{
    [SerializeField] private int treshold1;
    [SerializeField] private int treshold2;
    [SerializeField] private float activeTime;
    [SerializeField] private float fadeOutTime;
    [SerializeField] private float inactiveTime;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform theBoss;

    private float activeCounter;
    private float fadeOutCounter;
    private float inactiveCounter;
    private Transform targetPoint;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activeCounter = activeTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (BossHealthController.instance.currentHealth > treshold1)
        {
            if (activeCounter > 0)
            {
                activeCounter -= Time.deltaTime;
                if (activeCounter <= 0)
                {
                    fadeOutCounter = fadeOutTime;
                    anim.SetTrigger("vanish");
                }
            }
            else if (fadeOutCounter > 0)
            {
                fadeOutCounter -= Time.deltaTime;
                if (fadeOutCounter <= 0)
                {
                    theBoss.gameObject.SetActive(false);
                    inactiveCounter = inactiveTime;
                }
            }
            else if (inactiveCounter > 0)
            {
                inactiveCounter -= Time.deltaTime;
                if (inactiveCounter <= 0)
                {
                    theBoss.position = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                    theBoss.gameObject.SetActive(true);

                    activeCounter = activeTime;
                }
            }
        }
    }

    public void EndBattle()
    {
        gameObject.SetActive(false);
    }
}
