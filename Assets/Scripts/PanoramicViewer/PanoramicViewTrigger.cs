using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PanoramicViewTrigger : MonoBehaviour
{
    [Tooltip("The panoramic camera this trigger activates. Assign the child camera in this prefab.")]
    public Camera panoramicCamera;

    [Tooltip("Only objects with this tag will activate the panoramic view.")]
    public string playerTag = "Player";

    [Tooltip("If true, this trigger only fires once.")]
    public bool triggerOnce = false;

    private bool hasTriggered = false;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        if (PanoramicViewManager.Instance == null || panoramicCamera == null) return;

        hasTriggered = true;
        PanoramicViewManager.Instance.EnterPanoramicView(panoramicCamera);
    }
}
