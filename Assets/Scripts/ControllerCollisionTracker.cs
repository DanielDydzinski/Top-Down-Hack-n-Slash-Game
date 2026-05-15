using UnityEngine;

public class ControllerCollisionTracker : MonoBehaviour
{
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("<color=red><b>[CollisionTracker]</b> ERROR: No CharacterController found on this GameObject!</color>");
        }
        else
        {
            Debug.Log("<color=cyan><b>[CollisionTracker]</b> Active. Monitoring all CharacterController physical impacts...</color>");
        }
    }

    void Update()
    {
        // Run a continuous 3D scan every frame to catch hidden/overlapping colliders 
        // that might be warping into the player without triggering a direct hit event.
        if (controller != null)
        {
            ScanForOverlappingPhysics();
        }
    }

    // 1. Catches objects that physically smash into the Character Controller during movement
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Ignore the ground/environment walk surfaces so your console isn't flooded
        if (hit.gameObject.isStatic || hit.gameObject.name.ToLower().Contains("ground") || hit.gameObject.name.ToLower().Contains("terrain"))
            return;

        Debug.LogError($"<color=yellow><b>[CONTROLLER IMPACT!]</b></color> Player bumped by: <b>'{hit.gameObject.name}'</b>\n" +
                       $"➔ <b>Tag:</b> {hit.gameObject.tag} | <b>Layer:</b> {LayerMask.LayerToName(hit.gameObject.layer)}\n" +
                       $"➔ <b>Root Parent:</b> {hit.transform.root.name} | <b>Hit Normal:</b> {hit.normal}", hit.gameObject);
    }

    // 2. Actively searches the immediate air space inside the player's body capsule
    void ScanForOverlappingPhysics()
    {
        Vector3 center = transform.TransformPoint(controller.center);
        // Slightly expand the radius to catch things just about to clip or heavily clipping
        float radius = controller.radius + 0.05f;

        Collider[] overlappedColliders = Physics.OverlapSphere(center, radius);

        foreach (Collider col in overlappedColliders)
        {
            // Ignore ourselves, children, and static level geometry
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform) || col.gameObject.isStatic)
                continue;

            if (col.gameObject.name.ToLower().Contains("ground") || col.gameObject.name.ToLower().Contains("terrain"))
                continue;

            Debug.LogError($"<color=red><b>[OVERLAP DETECTED!]</b></color> Object is deeply inside Player space: <b>'{col.name}'</b>\n" +
                           $"➔ <b>Root Parent:</b> {col.transform.root.name} | <b>Layer:</b> {LayerMask.LayerToName(col.gameObject.layer)}", col.gameObject);
        }
    }

    // 3. Just in case the interfering object is marked as an "Is Trigger" hitbox
    void OnTriggerStay(Collider other)
    {
        if (other.transform.root == transform || other.gameObject.isStatic) return;

        Debug.LogWarning($"<color=orange><b>[TRIGGER OVERLAP]</b></color> Player is sitting inside Trigger Zone: <b>'{other.name}'</b> | Parent: {other.transform.root.name}");
    }
}