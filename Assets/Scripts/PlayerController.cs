using System.Security.Cryptography.X509Certificates;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    #region References
    [Header("References")]
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Transform shotPoint;
    #endregion

    #region Movement
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    
    public bool canMove;
    private Vector2 moveInput;
    #endregion

    #region Jump
    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private int maxExtraJumps = 1;

    private int extraJumpsLeft;
    private bool isOnGround;
    private float coyoteCounter;
    private float jumpBufferCounter;
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
    #endregion

    #region Input
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference bombAction;
    [SerializeField] private InputActionReference interactAction;
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
    #endregion

    #region Unity Callbacks
    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        fireAction.action.Enable();
        dashAction.action.Enable();
        bombAction.action.Enable();
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += Interact;
        }

        jumpAction.action.performed += Jump;
        dashAction.action.performed += StartDash;
        bombAction.action.performed += DropBomb;
    }
    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        fireAction.action.Disable();
        dashAction.action.Disable();
        bombAction.action.Disable();
        if (interactAction != null)
        {
            interactAction.action.performed -= Interact;
            interactAction.action.Disable();
        }

        jumpAction.action.performed -= Jump;
        dashAction.action.performed -= StartDash;
        bombAction.action.performed -= DropBomb;
    }

    #endregion

    void Start()
    {
        mainCam = Camera.main;

        originalGravity = theRB.gravityScale;

        abilities = GetComponent<PlayerAbilityTracker>();

        gate = FindFirstObjectByType<GateController>();

        canMove = true;
    }


    private void Update()
    {
        mouseWorldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        float dir = mouseWorldPos.x - transform.position.x;

        mouseWorldPos = mainCam.ScreenToWorldPoint
        (
            Mouse.current.position.ReadValue()
        );

        // Aim direction
        aimDirection = (mouseWorldPos - (Vector2)transform.position).normalized;

        // Facing direction (only X matters)
        if (aimDirection.x > 0.01f)
            facingDirection = 1;
        else if (aimDirection.x < -0.01f)
            facingDirection = -1;

        // Apply scale
        transform.localScale = new Vector3(facingDirection, 1f, 1f);

        // Rotate shot point
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        shotPoint.rotation = Quaternion.Euler(0, 0, angle);

        // Shooting (fire while button is held)
        if (canMove && fireAction.action.IsPressed())
        {
            if (shotCounter <= 0f)
            {
                Fire();
                shotCounter = timeBetweenShots;
            }
        }

        if (shotCounter > 0f)
        {
            shotCounter -= Time.deltaTime;
        }
    } 

    // Update is called once per frame
    void FixedUpdate()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        // Dash recharge counter
        if (dashRechargeCounter > 0)
        {
            dashRechargeCounter -= Time.deltaTime;
        }

        // Dash system
        if (isDashing)
        {
            if (dashCounter > 0)
            {
                dashCounter -= Time.fixedDeltaTime;

                theRB.linearVelocity = new Vector2
                (
                    dashSpeed * dashDirection,
                    theRB.linearVelocity.y
                );

                // Dash after image counter
                afterImageCounter -= Time.deltaTime;
                if (afterImageCounter < 0)
                {
                    ShowAfterImage();
                }
            }
            else
            {
                isDashing = false;
                dashRechargeCounter = waitAfterDashing;
                theRB.gravityScale = originalGravity;
            }
            return;
        }

        // Normal movement (only if canMove)
        if (canMove)
        {
            float moveDir = moveInput.x;

            theRB.linearVelocity = new Vector2
            (
                moveDir * moveSpeed,
                theRB.linearVelocity.y
            );
        }
        else
        {
            theRB.linearVelocity = Vector2.zero;

            // Force idle animation when movement is disabled
            anim.SetBool("isOnGround", true);
            anim.SetFloat("speed", 0f);

            return;
        }

        //Checking if on the ground
        isOnGround = Physics2D.OverlapCircle(groundPoint.position, .2f, whatIsGround);

        if (isOnGround)
        {
            extraJumpsLeft = maxExtraJumps;
        }


        //Coyote Time counter
        if (isOnGround)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }

        //Jump buffer counter
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // Jump Buffer + Coyote Time
        if (jumpBufferCounter > 0f)
        {
            //Normal Jump (Coyote or ground)
            if (coyoteCounter > 0f)
            {
                theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }
            //Double Jump (In air)
            else if (!isOnGround && extraJumpsLeft > 0 && (abilities == null || abilities.canDoubleJump))
            {
                theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce * 0.9f);
                extraJumpsLeft--;
                anim.SetTrigger("doubleJump");
                jumpBufferCounter = 0f;
            }   
        }

        // Fall + Jump Multiplier
        if (theRB.linearVelocity.y < 0)
        {
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (theRB.linearVelocity.y > 0 && !jumpAction.action.IsPressed())
        {
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        //Anim set for "isOnGround"
        anim.SetBool("isOnGround", isOnGround);

        // Anim set for "speed"
        float animSpeed = Mathf.Abs(theRB.linearVelocity.x);
        anim.SetFloat("speed", animSpeed, 0.01f, Time.deltaTime);
    }

    //Fire shot
    void Fire()
    {
        if (!canMove)
            return;

        Vector2 shootDir = aimDirection;

        BulletController bullet =
            Instantiate(shotToFire, shotPoint.position, Quaternion.identity);

        bullet.moveDir = shootDir;

        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        anim.SetTrigger("shotFired");
    }

    //Jump buffer counter
    void Jump(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        jumpBufferCounter = jumpBufferTime;
    }

    // Interact (used for gates and other interactables)
    void Interact(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (gate != null)
            gate.HandlePlayerInteract();
    }

    // Bomb spawn
    void DropBomb(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (abilities != null && !abilities.canDropBomb)
            return;

        GameObject newBomb = Instantiate(bomb, bombPoint.position, Quaternion.identity);

        Rigidbody2D rb = newBomb.GetComponent<Rigidbody2D>();

        Vector2 throwDir = (aimDirection + Vector2.up * 0.3f).normalized;
        rb.linearVelocity = throwDir * bombThrowForce;

        if (rb != null)
        {
            rb.linearVelocity = aimDirection * bombThrowForce;
        }
    }

    // Start dash
    void StartDash(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        if (abilities != null && !abilities.canDash)
            return;

        if (isDashing || dashRechargeCounter > 0f) 
            return;

        // If there is no input dash to the facing direction
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            dashDirection = Mathf.RoundToInt(Mathf.Sign(moveInput.x));
        }
        else
        {
            dashDirection = facingDirection;
        }

        isDashing = true;
        dashCounter = dashTime;

        theRB.gravityScale = 0f;

        ShowAfterImage();
    }

    // After image
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
