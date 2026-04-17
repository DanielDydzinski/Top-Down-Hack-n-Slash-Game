//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.AI;

//public class EnemyAI : MonoBehaviour {

//	[SerializeField]
//	private Transform target;

//	[SerializeField]
//	private float patrolRate;
//	[SerializeField]
//	private float patrolRateVariance;
//	[SerializeField]
//	private float patrolDistance;
//	[SerializeField]
//	private float patrolDistanceVariance;
//	private bool findPatrolPoint;
//	private float patrolTime;
//	private float randPatrolTime;

//	[SerializeField]
//	private float engagedDistance;

//	 private Animator aiAnim;

//	private NavMeshAgent nav;

//	private AbilityManager abilityManager;

//    private Health hp;

//	private float rotationSpeed;

//	// Use this for initialization
//	void Start () {

//		findPatrolPoint = true;
//		patrolTime = 0.0f;
//		randPatrolTime = Random.Range (patrolRate - patrolRateVariance, patrolRate + patrolRateVariance);

//		target = GameObject.FindGameObjectWithTag ("Player").transform;
//		rotationSpeed = 5.0f;
//		nav = GetComponent<NavMeshAgent> ();
//		aiAnim = GetComponent<Animator> ();
//		abilityManager = GetComponent<AbilityManager> ();
//        hp = GetComponent<Health>();

//		//nav.SetDestination (target.position);
//	}
	
//	// Update is called once per frame
//	void Update () {
//		//if (Input.GetKey (KeyCode.C)) {
//		//	abilityManager.CancelAbility ();
//		//}
//		//if (Input.GetKey (KeyCode.V)) {
//		//	aiAnim.ResetTrigger ("GetHit");
//		//}
//	}
	 
//	void FixedUpdate()
//	{
//        if (!hp.GetisDead())
//        {
//            aiAnim.SetFloat("speed", nav.velocity.magnitude);

//            float toPlayerDistance = Vector3.Distance(transform.position, target.position);
//            float dotP = Vector3.Dot(transform.forward, target.forward);
//            if (toPlayerDistance < engagedDistance)
//            {
//                Attack();
//            }
//            else
//            {
//                Wander();
//            }
//        }
//        else if (nav.enabled)
//        {
//            nav.isStopped = true;
//            nav.enabled = false;
//            Destroy(gameObject, 3.0f);
//        }
//	}

//	private void Attack()
//	{	
//		int cdCount  = abilityManager.cooldowns.Count;
//		/*for (int i = 0; i < cdCount; i ++ )
//		{
//			if (abilityManager.cooldowns [abilityManager.abilities [i].abilityName].coolDownReady)
//			{
//				abilityManager.StartCastingAbility (abilityManager.abilities [i].abilityName);
//				break;
//			}
//		}*/
//		if (nav.enabled) {
//			nav.SetDestination (target.position);
//			// Check if we've reached the destination
//			if (!nav.pathPending) {
//				if (nav.remainingDistance <= nav.stoppingDistance) {
//					if (!nav.hasPath || nav.velocity.sqrMagnitude == 0f) {
//						RotateTowards (target);
//						for (int i = 0; i < cdCount; i ++ )
//		{
//			if (abilityManager.cooldowns [abilityManager.abilities [i].abilityName].coolDownReady)
//			{
//				abilityManager.StartCastingAbility (abilityManager.abilities [i].abilityName);
//				break;
//			}
//		}
//					}
//				}
//			}
//		}
//	}

//	private void RotateTowards(Transform target)
//	{
//		Vector3 direction = (target.position - transform.position).normalized;
//		Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));    // flattens the vector3
//		transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
//	}

//	private void Wander()
//	{
//		if (findPatrolPoint) 
//		{
//			float randPatrolDistance = Random.Range (patrolDistance - patrolDistanceVariance, patrolDistance + patrolDistanceVariance);
//			Vector3 patrolTarget = RandomNavSphere (transform.position, randPatrolDistance, NavMesh.AllAreas);

//			nav.SetDestination (patrolTarget);
//			findPatrolPoint = false;
//		}
			
//		patrolTime += Time.deltaTime;
//		if (patrolTime > randPatrolTime) 
//		{
//			findPatrolPoint = true;
//			patrolTime = 0.0f;
//			randPatrolTime = Random.Range (patrolRate - patrolRateVariance, patrolRate + patrolRateVariance);
//		}

//	}

//	public static Vector3 RandomNavSphere (Vector3 origin, float distance, int layermask) 
//	{
//		Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance;

//		randomDirection += origin;

//		NavMeshHit navHit;

//		NavMesh.SamplePosition (randomDirection, out navHit, distance, layermask);

//		return navHit.position;
//	}
//}
