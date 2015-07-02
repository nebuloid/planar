using UnityEngine;
using System.Collections;

public class MovementScript : MonoBehaviour {
	int speed = 5;
	int gravity = 5;
	CharacterController cc;
	// Use this for initialization
	void Start () {
		cc = GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () {
		if(GetComponent<NetworkView>().isMine) {
			cc.Move(new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime,
			        -gravity * Time.deltaTime, 
			        Input.GetAxis("Vertical") * speed * Time.deltaTime));
		}
	}
}
