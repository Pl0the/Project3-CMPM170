using UnityEngine;

//prefab needs collider2D with "istrigger" checked.
public class oxygenpickup : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float refillFraction = 1f / 3f;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        var meter = other.GetComponent<oxygenmeter>();
        if (meter == null) return; //something other than player touched it

        collected = true;
        meter.AddFraction(refillFraction);
        Destroy(gameObject);
    }
}
