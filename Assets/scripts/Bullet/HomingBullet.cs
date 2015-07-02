using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HomingBullet : MonoBehaviour {

	public float speed = 10f;
	private List<GameObject> enemies;
	public float homingFactor = 10f;
	public GameObject mine = null;
	private int destroyedID = -1111;

	// Use this for initialization
	void Start () {
		StartCoroutine("DestroySelf");
	}

	// Update is called once per frame
	void Update () {
		transform.Translate (transform.forward * Time.deltaTime * speed, Space.World);

		foreach (GameObject g in EnemyListController.instance.enemyList) {
			if(Vector3.Distance(g.transform.position, transform.position) < homingFactor && mine != g)
			{
				transform.LookAt(g.transform.position);
			}
		}
	}

	void OnTriggerEnter(Collider c)
	{
		if (mine != c.gameObject)
			if (c.transform.tag == "playerShip") {
				c.gameObject.GetComponent<OVRPlayerController>().Die();
				Debug.Log("bullet hit ship");
				if (transform.GetComponent<NetworkView>().GetInstanceID() != destroyedID) {
					destroyedID = transform.GetComponent<NetworkView>().GetInstanceID();
					if (gameObject.GetComponent<NetworkView>().isMine)
						Network.Destroy(gameObject);
				}
			}
	}

	IEnumerator DestroySelf()
	{
		yield return new WaitForSeconds(4f);
		if (transform.GetComponent<NetworkView>().GetInstanceID() != destroyedID) {
			destroyedID = transform.GetComponent<NetworkView>().GetInstanceID();
			if (gameObject.GetComponent<NetworkView>().isMine)
				Network.Destroy(gameObject);
		}
		yield break;
	}
}
