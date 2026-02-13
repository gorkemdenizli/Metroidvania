using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D theRB;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;

    private bool isOnGround;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask whatIsGround;

    private Vector2 moveInput;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [SerializeField] private Animator anim;

    public BulletController shotToFire;
    public Transform shotPoint;
    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();

        jumpAction.action.performed += Jump;
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();

        jumpAction.action.performed -= Jump;
    }

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

        anim.SetBool("isOnGround", isOnGround);
        anim.SetFloat("speed", Mathf.Abs(theRB.linearVelocity.x));

        if (Input.GetButtonDown("Fire1"))
        {
            Instantiate(shotToFire, shotPoint.position, shotPoint.rotation);
        }
    }

    //Jumping
    void Jump(InputAction.CallbackContext context)
    {
        if (isOnGround)
        {
            theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
        }
    }
}
