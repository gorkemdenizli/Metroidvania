using UnityEngine;

// Scriptable stats for a gun; assign to Weapon component.
[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Min(1)] public int damage = 10;
    [Tooltip("Shots per second.")]
    public float fireRate = 6f;
    [Range(0f, 10f)]
    [Tooltip("Nişan hassasiyeti (0–10). 10 = tam isabet (sapma yok), 0 = maksimum sapma (±10°).")]
    public float accuracy = 8f;
    [Min(1)] public int magazineSize = 12;
    [Tooltip("Total rounds at spawn (mag fills first, rest is reserve).")]
    [Min(0)] public int startingTotalAmmo = 120;
    [Tooltip("Sınırsız toplam mermi; şarjör yine de azalır ve dolar, toplam mermi asla tükenmez.")]
    public bool infiniteAmmo = false;
    [Tooltip("Seconds to refill magazine.")]
    public float reloadSpeed = 1.5f;
    public float bulletSpeed = 18f;
    public Sprite weaponSprite;

    [Header("Screen Shake")]
    [Tooltip("Max positional offset (world units) applied to the camera on each shot.")]
    [Min(0f)] public float shootShakeIntensity = 0.08f;
    [Tooltip("Seconds the shake lasts per shot.")]
    [Min(0f)] public float shootShakeDuration = 0.08f;
}
