using UnityEngine;

//red arrow that changes gravity when the player touches it.
//put this on an empty GameObject, pick a direction, move it where you want. duplicate for more arrows.
//the arrow sprite + trigger collider get made at runtime, so nothing else needs setting up.
public class gravityarrow : MonoBehaviour
{
    public enum Direction { Down, Up, Left, Right }
    public enum Mode { PointWay, Flip }

    [Tooltip("PointWay: gravity goes the way the arrow points.\nFlip: gravity reverses (arrow direction is just for looks).")]
    [SerializeField] private Mode mode = Mode.PointWay;
    [SerializeField] private Direction direction = Direction.Up;

    [Header("Look")]
    [SerializeField] private Color color = new Color(0.95f, 0.1f, 0.1f, 1f);
    [Tooltip("Arrow fades to this alpha when gravity already points its way.")]
    [Range(0f, 1f)]
    [SerializeField] private float inactiveAlpha = 0.35f;
    [SerializeField] private float size = 1f;
    [SerializeField] private int sortingOrder = 1;

    [Header("Flip mode")]
    [Tooltip("Seconds before the same arrow can flip gravity again.")]
    [SerializeField] private float flipCooldown = 0.5f;

    private SpriteRenderer sr;
    private playercontroller player;
    private float lastFlipTime = -999f;
    private static Sprite arrowSprite; //shared by every arrow

    public Vector2 DirVector => direction switch
    {
        Direction.Up => Vector2.up,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.down,
    };

    private void Awake()
    {
        if (arrowSprite == null) arrowSprite = MakeArrowSprite(64);

        //visual on a child so the root's scale/rotation stay yours to edit
        var visual = new GameObject("Arrow Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * size;
        float angle = Mathf.Atan2(DirVector.y, DirVector.x) * Mathf.Rad2Deg; //sprite points right by default
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        //trigger so the player passes through it
        var box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one * size;

        player = FindFirstObjectByType<playercontroller>();
    }

    private void Update()
    {
        if (mode != Mode.PointWay || player == null) return;
        //fade when this arrow wouldn't change anything
        var c = color;
        if (player.GravityDir == DirVector) c.a = inactiveAlpha;
        sr.color = c;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponent<playercontroller>();
        if (pc == null) return; //only the player

        if (mode == Mode.PointWay)
        {
            pc.SetGravity(DirVector);
        }
        else if (Time.time - lastFlipTime >= flipCooldown)
        {
            lastFlipTime = Time.time;
            pc.FlipGravity();
        }
    }

    //white arrow pointing right, 1 world unit wide. tinted by SpriteRenderer.color
    private static Sprite MakeArrowSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            //normalized coords: u 0..1 left->right, v -0.5..0.5 around the middle
            float u = (x + 0.5f) / res;
            float v = (y + 0.5f) / res - 0.5f;

            bool shaft = u >= 0.08f && u < 0.55f && Mathf.Abs(v) <= 0.12f;
            bool head = u >= 0.5f && u <= 0.95f && Mathf.Abs(v) <= (0.95f - u) * 0.9f; //triangle

            pixels[y * res + x] = (shaft || head) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    //shows the arrow in the editor before pressing Play
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Vector3 p = transform.position;
        Vector3 d = (Vector3)DirVector * (size * 0.45f);
        Vector3 perp = new Vector3(-d.y, d.x, 0f) * 0.6f;
        Gizmos.DrawLine(p - d, p + d);
        Gizmos.DrawLine(p + d, p + d * 0.2f + perp);
        Gizmos.DrawLine(p + d, p + d * 0.2f - perp);
        Gizmos.DrawWireCube(p, Vector3.one * size);
    }
}
