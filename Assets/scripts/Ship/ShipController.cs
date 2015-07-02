using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipController : MonoBehaviour {

	//stage boundaries
	public float minZBoundary = -10000f;
	public float maxZBoundary = 10000f;
	public float minYBoundary = 0f;
	public float maxYBoundary = 5000f;
	public float minXBoundary = -10000f;
	public float maxXBoundary = 10000f;
	
	//ship rendering
	public MeshRenderer shipRenderer;
	public GameObject shipCam;

	//ship movement
	public float jerkMax, accelerationMax, velocityMax, rotateMax = 100f;
	public float currJerk = 1f, currAcc = 1f, currVel = 1f;
	public Transform spawnPoint;
		
	private bool delCam = false;

	//ship shooting
	public HomingBullet bullet;
	Vector3 reticuleVector;
	public int bulletCapacityMax = 5;
	private int bulletCount;
	public float bulletRegenDelay = .25f;

	// Use this for initialization
	void Start () {
		EnemyListController.instance.SubscribeToEnemyList(gameObject);
		bulletCount = bulletCapacityMax;
		StartCoroutine ("RegenBullets");
	}

	void Update()
	{
		if (!GetComponent<NetworkView>().isMine && delCam == false) {
			delCam = true;
			Destroy(transform.GetChild(0).gameObject);
			return;		
		} else if (!GetComponent<NetworkView>().isMine) {
			return;
		}

		Vector3 v;

		if (Mathf.Abs(Input.GetAxis("Mouse X")) > .1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > .1f) {
			v = transform.rotation.eulerAngles;
			float xBy = -Input.GetAxis ("Mouse X") * Time.deltaTime * rotateMax;
			//Debug.Log(v.x + xBy);
			if (!(v.x + xBy > 265f && (v.x + xBy) < 275) && !(v.x + xBy > 85f && v.x + xBy < 95))
				transform.Rotate (new Vector3 (-Input.GetAxis ("Mouse X") * Time.deltaTime * rotateMax, 0f, 0f));
				transform.Rotate (new Vector3 (0f, (Input.GetAxis ("Mouse Y") * Time.deltaTime * rotateMax), 0f));
		}

		v = transform.rotation.eulerAngles;
		//Debug.Log(transform.rotation.eulerAngles.x + "x " + transform.rotation.eulerAngles.y + "y " + transform.rotation.z + "z");
		v.z = 0f;
		Quaternion q = new Quaternion ();
		q.eulerAngles = v;
		Vector3 adjVect = q.eulerAngles;
		if (adjVect.x == 270f)
			adjVect.x = 271f;
		if (adjVect.x == 90f)
			adjVect.x = 91f;

		q.eulerAngles = adjVect;
		transform.rotation = q;

		if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire1")) 
		{
			ShootBullet();
			//Debug.Log("bullet fired: " + transform.forward + " " +  b.transform.forward);
		}
		//out
//		//float axisV = Input.GetAxis("Vertical");
//		float axisV = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftYAxis);
//		if(Mathf.Abs(axisV) > 0.5f)
//		{
//			transform.Translate(axisV * transform.forward * currVel * Time.deltaTime, Space.World);
//		}
//		
//		//float axisH = Input.GetAxis("Horizontal");
//		float axisH = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftXAxis);
//		Debug.Log("axisH : " + axisH);
//		if(Mathf.Abs(axisH) > 0.5f)
//		{
//			transform.Translate(axisH * transform.right * currVel * Time.deltaTime, Space.World);
//		}
//		//done oout

		///fIX

			float leftAxisX = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftXAxis);
			float leftAxisY = -OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftYAxis);
			Vector3 MoveThrottle = new Vector3();
			if(leftAxisY > 0.0f)
				MoveThrottle += leftAxisY
					* transform.TransformDirection(Vector3.forward * 5f);
			
			if(leftAxisY < 0.0f)
				MoveThrottle += Mathf.Abs(leftAxisY)
				* transform.TransformDirection(Vector3.back * 5f);
			
			if(leftAxisX < 0.0f)
				MoveThrottle += Mathf.Abs(leftAxisX)
				* transform.TransformDirection(Vector3.left * 5f);
			
			if(leftAxisX > 0.0f)
				MoveThrottle += leftAxisX
				* transform.TransformDirection(Vector3.right * 5f);


		transform.Translate(MoveThrottle);
		//doe fix
		if (transform.position.x < minXBoundary) 
		{
			Reflect(new Vector3(0f, 0f, 1f));
		} else if (transform.position.x > maxXBoundary) 
		{
			Reflect(new Vector3(0f, 0f, -1f));
		}
		
		if (transform.position.y > maxYBoundary) 
		{
			Die();
		} else if (transform.position.y < minYBoundary) 
		{
			Die ();
		}
		
		if (transform.position.z > maxZBoundary) 
		{
			FlipTurn(new Vector3(0f, 0f, minZBoundary));
		} else if (transform.position.z < minZBoundary) 
		{
			FlipTurn(new Vector3(0f, 0f, maxZBoundary));
		}
	}
	
	public void ShootBullet()
	{
		if (bulletCount > 0) {
			--bulletCount;
			transform.Translate (transform.forward, Space.World);
			HomingBullet b = Network.Instantiate (bullet, transform.position, transform.rotation, 0) as HomingBullet;
			b.mine = gameObject;
			//Debug.Log("bullet fired: " + transform.forward + " " +  b.transform.forward);
		}
	}
	
	IEnumerator RegenBullets()
	{
		while (true)
		{
			if(bulletCount < bulletCapacityMax)
			{
				++bulletCount;
				yield return new WaitForSeconds(bulletRegenDelay);
			}
			
			yield return 0;
		}
		
		yield break;
	}
	
	public void Die()
	{
		Debug.Log ("you dead bro");
		StopCoroutine ("RegenBullets");
		Respawn ();
	}
	
	public void Respawn()
	{
		StartCoroutine ("RegenBullets");
		bulletCount = bulletCapacityMax;
		//transform.position = spawnPoint.position;
		//transform.rotation = spawnPoint.rotation;
		transform.position = new Vector3(0f, 0f, 0f);
		transform.rotation = Quaternion.identity;
	}
	
	public void FlipTurn(Vector3 turnVec)
	{
		transform.LookAt (turnVec);
	}
	
	public void Reflect(Vector3 normal)
	{
		
		transform.LookAt(Vector3.Reflect (transform.forward, normal));
	}
}


//
//if(DirXform != null)
//{
//	float leftAxisX = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftXAxis);
//	float leftAxisY = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftYAxis);
//	
//	if(leftAxisY > 0.0f)
//		MoveThrottle += leftAxisY
//			* DirXform.TransformDirection(Vector3.forward * moveInfluence);
//	
//	if(leftAxisY < 0.0f)
//		MoveThrottle += Mathf.Abs(leftAxisY)
//			* DirXform.TransformDirection(Vector3.back * moveInfluence);
//	
//	if(leftAxisX < 0.0f)
//		MoveThrottle += Mathf.Abs(leftAxisX)
//			* DirXform.TransformDirection(Vector3.left * moveInfluence);
//	
//	if(leftAxisX > 0.0f)
//		MoveThrottle += leftAxisX
//			* DirXform.TransformDirection(Vector3.right * moveInfluence);
//}
//
//YRotation += Input.GetAxis("Mouse X") * rotateInfluence * 3.25f;
//XRotation += Input.GetAxis ("Mouse Y") * rotateInfluence * 3.25f;
//
//DirXform.rotation = Quaternion.Euler(XRotation, YRotation, 0.0f);
//transform.rotation = DirXform.rotation;