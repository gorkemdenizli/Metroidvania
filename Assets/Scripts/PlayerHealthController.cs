using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    public static PlayerHealthController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } 
        else 
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibilityLength;
    private float invincibilityCounter;
    [SerializeField] private float flashLength;
    private float flashCounter;
    [SerializeField] private SpriteRenderer[] playerSprites;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;

        UIController.instance.UpdateHealthSlider(currentHealth, maxHealth);
    }

    // Update is called once per frame
    void Update()
    {
        if (invincibilityCounter > 0)
        {
            invincibilityCounter -= Time.deltaTime;

            flashCounter -= Time.deltaTime;

            if (flashCounter <= 0)
            {
                foreach (SpriteRenderer sr in playerSprites)
                {
                    sr.enabled = !sr.enabled;
                }
                flashCounter = flashLength;
            }

            if (invincibilityCounter <= 0)
            {
                foreach (SpriteRenderer sr in playerSprites)
                {
                    sr.enabled = true;
                }
                flashCounter = 0f;
            }
        }
    }

    public void DamagePlayer(int damageAmount)
    {
        if (invincibilityCounter <= 0)
        {
            currentHealth -= damageAmount;

            if (currentHealth <= 0)
            {
                currentHealth = 0;

                //gameObject.SetActive(false);
                RespawnController.instance.Respawn();
            }
            else
            {
                invincibilityCounter = invincibilityLength;
            }

            UIController.instance.UpdateHealthSlider(currentHealth, maxHealth);
        }
    }

    public void fillHealth()
    {
        currentHealth = maxHealth;

        UIController.instance.UpdateHealthSlider(currentHealth, maxHealth);
    }

    public void HealPlayer(int healAmount)
    {
        currentHealth += healAmount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        UIController.instance.UpdateHealthSlider(currentHealth, maxHealth);
    }
}
