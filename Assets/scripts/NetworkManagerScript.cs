using UnityEngine;
using System.Collections;

public class NetworkManagerScript : MonoBehaviour {
	float btnX;
	float btnY;
	float btnW;
	float btnH;

	public GameObject playerPrefab1;
	public GameObject playerPrefab2;
	public Transform spawnObject1;
	public Transform spawnObject2;

	string gameName = "TestGame";
	bool refreshing;
	HostData[] mHostData;

	// Use this for initialization
	void Start () {
		btnX = (float)(Screen.width * 0.05);
		btnY = (float)(Screen.height * 0.05);
		btnW = (float)(Screen.width * 0.1);
		btnH = (float)(Screen.width * 0.1);
	}

	void startServer() {
		Network.InitializeServer(32, 25001, !Network.HavePublicAddress());
		MasterServer.RegisterHost (gameName, "Tutorial Game Name", "This is a tutorial game");
	}

	void refreshHostList() {
		MasterServer.RequestHostList(gameName);
		refreshing = true;
	}

	void OnServerInitialized() {
		Debug.Log ("Server initialized!");
		spawnPlayer(1);
	}

	void OnConnectedToServer(){
		spawnPlayer(2);
	}

	void spawnPlayer(uint playerNum) {
		if (playerNum == 1) {
			GameObject spawnThing = Network.Instantiate(playerPrefab1, spawnObject1.position, Quaternion.identity, 0) as GameObject;
		} else {
			GameObject spawnThing = Network.Instantiate(playerPrefab2, spawnObject2.position, Quaternion.identity, 0) as GameObject;

		}
	}

	void OnMasterServerEvent(MasterServerEvent mse) {
		if (mse == MasterServerEvent.RegistrationSucceeded) {
			Debug.Log("Registered Server!");
		}
	}

	void OnGUI () {
		if (!Network.isClient && !Network.isServer) {
			if(GUI.Button (new Rect(btnX, btnY, btnW, btnH), "Start Server")) {
				Debug.Log ("Starting Server");
				startServer();
			}

			if(GUI.Button (new Rect(btnX, (float)(btnY * 1.2 + btnH), btnW, btnH), "Refresh Hosts")) {
				Debug.Log ("Refreshing Server");
				refreshHostList();
			}

			if (mHostData != null) {
				for(int i = 0; i < mHostData.Length; i++) {
					if(GUI.Button (new Rect((float)(btnX * 1.5 + btnW), 
					                     (float)(btnY * 1.2 + (btnH * i)), 
					                     (float)(btnW*3), (float)(btnH *0.5)),mHostData[i].gameName)) {
						Network.Connect (mHostData[i]);
						Debug.Log ("Connecting");
					}
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (refreshing) {
			if (MasterServer.PollHostList ().Length > 0) {
				refreshing = false;
				Debug.Log (MasterServer.PollHostList ().Length);
				mHostData = MasterServer.PollHostList ();
			}
		}
	
	}
}
