using UnityEngine;

public class GibsContainer : MonoBehaviour
{
    [Tooltip("How long individual pieces stay on the ground before fading/deleting if not stepped on.")]
    [SerializeField] private float pieceLifespan = 10f;

    void Start()
    {
        // 1. Gather all the pieces inside this container
        int childCount = transform.childCount;
        Transform[] pieces = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            pieces[i] = transform.GetChild(i);
        }

        // 2. Set them free into the world
        foreach (Transform piece in pieces)
        {
            // Detach from the parent container completely
            piece.SetParent(null);

            // Give each piece its own individual fallback clean-up timer
            Destroy(piece.gameObject, pieceLifespan);
        }

        // 3. Destroy this empty parent container immediately since it's no longer needed
        Destroy(gameObject);
    }
}