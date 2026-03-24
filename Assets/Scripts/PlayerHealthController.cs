using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealthController : MonoBehaviour
{
    public static PlayerHealthController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        } 
        else 
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Scene players are destroyed when a DDOL player already exists, but Cinemachine
    /// still references the destroyed transform from the loaded scene. Rebind after load.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Transform t = transform;
        foreach (CinemachineCamera vcam in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
        {
            if (vcam == null)
                continue;

            CameraTarget ct = vcam.Target;
            ct.TrackingTarget = t;
            vcam.Target = ct;
        }

        // Duplicate scene Player OnDisable runs end-of-frame and disables shared InputActions.
        StartCoroutine(RestorePlayerInputAfterSceneDuplicateTeardown());
    }

    private IEnumerator RestorePlayerInputAfterSceneDuplicateTeardown()
    {
        yield return null;

        PlayerController pc = instance.GetComponent<PlayerController>();
        if (pc != null)
            pc.RestoreInputAfterSceneLoad();
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
