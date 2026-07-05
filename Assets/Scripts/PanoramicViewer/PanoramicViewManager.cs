using UnityEngine;

public class PanoramicViewManager : MonoBehaviour
{
    public static PanoramicViewManager Instance { get; private set; }

    [Header("Player References")]
    [Tooltip("The camera used during normal gameplay.")]
    public Camera playerCamera;

    [Tooltip("Player scripts to disable while in panoramic view (movement, mouse look, etc).")]
    public MonoBehaviour[] playerControlScripts;

    [Header("Cursor")]
    [Tooltip("If your player look script locks the cursor, enable this so it gets locked again on exit.")]
    public bool lockCursorOnExit = true;

    [Header("Exit Keys")]
    public KeyCode[] exitKeys = { KeyCode.Escape, KeyCode.Space };

    public bool IsInPanoramicView { get; private set; }

    private Camera currentPanoramicCamera;
    private PanoramicCameraLook currentLookScript;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!IsInPanoramicView) return;

        for (int i = 0; i < exitKeys.Length; i++)
        {
            if (Input.GetKeyDown(exitKeys[i]))
            {
                ExitPanoramicView();
                break;
            }
        }
    }

    public void EnterPanoramicView(Camera panoramicCamera)
    {
        if (IsInPanoramicView || panoramicCamera == null) return;

        IsInPanoramicView = true;
        currentPanoramicCamera = panoramicCamera;
        currentLookScript = panoramicCamera.GetComponent<PanoramicCameraLook>();

        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        currentPanoramicCamera.gameObject.SetActive(true);
        if (currentLookScript != null) currentLookScript.enabled = true;

        SetPlayerControlsEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitPanoramicView()
    {
        if (!IsInPanoramicView) return;

        IsInPanoramicView = false;

        if (currentLookScript != null) currentLookScript.enabled = false;
        if (currentPanoramicCamera != null) currentPanoramicCamera.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);

        SetPlayerControlsEnabled(true);

        if (lockCursorOnExit)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        currentPanoramicCamera = null;
        currentLookScript = null;
    }

    private void SetPlayerControlsEnabled(bool value)
    {
        if (playerControlScripts == null) return;
        for (int i = 0; i < playerControlScripts.Length; i++)
        {
            if (playerControlScripts[i] != null) playerControlScripts[i].enabled = value;
        }
    }
}
