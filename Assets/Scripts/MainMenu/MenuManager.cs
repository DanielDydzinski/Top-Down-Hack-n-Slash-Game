using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Singleton Instance
    public static MenuManager Instance { get; private set; }

    [Header("Animation Settings")]
    [Tooltip("Reference to the Animator component used for the menu character or camera.")]
    public Animator characterAnimator;

    [Header("Scene Settings")]
    [Tooltip("The time in seconds to wait before loading the new scene.")]
    [SerializeField] private float sceneLoadDelay = 1.0f;

    private void Awake()
    {
        // Enforce Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keeps manager alive between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Public function to hook up directly to your Play Button UI Event.
    /// </summary>
    public void PlayGame()
    {
        StartCoroutine(LoadSceneWithDelay("AssigmentMain", sceneLoadDelay));
    }

    /// <summary>
    /// Coroutine that handles the delayed scene transition.
    /// </summary>
    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Triggers the 'StandUp' animation. Can be called alongside PlayGame or independently.
    /// </summary>
    public void TriggerStandUp()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("StandUp");
        }
        else
        {
            Debug.LogWarning("MenuManager: Animator reference is missing!");
        }
    }
}
