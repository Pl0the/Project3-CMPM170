using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//tiny helper so UnityEvents (like escapeobjective's On Escaped) can switch scenes.
//add to any object, then in the event pick sceneloader > LoadScene and type the scene name.
public class sceneloader : MonoBehaviour
{
    [Tooltip("Seconds to wait before switching, so the player sees themselves reach the goal.")]
    [SerializeField] private float delay = 0.75f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAfterDelay(sceneName));
    }

    private IEnumerator LoadAfterDelay(string sceneName)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneName);
    }
}
