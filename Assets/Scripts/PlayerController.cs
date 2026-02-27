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

    private Camera mainCam;
    private float originalGravity;

    public Vector2 mouseWorldPos { get; private set; }
    public Vector2 aimDirection { get; private set; }
    public int facingDirection { get; private set; } // 1 = Right, -1 = Left

    [SerializeField] private Transform bombPoint;
    [SerializeField] private GameObject bomb;


    #region Unity Callbacks
    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        fireAction.action.Enable();
        dashAction.action.Enable();

        jumpAction.action.performed += Jump;
        fireAction.action.performed += Fire;
        dashAction.action.performed += StartDash;
    }
    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        fireAction.action.Disable();
        dashAction.action.Disable();

        jumpAction.action.performed -= Jump;
        fireAction.action.performed -= Fire;
        dashAction.action.performed -= StartDash;
    }

    #endregion

    void Start()
    {
        mainCam = Camera.main;

        originalGravity = theRB.gravityScale;
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

        // Normal movement
        float moveDir = moveInput.x;

        theRB.linearVelocity = new Vector2
        (
            moveDir * moveSpeed,
            theRB.linearVelocity.y
        );

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
            else if (!isOnGround && extraJumpsLeft > 0)
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

        //float moveRelativeToFacing = moveDir * facingDirection;

        //Anim set for "isOnGround"
        anim.SetBool("isOnGround", isOnGround);

        // Anim set for "speed"
        float animSpeed = Mathf.Abs(theRB.linearVelocity.x);
        anim.SetFloat("speed", animSpeed, 0.01f, Time.deltaTime);

        // anim.SetFloat("moveDir", moveRelativeToFacing);
    }

    //Fire shot
    void Fire(InputAction.CallbackContext context)
    {
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
        jumpBufferCounter = jumpBufferTime;
    }

    // Start dash
    void StartDash(InputAction.CallbackContext context)
    {
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
