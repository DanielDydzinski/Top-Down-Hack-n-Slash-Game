using UnityEngine;

// Pieces stay as children and scatter via physics in place - Rigidbody simulation doesn't require
// detaching from the parent Transform. The whole container is now pooled as one unit by
// DeathHandler/ObjectPooler, exactly like a jointed ragdoll (auto-returned after its lifetime).
// Individual pieces are still independently "crunchable" via CarcassCruncher, which deactivates
// just the one piece hit rather than the whole container - PoolInfo restores it automatically
// the next time this pooled instance is reused. Kept as an (empty) marker so existing prefab
// references don't break.
public class GibsContainer : MonoBehaviour
{
}