using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObject : MonoBehaviour {

	[SerializeField]
	private bool byTime;
	[SerializeField] private float destroyTime;
	private float timer;

	[SerializeField]
	private bool byDistance;
	[SerializeField] private float destroyDistance;
	private Vector3 startPosition;

	// OnEnable (not Start) so a pooled instance re-arms its timer every time it's reactivated -
	// Start only ever fires once per component instance, which would leave a reused instance
	// with a stale timer that's already past destroyTime and retires it almost instantly.
	void OnEnable ()
	{
		timer = 0f;
		startPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (byTime)
		{
			timer += Time.deltaTime;
			if (timer >= destroyTime)
			{
				RetireSelf();
			}
		}

		if (byDistance)
		{
			float distance = Vector3.Distance (startPosition, transform.position);
			if (distance >= destroyDistance)
			{
				RetireSelf();
			}

		}
	}

	// Some effect prefabs are the pooled root themselves; others are just one nested part of a
	// pooled effect with its own independent lifetime. Only the root should go back to the pool -
	// a nested part just deactivates, and ObjectPooler resets it on the root's next reuse.
	private void RetireSelf()
	{
		PoolInfo poolInfo = GetComponent<PoolInfo>();
		if (poolInfo != null && ObjectPooler.Instance != null)
		{
			ObjectPooler.Instance.ReturnToPool(gameObject);
		}
		else if (GetComponentInParent<PoolInfo>() != null)
		{
			gameObject.SetActive(false);
		}
		else
		{
			Destroy(gameObject);
		}
	}
}
