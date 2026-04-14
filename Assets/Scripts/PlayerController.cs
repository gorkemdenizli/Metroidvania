using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    #region Animator parameter names (must match Animator Controller)
    private const string AnimSpeed = "speed";
    private const string AnimYVelocity = "yVelocity";
    private const string AnimIsOnGround = "isOnGround";
    private const string AnimJumpPressed = "jumpPressed";
    private const string AnimIsDead = "isDead";
    private const string AnimShotFired  = "shotFired";
    private const string AnimDoubleJump = "doubleJump";
    #endregion

    #region References
    [Header("References")]
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Transform shotPoint;
    #endregion

    #region Movement
    [Header("Movement")]
    [Tooltip("Max horizontal speed while walking (Blend Tree ~0.5 when run is 2x walk).")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float walkSpeed = 6f;
    [Tooltip("Max horizontal speed while holding Sprint (Blend Tree 1).")]
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float groundAcceleration = 80f;
    [SerializeField] private float groundDeceleration = 70f;

    public bool canMove;
    private Vector2 moveInput;
    #endregion

    #region Jump
    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [Tooltip("Yerde değilken bu süre içinde basılan zıplama yine kabul edilir.")]
    [SerializeField] private float jumpCoyoteTime  = 0.12f;
    [Tooltip("Bu süre içinde basılan zıplama input'u, yere inince otomatik tetiklenir.")]
    [SerializeField] private float jumpBufferTime  = 0.15f;
    [Tooltip("Kaç ekstra zıplama hakkı (1 = double jump). PlayerAbilityTracker.canDoubleJump gerektirir.")]
    [SerializeField] private int   maxExtraJumps   = 1;

    private float lastGroundedTime  = -100f;
    private float jumpGraceEnd      = -100f;   // zıpladıktan sonra kısa süre ground check devre dışı
    private float jumpBufferTimer;              // geri sayım
    private int   extraJumpsLeft;
    private bool  wasOnGround;                  // landing tespiti için
    private bool  hasDoubleJumpParam;
    #endregion

    #region Dash
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float waitAfterDashing;

    private float dashRechargeCounter;
    private float dashCounter;
    private bool isDashing;
    private int dashDirection;
    #endregion

    #region Gravity
    [Header("Gravity")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    #endregion

    #region Combat
    [Header("Combat")]
    [SerializeField] private BulletController shotToFire;
    [SerializeField] private float timeBetweenShots = 0.2f;

    private float shotCounter;
    #endregion

    #region Ground Check
    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckRadius = 0.2f;
    private bool isOnGround;
    #endregion

    #region Input
    [Header("Input Actions")]
    [Tooltip("Optional: if Move/Jump references are empty, the Player map on this asset is used.")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference bombAction;
    [SerializeField] private InputActionReference interactAction;

    private InputActionMap embeddedPlayerMap;
    private InputAction embeddedMove;
    private InputAction embeddedJump;
    private InputAction embeddedSprint;
    private InputAction embeddedFire;
    private InputAction embeddedDash;
    private InputAction embeddedBomb;
    private InputAction embeddedInteract;
    private bool useEmbeddedPlayerActions;
    #endregion

    #region After Image
    [Header("Dash After Image")]
    [SerializeField] private SpriteRenderer theSR;
    [SerializeField] private SpriteRenderer afterImage;
    [SerializeField] private float afterImageLifetime;
    [SerializeField] private float timeBetweenAfterImages;
    [SerializeField] private Color afterImageColor;

    private float afterImageCounter;
    #endregion

    #region Bomb
    [Header("Bomb")]
    [SerializeField] private Transform bombPoint;
    [SerializeField] private GameObject bomb;
    [SerializeField] private float bombThrowForce = 15f;
    #endregion

    #region Group Later
    [Header("Group Later")]
    [SerializeField] public PlayerAbilityTracker abilities;
    private Camera mainCam;
    private float originalGravity;
    public Vector2 mouseWorldPos { get; private set; }
    public Vector2 aimDirection { get; private set; }
    public int facingDirection { get; private set; } // 1 = Right, -1 = Left
    private GateController gate;
    private bool hasShotFiredParam;
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        TryBindEmbeddedPlayerActions();
        CacheAnimatorOptionalParams();

        if (anim != null)
            anim.updateMode = AnimatorUpdateMode.Fixed;
    }

    void TryBindEmbeddedPlayerActions()
    {
        if (moveAction != null || inputActionAsset == null)
            return;

        embeddedPlayerMap = inputActionAsset.FindActionMap("Player", throwIfNotFound: true);
        embeddedMove = embeddedPlayerMap.FindAction("Move", throwIfNotFound: true);
        embeddedJump = embeddedPlayerMap.FindAction("Jump", throwIfNotFound: true);
        embeddedSprint = embeddedPlayerMap.FindAction("Sprint", throwIfNotFound: true);
        embeddedFire = embeddedPlayerMap.FindAction("Attack", throwIfNotFound: true);
        embeddedDash = embeddedPlayerMap.FindAction("Dash", throwIfNotFound: true);
        embeddedBomb = embeddedPlayerMap.FindAction("Bomb", throwIfNotFound: true);
        embeddedInteract = embeddedPlayerMap.FindAction("Interact", throwIfNotFound: false);
        useEmbeddedPlayerActions = true;
    }

    void OnEnable()
    {
        if (useEmbeddedPlayerActions)
        {
            embeddedPlayerMap.Enable();
            embeddedJump.performed += Jump;
            embeddedDash.performed += StartDash;
            embeddedBomb.performed += DropBomb;
            if (embeddedInteract != null)
                embeddedInteract.performed += Interact;
            return;
        }

        if (moveAction != null)
            moveAction.action.Enable();
        if (jumpAction != null)
            jumpAction.action.Enable();
        if (fireAction != null)
            fireAction.action.Enable();
        if (dashAction != null)
            dashAction.action.Enable();
        if (bombAction != null)
            bombAction.action.Enable();
        if (sprintAction != null)
            sprintAction.action.Enable();
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += Interact;
        }

        if (jumpAction != null)
            jumpAction.action.performed += Jump;
        if (dashAction != null)
            dashAction.action.performed += StartDash;
        if (bombAction != null)
            bombAction.action.performed += DropBomb;
    }

    void OnDisable()
    {
        if (useEmbeddedPlayerActions)
        {
            embeddedJump.performed -= Jump;
            embeddedDash.performed -= StartDash;
            embeddedBomb.performed -= DropBomb;
            if (embeddedInteract != null)
                embeddedInteract.performed -= Interact;
            embeddedPlayerMap.Disable();
            return;
        }

        if (moveAction != null)
            moveAction.action.Disable();
        if (jumpAction != null)
            jumpAction.action.Disable();
        if (fireAction != null)
            fireAction.action.Disable();
        if (dashAction != null)
            dashAction.action.Disable();
        if (bombAction != null)
            bombAction.action.Disable();
        if (sprintAction != null)
            sprintAction.action.Disable();
        if (interactAction != null)
        {
            interactAction.action.performed -= Interact;
            interactAction.action.Disable();
        }

        if (jumpAction != null)
            jumpAction.action.performed -= Jump;
        if (dashAction != null)
            dashAction.action.performed -= StartDash;
        if (bombAction != null)
            bombAction.action.performed -= DropBomb;
    }

    /// <summary>
    /// Scene copies of the player call <see cref="OnDisable"/> and disable the shared
    /// <see cref="InputActionReference"/> assets. Re-enable after load (next frame).
    /// Does not re-subscribe input callbacks; the persistent player still holds them.
    /// </summary>
    public void RestoreInputAfterSceneLoad()
    {
        if (useEmbeddedPlayerActions)
            embeddedPlayerMap.Enable();
        else
        {
            if (moveAction != null)
                moveAction.action.Enable();
            if (jumpAction != null)
                jumpAction.action.Enable();
            if (fireAction != null)
                fireAction.action.Enable();
            if (dashAction != null)
                dashAction.action.Enable();
            if (bombAction != null)
                bombAction.action.Enable();
            if (sprintAction != null)
                sprintAction.action.Enable();
            if (interactAction != null)
                interactAction.action.Enable();
        }

        canMove = true;
        gate = FindFirstObjectByType<GateController>();
    }

    #endregion

    void Start()
    {
        mainCam = Camera.main;
        originalGravity = theRB.gravityScale;
        abilities = GetComponent<PlayerAbilityTracker>();
        gate = FindFirstObjectByType<GateController>();
        canMove = true;
        extraJumpsLeft = maxExtraJumps;
    }

    void CacheAnimatorOptionalParams()
    {
        hasShotFiredParam  = false;
        hasDoubleJumpParam = false;
        if (anim == null)
            return;
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
            {
                if (p.name == AnimShotFired)  hasShotFiredParam  = true;
                if (p.name == AnimDoubleJump) hasDoubleJumpParam = true;
            }
        }
    }

    private void Update()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        moveInput = ReadMoveInput();

        if (mainCam != null)
        {
            if (Mouse.current != null)
            {
                mouseWorldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
            else
            {
                mouseWorldPos = (Vector2)transform.position + Vector2.right * facingDirection;
            }

            aimDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        }
        else
        {
            aimDirection = Vector2.right * facingDirection;
        }

        if (aimDirection.x > 0.01f)
            facingDirection = 1;
        else if (aimDirection.x < -0.01f)
            facingDirection = -1;

        transform.localScale = new Vector3(facingDirection, 1f, 1f);

        if (shotPoint != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            shotPoint.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (canMove && IsFirePressed())
        {
            if (shotCounter <= 0f)
            {
                Fire();
                shotCounter = timeBetweenShots;
            }
        }

        if (shotCounter > 0f)
            shotCounter -= Time.deltaTime;

        // Zıpladıktan kısa bir süre boyunca ground check'i atla — aksi hâlde
        // aynı frame'de hâlâ "yerde" görünüp Animator'a yanlış sinyal gider.
        isOnGround = groundPoint != null
                     && Time.time >= jumpGraceEnd
                     && Physics2D.OverlapCircle(groundPoint.position, groundCheckRadius, whatIsGround);

        if (isOnGround)
        {
            lastGroundedTime = Time.time;
            extraJumpsLeft   = maxExtraJumps;
        }

        // Landing tespiti: yeni indi → stale jumpPressed trigger'ını temizle
        if (isOnGround && !wasOnGround && anim != null)
            anim.ResetTrigger(AnimJumpPressed);
        wasOnGround = isOnGround;

        // Jump buffer geri sayımı
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
            TryConsumeJumpBuffer();
        }
    }

    private void LateUpdate()
    {
        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        if (anim == null)
            return;

        float absVx = Mathf.Abs(theRB.linearVelocity.x);
        bool wantsRun = IsSprintHeld();
        float speedForBlend = canMove ? ComputeLocomotionBlendSpeed(absVx, wantsRun) : 0f;

        anim.SetFloat(AnimSpeed, speedForBlend);
        anim.SetFloat(AnimYVelocity, theRB.linearVelocity.y);
        anim.SetBool(AnimIsOnGround, isOnGround);

        bool dead = PlayerHealthController.instance != null &&
                    PlayerHealthController.instance.currentHealth <= 0;
        anim.SetBool(AnimIsDead, dead);
    }

    /// <summary>
    /// Locomotion blend tree uses thresholds 0 = Idle, 0.4 = Walk, 1 = Run.
    /// </summary>
    private float ComputeLocomotionBlendSpeed(float absVx, bool wantsRun)
    {
        float runCap = Mathf.Max(0.001f, runSpeed);
        float walkCap = Mathf.Max(0.001f, walkSpeed);

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            if (wantsRun)
                return Mathf.Clamp01(absVx / runCap);
            return Mathf.Clamp01(absVx / walkCap) * 0.4f;
        }

        return Mathf.Clamp01(absVx / runCap);
    }

    private bool IsSprintHeld()
    {
        if (sprintAction != null)
            return sprintAction.action.IsPressed();
        if (useEmbeddedPlayerActions && embeddedSprint != null)
            return embeddedSprint.IsPressed();
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }

    private Vector2 ReadMoveInput()
    {
        if (useEmbeddedPlayerActions && embeddedMove != null)
            return embeddedMove.ReadValue<Vector2>();
        if (moveAction != null)
            return moveAction.action.ReadValue<Vector2>();
        return Vector2.zero;
    }

    private bool IsFirePressed()
    {
        if (useEmbeddedPlayerActions && embeddedFire != null)
            return embeddedFire.IsPressed();
        return fireAction != null && fireAction.action.IsPressed();
    }

    private bool IsJumpHeld()
    {
        if (useEmbeddedPlayerActions && embeddedJump != null)
            return embeddedJump.IsPressed();
        return jumpAction != null && jumpAction.action.IsPressed();
    }

    void FixedUpdate()
    {
        if (dashRechargeCounter > 0)
            dashRechargeCounter -= Time.fixedDeltaTime;

        if (isDashing)
        {
            if (dashCounter > 0)
            {
                dashCounter -= Time.fixedDeltaTime;

                theRB.linearVelocity = new Vector2(
                    dashSpeed * dashDirection,
                    theRB.linearVelocity.y);

                afterImageCounter -= Time.fixedDeltaTime;
                if (afterImageCounter < 0)
                    ShowAfterImage();
            }
            else
            {
                isDashing = false;
                dashRechargeCounter = waitAfterDashing;
                theRB.gravityScale = originalGravity;
            }
            return;
        }

        if (!canMove)
        {
            theRB.linearVelocity = Vector2.zero;
            return;
        }

        bool wantsRun = IsSprintHeld();
        float targetMax = wantsRun ? runSpeed : walkSpeed;
        float targetVx = moveInput.x * targetMax;
        float accel = Mathf.Abs(moveInput.x) > 0.01f ? groundAcceleration : groundDeceleration;
        float newVx = Mathf.MoveTowards(theRB.linearVelocity.x, targetVx, accel * Time.fixedDeltaTime);
        theRB.linearVelocity = new Vector2(newVx, theRB.linearVelocity.y);

        if (theRB.linearVelocity.y < 0)
        {
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (theRB.linearVelocity.y > 0f && !IsJumpHeld())
        {
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    void Fire()
    {
        if (!canMove || shotToFire == null || shotPoint == null)
            return;

        Vector2 shootDir = aimDirection;

        BulletController bullet =
            Instantiate(shotToFire, shotPoint.position, Quaternion.identity);

        bullet.moveDir = shootDir;

        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        if (hasShotFiredParam)
            anim.SetTrigger(AnimShotFired);
    }

    void Jump(InputAction.CallbackContext context)
    {
        if (!canMove) return;

        jumpBufferTimer = jumpBufferTime;
        TryConsumeJumpBuffer();
    }

    void TryConsumeJumpBuffer()
    {
        if (jumpBufferTimer <= 0f || !canMove || groundPoint == null) return;

        bool canGroundJump = isOnGround || (Time.time - lastGroundedTime <= jumpCoyoteTime);

        if (canGroundJump)
        {
            ApplyJump(jumpForce, isDoubleJump: false);
            return;
        }

        bool canDouble = extraJumpsLeft > 0
                         && (abilities == null || abilities.canDoubleJump);
        if (canDouble)
            ApplyJump(jumpForce * 0.85f, isDoubleJump: true);
    }

    void ApplyJump(float force, bool isDoubleJump)
    {
        theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, force);
        jumpBufferTimer  = 0f;
        lastGroundedTime = -100f;
        jumpGraceEnd     = Time.time + 0.12f;

        if (anim == null) return;

        if (isDoubleJump)
        {
            extraJumpsLeft--;
            if (hasDoubleJumpParam)
                anim.SetTrigger(AnimDoubleJump);
            else
                anim.SetTrigger(AnimJumpPressed);
        }
        else
        {
            anim.SetTrigger(AnimJumpPressed);
        }
    }

    void Interact(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (gate != null)
            gate.HandlePlayerInteract();
    }

    void DropBomb(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (abilities != null && !abilities.canDropBomb)
            return;

        GameObject newBomb = Instantiate(bomb, bombPoint.position, Quaternion.identity);

        Rigidbody2D rb = newBomb.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = aimDirection * bombThrowForce;
    }

    void StartDash(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (abilities != null && !abilities.canDash)
            return;

        if (isDashing || dashRechargeCounter > 0f)
            return;

        if (Mathf.Abs(moveInput.x) > 0.1f)
            dashDirection = Mathf.RoundToInt(Mathf.Sign(moveInput.x));
        else
            dashDirection = facingDirection;

        isDashing = true;
        dashCounter = dashTime;

        theRB.gravityScale = 0f;

        ShowAfterImage();
    }

    void ShowAfterImage()
    {
        SpriteRenderer image = Instantiate(afterImage, transform.position, transform.rotation);
        image.sprite = theSR.sprite;
        image.transform.localScale = transform.localScale;
        image.color = afterImageColor;

        Destroy(image.gameObject, afterImageLifetime);

        afterImageCounter = timeBetweenAfterImages;
    }
}
