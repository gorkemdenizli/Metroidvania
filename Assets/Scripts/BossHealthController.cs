using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthController : MonoBehaviour
{
    public static BossHealthController instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] public int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private BossBattle theBoss;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;

        UpdateHealthSlider(currentHealth, maxHealth);
    }

    public void UpdateHealthSlider(int currentHealth, int maxHealth)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }

    public void DamageBoss(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            theBoss.EndBattle();
        }

        UpdateHealthSlider(currentHealth, maxHealth);
    }


}
