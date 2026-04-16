using UnityEngine;

// Scriptable stats for a gun; assign to Weapon component.
[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Min(1)] public int damage = 10;
    [Tooltip("Shots per second.")]
    public float fireRate = 6f;
    [Tooltip("Random spread in degrees applied to bullet rotation (half-angle range).")]
    public float accuracy = 2f;
    [Min(1)] public int magazineSize = 12;
    [Tooltip("Total rounds at spawn (mag fills first, rest is reserve).")]
    [Min(0)] public int startingTotalAmmo = 120;
    [Tooltip("Seconds to refill magazine.")]
    public float reloadSpeed = 1.5f;
    public float bulletSpeed = 18f;
    public Sprite weaponSprite;
}
