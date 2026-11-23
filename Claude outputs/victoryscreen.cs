using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

//put this on an empty GameObject in the Victory scene.
//builds the "VICTORY" text + Restart button at runtime, nothing else to set up.
public class victoryscreen : MonoBehaviour
{
    [Tooltip("Scene to load when Restart is pressed. Must be in Build Profiles > Scene List.")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Look")]
    [SerializeField] private string title = "VICTORY";
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.6f, 0.2f, 0.9f, 1f);

    private void Start()
    {
        Time.timeScale = 1f; //just in case the game paused itself
        EnsureEventSystem();

        //canvas that scales with screen size
        var canvasGO = new GameObject("Victory Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        //full-screen background
        var bg = MakeUI("Background", canvasGO.transform);
        Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = backgroundColor;

        //title
        var titleRT = MakeUI("Title", canvasGO.transform);
        titleRT.anchoredPosition = new Vector2(0, 120);
        titleRT.sizeDelta = new Vector2(1200, 250);
        var titleText = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 180;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = titleColor;

        //restart button
        var btnRT = MakeUI("Restart Button", canvasGO.transform);
        btnRT.anchoredPosition = new Vector2(0, -140);
        btnRT.sizeDelta = new Vector2(420, 110);
        var btnImage = btnRT.gameObject.AddComponent<Image>();
        btnImage.color = buttonColor;
        var button = btnRT.gameObject.AddComponent<Button>();
        button.targetGraphic = btnImage;
        button.onClick.AddListener(Restart);

        var labelRT = MakeUI("Label", btnRT);
        Stretch(labelRT);
        var label = labelRT.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = "Restart";
        label.fontSize = 60;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        //so Enter/Space works without clicking first
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void Update()
    {
        //R also restarts
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
#else
        if (Input.GetKeyDown(KeyCode.R)) Restart();
#endif
    }

    public void Restart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private static RectTransform MakeUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt; //anchored to center by default
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    //buttons need an EventSystem to be clickable
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
