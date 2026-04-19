using UnityEngine;
using System.Collections;
public class ExpandOnStart : MonoBehaviour
{

    [SerializeField] private Vector3 startScale;
    [SerializeField] private Vector3 endScale;
    [SerializeField] private float duration;

    void Start()
    {
        transform.localScale = startScale; 
        StartCoroutine(Expand());
    }

    IEnumerator Expand()
    {
 
        float time = 0;
        while (time < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = endScale;
    }
}
