using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

//needs rb
[RequireComponent(typeof(Rigidbody2D))]
public class playercontroller : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
 
    [HideInInspector] public bool canMove = true; //oxy flips so player "dies
 
    private Rigidbody2D rb;
    private Vector2 input;
 
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;                 //top down: no falling
        rb.freezeRotation = true;             //no spin on bump
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; //no tunneling
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
 
    private void Update()
    {
        float x = 0f, y = 0f;
 
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
#endif
 
        //normalize input vector
        input = new Vector2(x, y).normalized;
    }
 
    private void FixedUpdate()
    {
        if (!canMove) input = Vector2.zero;
        rb.linearVelocity = input * moveSpeed;
    }
}