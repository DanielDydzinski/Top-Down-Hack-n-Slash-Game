using UnityEngine;

/// <summary>
/// Attach one instance to your Player         → Slot = Player
/// Attach one instance to mouseWorldTransform → Slot = Mouse
///
/// Each writes to its own shader global so they never overwrite each other.
/// The Player slot also writes _PlayerWorldY so the shader can protect floors.
/// The Mouse slot can optionally only activate when its position is behind an
/// object that is already fading the player's view.
/// </summary>
public class DitherFadeCircleMask : MonoBehaviour
{
    public enum CircleSlot { Player, Mouse }

    [Header("Slot")]
    [Tooltip("Player → _DitherCircle\nMouse  → _DitherCircle2")]
    public CircleSlot slot = CircleSlot.Player;

    [Header("Circle")]
    public float circleRadius = 180f;
    public float featherWidth = 60f;

    [Header("Mouse-slot only: obstruction gate")]
    [Tooltip("When true the mouse circle only activates when the cursor is pointing\n" +
             "at an object that is already fading the player view.\n" +
             "Has no effect when Slot = Player.")]
    public bool onlyWhenObstructing = true;

    [Tooltip("LayerMask for the obstruction raycast — match the Camera Fader's Fade Layer.")]
    public LayerMask obstructionLayer;

    [Header("Floor protection (Player slot only)")]
    [Tooltip("Buffer below player feet Y before the floor cutoff kicks in.\n" +
             "Raise if thin floors still flicker.")]
    public float floorFadeOffset = 0.1f;

    [Header("References")]
    public Camera targetCamera;

    // ── Shader global IDs ─────────────────────────────────────────────────
    private static readonly int IDCircle1 = Shader.PropertyToID("_DitherCircle");
    private static readonly int IDCircle2 = Shader.PropertyToID("_DitherCircle2");
    private static readonly int IDPlayerWorldY = Shader.PropertyToID("_PlayerWorldY");
    private static readonly int IDFloorFadeOffset = Shader.PropertyToID("_FloorFadeOffset");

    private int ActiveCircleID => slot == CircleSlot.Player ? IDCircle1 : IDCircle2;
    private Vector4 OffScreen => new Vector4(-99999f, -99999f, circleRadius, featherWidth);

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (slot == CircleSlot.Player) WriteFloorGlobals();
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        if (slot == CircleSlot.Player) UpdatePlayerSlot();
        else UpdateMouseSlot();
    }

    // ── Player slot ───────────────────────────────────────────────────────
    private void UpdatePlayerSlot()
    {
        Vector3 sp = targetCamera.WorldToScreenPoint(transform.position);

        Shader.SetGlobalVector(ActiveCircleID, sp.z < 0f
            ? OffScreen
            : new Vector4(sp.x, sp.y, circleRadius, featherWidth));

        WriteFloorGlobals();
    }

    private void WriteFloorGlobals()
    {
        Shader.SetGlobalFloat(IDPlayerWorldY, transform.position.y);
        Shader.SetGlobalFloat(IDFloorFadeOffset, floorFadeOffset);
    }

    // ── Mouse slot ────────────────────────────────────────────────────────
    private void UpdateMouseSlot()
    {
        if (onlyWhenObstructing && !IsPositionObstructed())
        {
            Shader.SetGlobalVector(ActiveCircleID, OffScreen);
            return;
        }

        Vector3 sp = targetCamera.WorldToScreenPoint(transform.position);

        Shader.SetGlobalVector(ActiveCircleID, sp.z < 0f
            ? OffScreen
            : new Vector4(sp.x, sp.y, circleRadius, featherWidth));
    }

    /// <summary>
    /// Raycast from camera toward this world position.
    /// Returns true only if the first hit object is currently being faded
    /// (has a FadeableObject that is not fully opaque).
    /// </summary>
    private bool IsPositionObstructed()
    {
        Vector3 camPos = targetCamera.transform.position;
        Vector3 dir = transform.position - camPos;
        float dist = dir.magnitude;

        if (dist < 0.01f) return false;

        if (Physics.Raycast(camPos, dir.normalized, out RaycastHit hit, dist, obstructionLayer))
        {
            var fader = hit.collider.GetComponentInParent<CameraObjectFader.FadeableObject>();
            return fader != null && !fader.IsFullyOpaque;
        }

        return false;
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(ActiveCircleID, OffScreen);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 sp = targetCamera.WorldToScreenPoint(transform.position);
        if (sp.z <= 0f) return;

        float tanHalfFov = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float pxToWorld  = sp.z / targetCamera.pixelHeight * 2f * tanHalfFov;

        Gizmos.color = slot == CircleSlot.Player
            ? new Color(0f, 1f, 1f, 0.55f)
            : new Color(1f, 0.5f, 0f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, circleRadius * pxToWorld);

        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
        Gizmos.DrawWireSphere(transform.position, (circleRadius + featherWidth) * pxToWorld);

        // Yellow floor-cutoff plane (Player slot only)
        if (slot == CircleSlot.Player)
        {
            float cutY = transform.position.y - floorFadeOffset;
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, cutY, transform.position.z),
                new Vector3(4f, 0.01f, 4f));
        }
    }
#endif
}
