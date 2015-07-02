/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the player's movement in virtual reality.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OVRPlayerController : MonoBehaviour
{

	public bool killedThings = false;
	/// <summary>
	/// The rate acceleration during movement.
	/// </summary>
	public float Acceleration = 0.1f;

	//public Transform spawnPoint;
	/// <summary>
	/// The rate of damping on movement.
	/// </summary>
	public float Damping = 0.3f;

	/// <summary>
	/// The rate of additional damping when moving sideways or backwards.
	/// </summary>
	public float BackAndSideDampen = 0.5f;

	/// <summary>
	/// The force applied to the character when jumping.
	/// </summary>
	public float JumpForce = 0.3f;

	/// <summary>
	/// The rate of rotation when using a gamepad.
	/// </summary>
	public float RotationAmount = 1.5f;

	/// <summary>
	/// The rate of rotation when using the keyboard.
	/// </summary>
	public float RotationRatchet = 45.0f;

	/// <summary>
	/// The player's current rotation about the Y axis.
	/// </summary>
	private float YRotation = 0.0f;

	private float XRotation = 0.0f;

	private NetworkView networkView;

	/// <summary>
	/// If true, tracking data from a child OVRCameraRig will update the direction of movement.
	/// </summary>
	public bool HmdRotatesY = true;

	/// <summary>
	/// Modifies the strength of gravity.
	/// </summary>
	public float GravityModifier = 0.379f;

	private float MoveScale = 1.0f;
	private Vector3 MoveThrottle = Vector3.zero;
	private float FallSpeed = 0.0f;	
	
	/// <summary>
	/// If true, each OVRPlayerController will use the player's physical height.
	/// </summary>
	public bool useProfileHeight = true;

	protected CharacterController Controller = null;
	protected OVRCameraRig CameraController = null;
	protected Transform DirXform = null;

	private float MoveScaleMultiplier = 1.0f;
	private float RotationScaleMultiplier = 1.0f;
	private bool  SkipMouseRotation = false;
	private bool  HaltUpdateMovement = false;
	private bool prevHatLeft = false;
	private bool prevHatRight = false;
	private float SimulationRate = 60f;
	private uint spamCount = 0;

	private bool delCam = false;

	//stage boundaries
	private float minZBoundary = -240f;
	private float maxZBoundary = 240f;
	private float minYBoundary = -50f;
	private float maxYBoundary = 50f;
	private float minXBoundary = -240f;
	private float maxXBoundary = 240f;
	
	//ship rendering
	public MeshRenderer shipRenderer;
	public GameObject shipCam;
	
	//ship movement
	public float jerkMax, accelerationMax, velocityMax, rotateMax = 100f;
	float currVel = 200f;
	//public Transform spawnPoint;

	//ship shooting
	private const string bulletPath = "Bullet";
	[SerializeField] private HomingBullet bullet;
	Vector3 reticuleVector;
	public int bulletCapacityMax = 5;
	private int bulletCount;
	public float bulletRegenDelay = .25f;

	public bool IsMine
	{
		get{ return networkView.isMine;}
	}

	void Awake()
	{
		networkView = gameObject.GetComponent<NetworkView>();
		bullet = Resources.Load<HomingBullet>(bulletPath);
		Controller = gameObject.GetComponent<CharacterController>();

		//if(Controller == null)
			//Debug.LogWarning("OVRPlayerController: No CharacterController attached.");

		// We use OVRCameraRig to set rotations to cameras,
		// and to be influenced by rotation
		OVRCameraRig[] CameraControllers;
		CameraControllers = gameObject.GetComponentsInChildren<OVRCameraRig>();

		//if(CameraControllers.Length == 0)
			//Debug.LogWarning("OVRPlayerController: No OVRCameraRig attached.");
		//else if (CameraControllers.Length > 1)
			//Debug.LogWarning("OVRPlayerController: More then 1 OVRCameraRig attached.");
		//else
			CameraController = CameraControllers[0];

		DirXform = transform;

		//if(DirXform == null)
			//Debug.LogWarning("OVRPlayerController: ForwardDirection game object not found. Do not use.");
		networkView.RPC("Respawn", RPCMode.All, Network.player);
#if UNITY_ANDROID && !UNITY_EDITOR
		OVRManager.display.RecenteredPose += ResetOrientation;
#endif
	}

	void Update()
	{
		if (!networkView.isMine) 
		{
			if(!killedThings)
			{	
				killedThings = true;
				EnemyListController.instance.SubscribeToEnemyList(gameObject);
				for(int i = 0; i < transform.childCount; ++i)
				{
					Destroy(transform.GetChild(0).gameObject);
				}
			}
			return;
		}
		//Debug.Log("networkView is Mine = " + networkView.isMine);
		
		Vector3 v;
		if (Mathf.Abs(OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.RightYAxis)) > .1f || Mathf.Abs(OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.RightXAxis)) > .1f) {
			v = transform.rotation.eulerAngles;
			float xBy = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.RightYAxis) * Time.deltaTime * rotateMax;
			//Debug.Log(v.x + xBy);
			if (!(v.x + xBy > 265f && (v.x + xBy) < 275) && !(v.x + xBy > 85f && v.x + xBy < 95))
				transform.Rotate (new Vector3 (OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.RightYAxis) * Time.deltaTime * rotateMax, 0f, 0f));
			transform.Rotate (new Vector3 (0f, (OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.RightXAxis) * Time.deltaTime * rotateMax), 0f));
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
		
		if (OVRGamepadController.GPC_GetButton(OVRGamepadController.Button.L1) ||
		    OVRGamepadController.GPC_GetButton(OVRGamepadController.Button.R1))
		{
			ShootBullet();
		}
				//float axisV = Input.GetAxis("Vertical");

		float axisV = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftYAxis);
		if(Mathf.Abs(axisV) > 0.5f)
		{
			transform.Translate(axisV * transform.forward * currVel * Time.deltaTime, Space.World);
		}
		
		//float axisH = Input.GetAxis("Horizontal");
		float axisH = OVRGamepadController.GPC_GetAxis(OVRGamepadController.Axis.LeftXAxis);
		if(Mathf.Abs(axisH) > 0.5f)
		{
			transform.Translate(axisH * transform.right * currVel * Time.deltaTime, Space.World);
		}

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
//	 
	/// <summary>
	/// Gets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void GetMoveScaleMultiplier(ref float moveScaleMultiplier)
	{
		moveScaleMultiplier = MoveScaleMultiplier;
	}

	/// <summary>
	/// Sets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void SetMoveScaleMultiplier(float moveScaleMultiplier)
	{
		MoveScaleMultiplier = moveScaleMultiplier;
	}

	/// <summary>
	/// Gets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void GetRotationScaleMultiplier(ref float rotationScaleMultiplier)
	{
		rotationScaleMultiplier = RotationScaleMultiplier;
	}

	/// <summary>
	/// Sets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void SetRotationScaleMultiplier(float rotationScaleMultiplier)
	{
		RotationScaleMultiplier = rotationScaleMultiplier;
	}

	/// <summary>
	/// Gets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">Allow mouse rotation.</param>
	public void GetSkipMouseRotation(ref bool skipMouseRotation)
	{
		skipMouseRotation = SkipMouseRotation;
	}

	/// <summary>
	/// Sets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">If set to <c>true</c> allow mouse rotation.</param>
	public void SetSkipMouseRotation(bool skipMouseRotation)
	{
		SkipMouseRotation = skipMouseRotation;
	}

	/// <summary>
	/// Gets the halt update movement.
	/// </summary>
	/// <param name="haltUpdateMovement">Halt update movement.</param>
	public void GetHaltUpdateMovement(ref bool haltUpdateMovement)
	{
		haltUpdateMovement = HaltUpdateMovement;
	}

	/// <summary>
	/// Sets the halt update movement.
	/// </summary>
	/// <param name="haltUpdateMovement">If set to <c>true</c> halt update movement.</param>
	public void SetHaltUpdateMovement(bool haltUpdateMovement)
	{
		HaltUpdateMovement = haltUpdateMovement;
	}

	/// <summary>
	/// Resets the player look rotation when the device orientation is reset.
	/// </summary>
	public void ResetOrientation()
	{
		YRotation = 0.0f;
	}

	public void ShootBullet()
	{
		spamCount++;
		if (bulletCount > 0 && spamCount % 5 == 0) {
			--bulletCount;
			//transform.Translate (transform.forward, Space.World);
			HomingBullet b = Network.Instantiate (bullet, transform.position, transform.rotation, 0) as HomingBullet;
			b.mine = gameObject;
			Debug.Log("bullet");
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
		networkView.RPC("Respawn", RPCMode.All, Network.player);
	}


	[RPC]
	public void Respawn(NetworkPlayer player)
	{
		if (player != Network.player)
			return;

		if(!networkView.isMine)
			return;

		StartCoroutine ("RegenBullets");
		bulletCount = bulletCapacityMax;
		//transform.position = spawnPoint.position;
		//transform.rotation = spawnPoint.rotation;
		transform.position = new Vector3(0f, 0f, 0f);
		//transform.rigidbody.rotation = Quaternion.identity;
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

