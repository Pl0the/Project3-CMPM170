using UnityEngine;
using UnityEngine.Events;

public class escapeobjective : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Player here. If left empty, it finds the object with playercontroller.")]
    [SerializeField] private playercontroller player;

    [Header("Purple goal (step 1)")]
    [SerializeField] private Vector2 goalPosition = new Vector2(5f, -3.6f); //inside bottom-right box
    [SerializeField] private Color goalColor = new Color(0.6f, 0.2f, 0.9f, 1f);

    [Header("Red escape spot (step 2)")]
    [Tooltip("If true, the red circle goes wherever the player started.")]
    [SerializeField] private bool useplayerStartAsSpawn = true;
    [SerializeField] private Vector2 spawnPosition = new Vector2(-6.5f, 4f);
    [SerializeField] private Color escapeColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    [Header("Circle settings")]
    [SerializeField] private float circleDiameter = 1f;
    [Tooltip("How close (center to center) the player needs to be to count as touching.")]
    [SerializeField] private float touchRadius = 0.75f;
    [SerializeField] private int sortingOrder = 1; //draw above the background

    [Header("On escape")]
    [Tooltip("Stop the oxygen drain once the player escapes so they can't die after winning.")]
    [SerializeField] private bool stopOxygenOnEscape = true;

    [Tooltip("Hook up a sound, UI text, etc. for when the escape sequence starts.")]
    public UnityEvent onEscapeActivated;
    [Tooltip("Hook up a win screen, sound, etc. for when the player reaches the red circle.")]
    public UnityEvent onEscaped;

    private enum Stage { GoToGoal, ReturnToSpawn, Escaped }
    private Stage stage = Stage.GoToGoal;

    private GameObject goalCircle;
    private GameObject escapeCircle;

    public bool EscapeActive => stage == Stage.ReturnToSpawn;
    public bool HasEscaped => stage == Stage.Escaped;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<playercontroller>();
        if (player == null)
        {
            Debug.LogWarning($"{name}: no playercontroller found in the scene.", this);
            enabled = false;
            return;
        }

        if (useplayerStartAsSpawn) spawnPosition = player.transform.position;

        Sprite circle = MakeCircleSprite(64);
        goalCircle = MakeCircle("Goal (Purple)", circle, goalPosition, goalColor);
        escapeCircle = MakeCircle("Escape (Red)", circle, spawnPosition, escapeColor);
        escapeCircle.SetActive(false); //hidden until step 1 is done
    }

    private void Update()
    {
        Vector2 p = player.transform.position;

        switch (stage)
        {
            case Stage.GoToGoal:
                if (Vector2.Distance(p, goalPosition) <= touchRadius)
                {
                    stage = Stage.ReturnToSpawn;
                    goalCircle.SetActive(false);
                    escapeCircle.SetActive(true);
                    Debug.Log("Escape sequence activated! Return to spawn.");
                    onEscapeActivated?.Invoke();
                }
                break;

            case Stage.ReturnToSpawn:
                if (Vector2.Distance(p, spawnPosition) <= touchRadius)
                {
                    stage = Stage.Escaped;
                    player.canMove = false;
                    if (stopOxygenOnEscape)
                    {
                        var meter = player.GetComponent<oxygenmeter>();
                        if (meter != null) meter.enabled = false;
                    }
                    Debug.Log("Player escaped!");
                    onEscaped?.Invoke();
                }
                break;
        }
    }

    private GameObject MakeCircle(string objName, Sprite sprite, Vector2 pos, Color color)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * circleDiameter;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    //builds a white circle sprite 1 world unit wide, tinted by SpriteRenderer.color
    private static Sprite MakeCircleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = res / 2f;
        var pixels = new Color32[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            byte a = (byte)(Mathf.Clamp01(r - d) * 255); //soft 1px edge
            pixels[y * res + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    //shows both spots in the editor so you can position them without pressing Play
    private void OnDrawGizmos()
    {
        Gizmos.color = goalColor;
        Gizmos.DrawWireSphere(goalPosition, circleDiameter / 2f);
        Gizmos.color = escapeColor;
        Vector2 s = spawnPosition;
        if (useplayerStartAsSpawn && !Application.isPlaying)
        {
            var pc = FindFirstObjectByType<playercontroller>();
            if (pc != null) s = pc.transform.position;
        }
        Gizmos.DrawWireSphere(s, circleDiameter / 2f);
    }
}
