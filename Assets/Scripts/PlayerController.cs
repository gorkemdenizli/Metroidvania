using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private bool isOnGround;
    private float coyoteCounter;
    private float jumpBufferCounter;
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
    #endregion

    #region Unity Callbacks
    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        fireAction.action.Enable();

        jumpAction.action.performed += Jump;
        fireAction.action.performed += Fire;
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        fireAction.action.Disable();

        jumpAction.action.performed -= Jump;
        fireAction.action.performed -= Fire;
    }
    #endregion

    // Update is called once per frame
    void FixedUpdate()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        //Move sideways
        theRB.linearVelocity = new Vector2(moveInput.x * moveSpeed, theRB.linearVelocity.y);

        //Handle direction change
        if(theRB.linearVelocity.x < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (theRB.linearVelocity.x > 0)
        {
            transform.localScale = Vector3.one;
        }

        //Checking if on the ground
        isOnGround = Physics2D.OverlapCircle(groundPoint.position, .2f, whatIsGround);

        //Coyote Time counter
        if (isOnGround)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }

        // Jump Buffer + Coyote Time
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
        else
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
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


        anim.SetBool("isOnGround", isOnGround);
        anim.SetFloat("speed", Mathf.Abs(theRB.linearVelocity.x));
    }

    //Fire shot
    void Fire(InputAction.CallbackContext context)
    {
        Instantiate(shotToFire, shotPoint.position, shotPoint.rotation).moveDir = new Vector2(transform.localScale.x, 0f);
    }

    //Jump buffer counter
    void Jump(InputAction.CallbackContext context)
    {
        jumpBufferCounter = jumpBufferTime;
    }
}
