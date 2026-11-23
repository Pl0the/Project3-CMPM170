using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

//side-view platformer controller with gravity that can point any of 4 directions
//(Major Havoc style run + jump). red arrows call SetGravity() to change it.
//needs rb + a collider
[RequireComponent(typeof(Rigidbody2D))]
public class playercontroller : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Gravity")]
    [SerializeField] private float gravityStrength = 30f;      //how fast you speed up while falling
    [SerializeField] private float maxFallSpeed = 15f;
    [SerializeField] private Vector2 startGravity = Vector2.down;
    [Tooltip("Turn the player so their feet point toward gravity.")]
    [SerializeField] private bool rotateToGravity = true;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 11f;
    [Tooltip("Let go of jump early for a shorter hop (upward speed gets multiplied by this).")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [Tooltip("Can still jump this many seconds after walking off a ledge.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Pressing jump this many seconds before landing still counts.")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.05f;

    [HideInInspector] public bool canMove = true; //oxy flips so player "dies"

    public Vector2 GravityDir { get; private set; }
    public bool IsGrounded { get; private set; }

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;          //-1..1 along the "floor"
    private bool jumpHeld;
    private bool jumpCutRequested;
    private bool prevJumpHeld;
    private float lastGroundedTime = -999f;
    private float lastJumpPressTime = -999f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private ContactFilter2D groundFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;                 //we do gravity ourselves so it can point any way
        rb.freezeRotation = true;             //no spin on bump
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; //no tunneling
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        groundFilter = new ContactFilter2D { useTriggers = false }; //arrows/pickups aren't ground

        SetGravity(startGravity);
    }

    //called by gravityarrow. snaps to the nearest of up/down/left/right.
    public void SetGravity(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        GravityDir = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));

        if (rotateToGravity)
        {
            //feet point toward gravity. gravity down = 0 degrees.
            float angle = Mathf.Atan2(GravityDir.y, GravityDir.x) * Mathf.Rad2Deg + 90f;
            rb.rotation = angle;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void FlipGravity() => SetGravity(-GravityDir);

    private void Update()
    {
        bool left = false, right = false, up = false, down = false;
        bool jumpDown = false;
        jumpHeld = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            left  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            down  = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            up    = kb.wKey.isPressed || kb.upArrowKey.isPressed;
            jumpHeld = kb.spaceKey.isPressed;
            jumpDown = kb.spaceKey.wasPressedThisFrame;

            //the key pointing away from gravity also jumps (W when gravity is down, S when it's up, etc.)
            var awayKey = AwayFromGravityKeys(kb);
            jumpHeld |= awayKey.held;
            jumpDown |= awayKey.pressed;
        }
#else
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        left = h < 0f; right = h > 0f; down = v < 0f; up = v > 0f;
        jumpHeld = Input.GetButton("Jump");
        jumpDown = Input.GetButtonDown("Jump");
#endif

        //walking uses the screen axis that runs along the current floor:
        //gravity up/down -> A/D move you, gravity left/right -> W/S move you
        if (GravityDir.y != 0f) moveInput = (right ? 1f : 0f) - (left ? 1f : 0f);
        else                    moveInput = (up ? 1f : 0f) - (down ? 1f : 0f);

        if (!canMove) { moveInput = 0f; jumpHeld = false; jumpDown = false; }

        if (jumpDown) lastJumpPressTime = Time.time;
        if (prevJumpHeld && !jumpHeld) jumpCutRequested = true; //let go of jump
        prevJumpHeld = jumpHeld;
    }

#if ENABLE_INPUT_SYSTEM
    private (bool held, bool pressed) AwayFromGravityKeys(Keyboard kb)
    {
        if (GravityDir == Vector2.down)  return (kb.wKey.isPressed || kb.upArrowKey.isPressed,    kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
        if (GravityDir == Vector2.up)    return (kb.sKey.isPressed || kb.downArrowKey.isPressed,  kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame);
        if (GravityDir == Vector2.left)  return (kb.dKey.isPressed || kb.rightArrowKey.isPressed, kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame);
        return                                  (kb.aKey.isPressed || kb.leftArrowKey.isPressed,  kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame);
    }
#endif

    private void FixedUpdate()
    {
        Vector2 g = GravityDir;
        //screen-space axis along the floor (x when gravity is vertical, y when horizontal)
        Vector2 side = g.y != 0f ? Vector2.right : Vector2.up;

        IsGrounded = col != null && col.Cast(g, groundFilter, groundHits, groundCheckDistance) > 0;
        if (IsGrounded) lastGroundedTime = Time.time;

        //split current velocity into "falling" and "walking" parts
        float fall = Vector2.Dot(rb.linearVelocity, g); //+ = moving toward gravity

        //gravity
        fall = Mathf.Min(fall + gravityStrength * Time.fixedDeltaTime, maxFallSpeed);

        //jump (with coyote time + buffer so it feels forgiving)
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            fall = -jumpSpeed;
            lastJumpPressTime = -999f;
            lastGroundedTime = -999f;
        }

        //let go early = shorter jump
        if (jumpCutRequested)
        {
            if (fall < 0f) fall *= jumpCutMultiplier;
            jumpCutRequested = false;
        }

        rb.linearVelocity = g * fall + side * (moveInput * moveSpeed);
    }

    //draws the gravity direction in the scene view while playing
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + GravityDir);
    }
}
