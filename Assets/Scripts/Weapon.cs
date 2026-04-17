using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
// Aim at mouse, fire bullets from magazine with spread, auto / manual reload coroutine.
public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private PlayerController player;

    [Tooltip("World aim origin (e.g. player root Z plane). Defaults to player transform.")]
    [SerializeField] private Transform aimPlaneRoot;

    [Tooltip("Rotates to point at cursor (e.g. gun arm pivot).")]
    [SerializeField] private Transform weaponPivot;

    [Tooltip("Spawn point for bullets; aim ray uses muzzle -> cursor for direction.")]
    [SerializeField] private Transform muzzle;

    [Tooltip("Optional extra transform; if no SpriteRenderer is found for flip, scale.y is used only when muzzle is NOT a child of this.")]
    [SerializeField] private Transform weaponVisual;

    [Tooltip("Gun sprite; flipY used when aiming left (does not mirror child transforms — keep Shot Point under Pivot, not under this sprite if possible).")]
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;

    [Header("Muzzle clearance (aim left)")]
    [Tooltip("Transform whose localPosition is nudged when aiming left; default = this Weapon root.")]
    [SerializeField] private Transform positionShiftTarget;

    [Tooltip("Added to cached base local pos when cursor is left of body (e.g. small negative X).")]
    [SerializeField] private Vector3 localOffsetWhenAimLeft;

    [Tooltip("Added to Shot Point localPosition (parent = Pivot) when aiming left. flipY moves only the sprite; raise Y here if bullets spawn below the barrel (e.g. 0, 0.15, 0).")]
    [SerializeField] private Vector3 muzzleLocalAdjustWhenAimLeft;

    [Header("Ammo UI (TMP)")]
    [Tooltip("Şarjördeki mermi. Total TMP atanmadıysa eski gibi \"mag / total\" tek satırda burada gösterilir.")]
    [FormerlySerializedAs("combinedAmmoText")]
    [FormerlySerializedAs("ammoCountText")]
    [SerializeField] private TMP_Text ammoMagazineText;
    [Tooltip("Genelde sabit \"/\"; ayrı TMP istemezsen boş bırak.")]
    [SerializeField] private TMP_Text ammoSeparatorText;
    [Tooltip("Sağdaki toplam sayı (_displayTotalAmmo). Reload bitene kadar mag boşken gecikmeli güncellenme aynı.")]
    [SerializeField] private TMP_Text ammoTotalText;

    [Tooltip("Optional UI Image next to ammo text — shows WeaponData.weaponSprite (static icon).")]
    [SerializeField] private Image ammoHudWeaponImage;

    [Tooltip("Optional SpriteRenderer on HUD (if not using UI Image) — same sprite.")]
    [SerializeField] private SpriteRenderer ammoHudWeaponSprite;

    [Header("Reload UI")]
    [Tooltip("Reload sırasında görünür; süre boyunca 0 → 1 dolar, bitince kapanır.")]
    [SerializeField] private Slider reloadProgressSlider;

    private int _magAmmo;
    private int _reserveAmmo;
    // Shown as right-hand number; decreases when reload finishes (by rounds moved from reserve), not each shot.
    private int _displayTotalAmmo;
    private float _nextShotTime;
    private bool _reloading;
    private Coroutine _reloadRoutine;
    private Vector3 _baseShiftLocalPos;
    private Vector3 _baseMuzzleLocalPos;

    // Cache starting ammo and sprite.
    void Awake()
    {
        if (aimPlaneRoot == null && player != null)
            aimPlaneRoot = player.transform;

        Transform shiftTarget = positionShiftTarget != null ? positionShiftTarget : transform;
        _baseShiftLocalPos = shiftTarget.localPosition;

        if (muzzle != null)
            _baseMuzzleLocalPos = muzzle.localPosition;

        IgnorePlayerVsShotLayerCollision();

        if (weaponData != null)
        {
            InitializeAmmoFromStartingTotal();
            ApplyWeaponSprite();
        }

        RefreshAmmoUi();

        if (reloadProgressSlider != null)
        {
            reloadProgressSlider.minValue = 0f;
            reloadProgressSlider.maxValue = 1f;
            reloadProgressSlider.gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (reloadProgressSlider != null)
            reloadProgressSlider.gameObject.SetActive(false);
    }

    // Split starting total into magazine + reserve.
    void InitializeAmmoFromStartingTotal()
    {
        int cap = weaponData.magazineSize;
        int total = Mathf.Max(0, weaponData.startingTotalAmmo);
        _magAmmo = Mathf.Min(cap, total);
        _reserveAmmo = total - _magAmmo;
        _displayTotalAmmo = total;
    }

    // mag / total UI; total matches carried after reload, lags while mag is empty until reload completes.
    void RefreshAmmoUi()
    {
        if (weaponData == null)
            return;

        int carried = _magAmmo + _reserveAmmo;
        if (carried == 0)
            _displayTotalAmmo = 0;

        if (ammoMagazineText != null && ammoTotalText != null)
        {
            ammoMagazineText.text = _magAmmo.ToString();
            ammoTotalText.text = _displayTotalAmmo.ToString();
            if (ammoSeparatorText != null)
                ammoSeparatorText.text = "/";
        }
        else if (ammoMagazineText != null)
        {
            ammoMagazineText.text = _magAmmo + " / " + _displayTotalAmmo;
        }
        else if (ammoTotalText != null)
        {
            ammoTotalText.text = _magAmmo + " / " + _displayTotalAmmo;
        }
    }

    // Call from pickups etc.
    public void AddReserveAmmo(int amount)
    {
        if (amount <= 0)
            return;
        _reserveAmmo += amount;
        _displayTotalAmmo += amount;
        RefreshAmmoUi();
    }

    public int MagAmmo => _magAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public int TotalAmmoRemaining => _magAmmo + _reserveAmmo;

    // Bullets on Shot layer should never collide with Player layer (spawn inside collider).
    static void IgnorePlayerVsShotLayerCollision()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int shotLayer = LayerMask.NameToLayer("Shot");
        if (playerLayer < 0 || shotLayer < 0)
            return;
        Physics2D.IgnoreLayerCollision(playerLayer, shotLayer, true);
    }

    // Left-aim mirror: use sprite flipY so muzzle children are not mirrored in world space.
    void ApplyLeftAimVisual(bool mouseLeftOfBody)
    {
        SpriteRenderer sr = weaponSpriteRenderer;
        if (sr == null && weaponVisual != null)
            sr = weaponVisual.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.flipY = mouseLeftOfBody;
            sr.transform.localScale = Vector3.one;
            if (weaponVisual != null && weaponVisual != sr.transform)
                weaponVisual.localScale = Vector3.one;
            return;
        }

        if (weaponVisual == null)
            return;

        bool muzzleUnderVisual = muzzle != null && muzzle.transform.IsChildOf(weaponVisual);
        if (muzzleUnderVisual)
        {
            weaponVisual.localScale = Vector3.one;
            return;
        }

        weaponVisual.localScale = new Vector3(1f, mouseLeftOfBody ? -1f : 1f, 1f);
    }

    // Point gun at mouse, flip Y when cursor is left of body.
    void LateUpdate()
    {
        if (weaponData == null || player == null || aimPlaneRoot == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 mouseWorld = AimPlaneUtil.ScreenToWorldOnPlane(cam, aimPlaneRoot.position);
        Vector2 bodyPos = aimPlaneRoot.position;
        bool mouseLeftOfBody = mouseWorld.x < bodyPos.x;

        Transform shiftTarget = positionShiftTarget != null ? positionShiftTarget : transform;
        shiftTarget.localPosition = mouseLeftOfBody
            ? _baseShiftLocalPos + localOffsetWhenAimLeft
            : _baseShiftLocalPos;

        ApplyLeftAimVisual(mouseLeftOfBody);

        if (weaponPivot != null)
        {
            Vector2 pivotPos = weaponPivot.position;
            Vector2 toMouse = (Vector2)mouseWorld - pivotPos;
            if (toMouse.sqrMagnitude > 1e-6f)
            {
                float aimAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
                weaponPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);
            }
        }

        // flipY does not move Shot Point; nudge muzzle in pivot space so spawn matches barrel art.
        if (muzzle != null)
        {
            muzzle.localPosition = _baseMuzzleLocalPos +
                                   (mouseLeftOfBody ? muzzleLocalAdjustWhenAimLeft : Vector3.zero);
        }
    }

    // One shot if allowed; starts auto reload when magazine empties.
    public bool TryShoot()
    {
        if (weaponData == null || bulletPrefab == null || muzzle == null || player == null)
            return false;

        if (_reloading)
            return false;

        if (_magAmmo <= 0)
        {
            StartReloadIfNeeded(force: true);
            return false;
        }

        float interval = 1f / Mathf.Max(0.01f, weaponData.fireRate);
        if (Time.time < _nextShotTime)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Vector3 mouseWorld = AimPlaneUtil.ScreenToWorldOnPlane(cam, aimPlaneRoot.position);
        Vector2 muzzlePos = muzzle.position;
        Vector2 toCursor = (Vector2)mouseWorld - muzzlePos;
        if (toCursor.sqrMagnitude < 1e-6f)
            toCursor = mouseWorld.x < aimPlaneRoot.position.x ? Vector2.left : Vector2.right;

        Vector2 dir = toCursor.normalized;
        float baseDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float spread = Random.Range(-weaponData.accuracy, weaponData.accuracy);
        Vector2 shootDir = (Quaternion.Euler(0f, 0f, baseDeg + spread) * Vector2.right).normalized;

        Bullet b = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        b.Initialize(shootDir, weaponData.bulletSpeed, weaponData.damage);

        _magAmmo--;
        _nextShotTime = Time.time + interval;
        RefreshAmmoUi();

        if (_magAmmo <= 0)
            StartReloadIfNeeded(force: true);

        return true;
    }

    // Player-requested reload; blocked if full or already reloading.
    public bool TryManualReload()
    {
        if (weaponData == null || _reloading)
            return false;
        if (_magAmmo >= weaponData.magazineSize)
            return false;
        if (_reserveAmmo <= 0)
            return false;

        return StartReloadIfNeeded(force: false);
    }

    // World gun sprite + optional HUD icon from WeaponData.
    void ApplyWeaponSprite()
    {
        if (weaponData == null || weaponData.weaponSprite == null)
            return;

        if (weaponSpriteRenderer != null)
            weaponSpriteRenderer.sprite = weaponData.weaponSprite;

        if (ammoHudWeaponImage != null)
            ammoHudWeaponImage.sprite = weaponData.weaponSprite;

        if (ammoHudWeaponSprite != null)
            ammoHudWeaponSprite.sprite = weaponData.weaponSprite;
    }

    // Starts reload coroutine unless already running.
    bool StartReloadIfNeeded(bool force)
    {
        if (weaponData == null || _reloading)
            return false;
        int cap = weaponData.magazineSize;
        if (!force && _magAmmo >= cap)
            return false;

        int need = cap - _magAmmo;
        if (need <= 0)
            return false;
        if (_reserveAmmo <= 0)
            return false;

        if (_reloadRoutine != null)
            StopCoroutine(_reloadRoutine);
        _reloadRoutine = StartCoroutine(ReloadRoutine());
        return true;
    }

    // Waits reloadSpeed then fills magazine.
    IEnumerator ReloadRoutine()
    {
        _reloading = true;
        RefreshAmmoUi();

        float duration = Mathf.Max(0.0001f, weaponData.reloadSpeed);
        if (reloadProgressSlider != null)
        {
            reloadProgressSlider.gameObject.SetActive(true);
            reloadProgressSlider.value = 0f;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (reloadProgressSlider != null)
                reloadProgressSlider.value = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (reloadProgressSlider != null)
        {
            reloadProgressSlider.value = 1f;
            reloadProgressSlider.gameObject.SetActive(false);
        }

        int cap = weaponData.magazineSize;
        int need = cap - _magAmmo;
        int load = Mathf.Min(need, _reserveAmmo);
        _magAmmo += load;
        _reserveAmmo -= load;
        _displayTotalAmmo -= load;

        _reloading = false;
        _reloadRoutine = null;
        RefreshAmmoUi();
    }
}
