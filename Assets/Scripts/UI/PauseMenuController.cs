using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Tooltip("The dimmed background + button panel, inactive by default")]
    [SerializeField] private GameObject pausePanelRoot;

    private bool isPaused;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
        TimeManager.SnapshotAndPause();
    }

    public void Resume()
    {
        isPaused = false;
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        TimeManager.Unpause();
    }

    // Wired to the "Main Menu" button's OnClick
    public void GoToMainMenu()
    {
        TimeManager.Unpause(); // reset Time.timeScale before leaving, else MainMenu opens frozen
        SceneManager.LoadScene("MainMenu");
    }

    // Wired to the "Quit" button's OnClick
    public void QuitGame()
    {
        Application.Quit();
    }
}
