using UnityEngine;


public class CameraFollow : MonoBehaviour
{


    [SerializeField]
    private Transform target;                       //target to follow
    [SerializeField]
    private float height;                           //height of the camera above the player
    [SerializeField]
    private float heightMin;                        //minimum height camera can go
    [SerializeField]
    private float heightMax;                        //maximum height the camera can go
    [SerializeField]
    private float angle;                            //the angle of the camera
    [SerializeField]
    private float offset;                           //extra distance from player to camera in z axis
    [SerializeField]
    private float offsetMin;                        //minimum distance of offset
    [SerializeField]
    private float offsetMax;                        // maximum distance of offset
    [SerializeField]
    private float damping;                          // how fast camera travels to its destination
    [SerializeField]
    private float heightDamping = 5f;               // how fast camera follows vertical (level) changes
    [Tooltip("Vertical offset added on top of the player's root position when framing them - target.position is the feet (root pivot), so without this the camera aims at the feet rather than roughly chest/head height. Barely visible zoomed out, obvious up close (e.g. the zoom override).")]
    [SerializeField] private float targetLookHeightOffset = 1.2f;

    [SerializeField] float playerToscreenEdgeLimit;     //how close to the screen the player can be

    [SerializeField] private float CameraBounderXmin;
    [SerializeField] private float cameraBounderXMax;
    [SerializeField] private float CamerabounderZMin;
    [SerializeField] private float CamerabounderZMax;

    [Header("Zoom Override (forced close zoom, e.g. mid-air attacks)")]
    [Tooltip("Offset (camera distance) forced while a zoom override is active - overrides the normal scroll-controlled offset entirely, not just its max.")]
    [SerializeField] private float zoomOverrideOffset = 3f;
    [Tooltip("Height forced while a zoom override is active.")]
    [SerializeField] private float zoomOverrideHeight = 2f;
    [Tooltip("Damping (lerp speed) used while a zoom override is active - separate from the normal damping so the forced zoom can snap in faster/slower than regular scroll-zoom does.")]
    [SerializeField] private float zoomOverrideDamping = 10f;

    private bool _isZoomOverridden;                 // set/cleared by SetZoomOverride/ClearZoomOverride (see MidFallAttackState Enter/ExitState)
    private float _overrideOffset;
    private float _overrideHeight;
    private float _overrideDamping;

    private Vector3 centre;                         //centre between the mouse and target, is where the camera will travel to
    private PlayerToMouse playerToMouse;            // holds cursor info
    private float groundHeight;                     // the player's current ground/base height, smoothed

    enum EdgeState { Top, Bottom, Right, Left, Neither };//edges of the screen
    EdgeState atEdgeVertical = EdgeState.Neither; // current state of vertical screen edges
    EdgeState atEdgeHorizontal = EdgeState.Neither;// current state of horizontal screen edges



    // Use this for initialization
    void Start()
    {
        Quaternion rot = Quaternion.Euler(new Vector3(angle, 0f, 0f)); // store the angle we want the camera to be in a quaternion
        transform.rotation = rot; //apply the rotation
        playerToMouse = target.GetComponent<PlayerToMouse>(); //get component from the target which stores cursor info
        groundHeight = target.position.y; // initialise to player's starting height
    }
    // Update is called once per frame
    void LateUpdate()
    {

        ScrollHeight(heightMin, heightMax);
        CamFollow();
    }

    private void CamFollow()
    {
        // Smoothly track the player's vertical level. Using the player's own y (which is
        // always correctly grounded by physics/controller) means we never depend on a flat
        // world or on raycasting terrain of unknown height.
        groundHeight = Mathf.Lerp(groundHeight, target.position.y, Time.deltaTime * heightDamping);

        // Flat variant (terrain-height-independent) rather than mouseInWorldPos - see PlayerToMouse.
        Vector3 cursorWorldPos = playerToMouse.mouseInWorldPosFlat;
        // Centre is horizontal only (x/z from player+cursor); vertical comes from groundHeight plus
        // targetLookHeightOffset, so moving the cursor over a canyon no longer drags the camera down,
        // and the camera frames roughly chest/head height instead of the feet-level root position.
        centre = new Vector3(
            (target.position.x + cursorWorldPos.x) / 2.0f,
            groundHeight + targetLookHeightOffset,
            (cursorWorldPos.z + target.position.z) / 2.0f);

        // While overridden, both the target (offset/height) and the transition speed (damping) are
        // swapped for the override's own values - the normal scroll-controlled offset/height keep
        // updating underneath (ScrollHeight still runs every frame) but are ignored until cleared,
        // so whatever the player had scrolled to is exactly where the camera eases back to.
        float useOffset = _isZoomOverridden ? _overrideOffset : offset;
        float useHeight = _isZoomOverridden ? _overrideHeight : height;
        float useDamping = _isZoomOverridden ? _overrideDamping : damping;

        Vector3 desiredPos = centre + new Vector3(0f, useHeight, -useOffset);
        Vector3 currentPos = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * useDamping); // lerp for the slowing-down-on-approach effect
        transform.position = currentPos; // apply so the viewport calc below uses the new position

        Vector3 targetCoords = Camera.main.WorldToViewportPoint(target.position); // player's viewport position
        bool atEdgeV = IsPlayerCloseToScreenEdgeV(targetCoords);//check if player is at vertical edge
        bool atEdgeH = IsPlayerCloseToScreenEdgeH(targetCoords);//check if player is at horizontal edge

        if (atEdgeV || atEdgeH)
        {

            // Instead of raycasting the floor (which hits whatever terrain is under the cursor
            // and returns an unreliable height), build the edge target on the SAME horizontal
            // plane as the player. We push a ray from the desired viewport edge and intersect it
            // with a flat plane at the player's height, so canyon/hill geometry is irrelevant.
            Vector3 viewPortEdge = targetCoords;
            if (atEdgeVertical == EdgeState.Top)
                viewPortEdge.y = 1.0f - playerToscreenEdgeLimit;
            if (atEdgeVertical == EdgeState.Bottom)
                viewPortEdge.y = playerToscreenEdgeLimit;
            if (atEdgeHorizontal == EdgeState.Right)
                viewPortEdge.x = 1.0f - playerToscreenEdgeLimit;
            if (atEdgeHorizontal == EdgeState.Left)
                viewPortEdge.x = playerToscreenEdgeLimit;

            Ray edgeLimitRay = Camera.main.ViewportPointToRay(viewPortEdge);
            // Mathematical plane at the player's height — no physics, no terrain dependency.
            Plane playerPlane = new Plane(Vector3.up, new Vector3(0f, target.position.y, 0f));
            float rayDist;
            if (playerPlane.Raycast(edgeLimitRay, out rayDist))
            {

                Vector3 edgeLimitWorldPos = edgeLimitRay.GetPoint(rayDist); // where the edge falls on the player's plane
                Vector3 dist = target.position - edgeLimitWorldPos; // difference between player and that screen-edge point
                dist.y = 0f; // keep it purely horizontal

                if (atEdgeHorizontal == EdgeState.Right || atEdgeHorizontal == EdgeState.Left)
                {
                    currentPos.x += dist.x; // clamp so player never crosses the screen edge horizontally
                }
                if (atEdgeVertical == EdgeState.Top || atEdgeVertical == EdgeState.Bottom)
                {
                    currentPos.z += dist.z; // clamp so player never crosses the screen edge vertically
                }
            }

            transform.position = currentPos; // update the camera's position
        } //if we are not at the screen edge make sure to reset the states
        if (!atEdgeV && atEdgeVertical != EdgeState.Neither)
        {
            atEdgeVertical = EdgeState.Neither;
        }
        if (!atEdgeH && atEdgeHorizontal != EdgeState.Neither)
        {
            atEdgeHorizontal = EdgeState.Neither;
        }


    }

    //check if target is close to the screen edge in vertical axis
    private bool IsPlayerCloseToScreenEdgeV(Vector3 playerCoords) // passing viewport position of the player
    {

        if (playerCoords.y > 1.0f - playerToscreenEdgeLimit)
        {
            atEdgeVertical = EdgeState.Top;
            return true;
        }
        else if (playerCoords.y < playerToscreenEdgeLimit)
        {
            atEdgeVertical = EdgeState.Bottom;
            return true;
        }

        return false;
    }
    //check if target is close to the screen edge in horizontal axis
    private bool IsPlayerCloseToScreenEdgeH(Vector3 playerCoords) // passing viewport position of the player
    {
        if (playerCoords.x > 1.0f - playerToscreenEdgeLimit)
        {
            atEdgeHorizontal = EdgeState.Right;
            return true;
        }
        else if (playerCoords.x < playerToscreenEdgeLimit)
        {
            atEdgeHorizontal = EdgeState.Left;
            return true;
        }
        return false;
    }

    //might not want to have this right here
    // allow scrolling the camera height
    private void ScrollHeight(float min, float max)
    {
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (height < max)
                height++;
            CamAutoOffset(offsetMin, offsetMax);
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (height > min)
                height--;
            CamAutoOffset(offsetMin, offsetMax);
        }
    }

    // Forces a close zoom for as long as it's held, rather than a brief pulse - e.g. for the whole
    // duration of a mid-air attack (see MidFallAttackState.EnterState/ExitState). CamFollow's own
    // damping lerp still smooths the transition in and out, using zoomOverrideDamping instead of the
    // normal damping while active, so it can snap in faster than regular scroll-zoom does.
    public void SetZoomOverride()
    {
        SetZoomOverride(zoomOverrideOffset, zoomOverrideHeight, zoomOverrideDamping);
    }

    public void SetZoomOverride(float offsetOverride, float heightOverride, float transitionDamping)
    {
        _isZoomOverridden = true;
        _overrideOffset = offsetOverride;
        _overrideHeight = heightOverride;
        _overrideDamping = transitionDamping;
    }

    public void ClearZoomOverride()
    {
        _isZoomOverridden = false;
    }

    //change the camera offset based on camera height
    private void CamAutoOffset(float offsetMin, float offsetMax)
    {
        float diff = heightMax - heightMin;
        float factor = diff - (heightMax - height);
        factor /= diff;

        float newOffset = offsetMax * factor;
        if (newOffset < offsetMin)
            newOffset = offsetMin;

        offset = newOffset;
    }


}