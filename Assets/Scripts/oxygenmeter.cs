using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Put this on the Player. Drains oxygen over time and shows it on a UI Slider.
// When it hits zero, the player stops moving.
public class oxygenmeter : MonoBehaviour
{
    [SerializeField] private float maxOxygen = 30f;       //30 = 30 seconds of air when full
    [SerializeField] private float drainPerSecond = 1f;
    [SerializeField] private Slider oxygenBar;            //UI Slider here
    [SerializeField] private Vector3 oxygenOffset = new Vector3(0f, 0.8f, 0f);

    [Tooltip("Hook up a game-over screen, sound, etc. here.")]
    public UnityEvent onOxygenDepleted;

    public float Current { get; private set; }
    public float Max => maxOxygen;
    public bool IsEmpty => Current <= 0f;

    private playercontroller player;

    private void Awake()
    {
        player = GetComponent<playercontroller>();
        Current = maxOxygen;

        if (oxygenBar != null)
        {
            oxygenBar.minValue = 0f;
            oxygenBar.maxValue = maxOxygen;
            oxygenBar.interactable = false; //clicking it doesn't change the value
        }
        UpdateBar();
    }

    private void Update()
    {
        if (IsEmpty) return;

        drainPerSecond += 0.2f * Time.deltaTime;

        Current = Mathf.Max(0f, Current - drainPerSecond * Time.deltaTime);
        UpdateBar();

        if (IsEmpty)
        {
            if (player != null) player.canMove = false;
            onOxygenDepleted?.Invoke();
        }
    }

    //adds a fraction of the max tank (0.333 = one third).
    public void AddFraction(float fraction)
    {
        if (IsEmpty) return; //dead, no revives
        Current = Mathf.Min(maxOxygen, Current + maxOxygen * fraction);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (oxygenBar != null) oxygenBar.value = Current;
    }

        private void LateUpdate()
    {
        if (oxygenBar != null && Camera.main != null)
        {
            oxygenBar.transform.position =
                Camera.main.WorldToScreenPoint(
                    transform.position + oxygenOffset
                );
        }
    }
}
