using System.Collections.Generic;
using UnityEngine;

//spawns prefabs at random spots inside this object's BoxCollider2D.
//skips any spot that overlaps a solid (non-trigger) collider, like walls or the player.
//resize box to cover map

[RequireComponent(typeof(BoxCollider2D))]
public class PickupSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [SerializeField] private GameObject[] prefabs;       // picks one at random each spawn
    [SerializeField] private int spawnCount = 3;
    [Tooltip("Hard cap on how many pickups can exist at once.")]
    [SerializeField] private int maxAlive = 3;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Placement rules")]
    [Tooltip("How much empty space a pickup needs around it. Roughly half the pickup's width.")]
    [SerializeField] private float clearanceRadius = 0.4f;
    [Tooltip("Minimum distance between spawned pickups.")]
    [SerializeField] private float minSpacing = 1f;
    [Tooltip("Which layers count as blocking. Leave as Everything unless you need otherwise.")]
    [SerializeField] private LayerMask blockingLayers = ~0;
    [Tooltip("Random spots tried per pickup before giving up.")]
    [SerializeField] private int maxAttemptsPerPickup = 30;

    private BoxCollider2D area;
    private readonly List<GameObject> spawned = new List<GameObject>();

    public IReadOnlyList<GameObject> Spawned => spawned;
    // Pickups destroy themselves when collected, so count the ones still around
    public int RemainingCount { get { spawned.RemoveAll(p => p == null); return spawned.Count; } }

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true; // the area itself should never block anything
    }

    private void Start()
    {
        if (spawnOnStart) SpawnAll();
    }

    public void SpawnAll()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning($"{name}: no prefabs assigned to PickupSpawner.", this);
            return;
        }

        int target = Mathf.Min(spawnCount, maxAlive - RemainingCount);
        int placed = 0;
        for (int i = 0; i < target; i++)
        {
            if (TrySpawnOne()) placed++;
        }

        if (placed < target)
            Debug.LogWarning($"{name}: only found room for {placed}/{target} pickups. " +
                             "Make the area bigger, or lower clearanceRadius or minSpacing.", this);
    }

    // Spawns a single pickup if we're under the cap. Returns false if capped or no room.
    public bool TrySpawnOne()
    {
        if (prefabs == null || prefabs.Length == 0) return false;
        if (RemainingCount >= maxAlive) return false;
        if (!TryFindSpot(out Vector2 spot)) return false;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        spawned.Add(Instantiate(prefab, spot, Quaternion.identity, transform));
        return true;
    }

    public void ClearAll()
    {
        foreach (var p in spawned) if (p != null) Destroy(p);
        spawned.Clear();
    }

    private bool TryFindSpot(out Vector2 spot)
    {
        Bounds b = area.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerPickup; attempt++)
        {
            spot = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));

            if (IsBlocked(spot)) continue;
            if (TooCloseToOtherPickups(spot)) continue;

            return true;
        }

        spot = default;
        return false;
    }

    private bool IsBlocked(Vector2 spot)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(spot, clearanceRadius, blockingLayers);
        foreach (var hit in hits)
        {
            if (hit == area) continue;      //ignore own spawn area
            if (hit.isTrigger) continue;    //ignore triggers (pickups, zones, etc.)
            return true;                    //collider = wall, player = blocked
        }
        return false;
    }

    private bool TooCloseToOtherPickups(Vector2 spot)
    {
        foreach (var p in spawned)
        {
            if (p == null) continue;
            if (Vector2.Distance(spot, p.transform.position) < minSpacing) return true;
        }
        return false;
    }

    //shows the spawn area in the even when object not selected
    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
