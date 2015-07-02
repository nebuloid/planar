using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyListController : MonoBehaviour {

	public List<GameObject> enemyList = new List<GameObject>();
	
	public static EnemyListController instance;

	void Awake()
	{
		instance = this;
	}


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SubscribeToEnemyList(GameObject g)
	{
		if(!enemyList.Contains(g))
			enemyList.Add (g);
	}

	public void UnsubscribeFromEnemyList(GameObject g)
	{
		enemyList.Remove (g);
	}
}
