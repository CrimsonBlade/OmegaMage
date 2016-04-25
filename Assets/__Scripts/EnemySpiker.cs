using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpiker : PT_MonoBehaviour, Enemy {
	[SerializeField]
	private float		touchDmg = 0.5f;
	public float		touchDamage{
		get{return(touchDmg);}
		set{touchDmg = value;}
	}
	//the pos Property is alreay implemented in PT_MonoBehaviour
	public string		typeString{
		get{return(roomXMLString);}
		set{roomXMLString = value;}
	}


	public float		speed = 5f;
	public string		roomXMLString = "{";
	

	public bool 	____________________;



	public Vector3		moveDir;
	public Transform	characterTrans;



	void Awake(){
		characterTrans = transform.Find("CharacterTrans");
	}

	void Start () {
		//set the move direction based on the character in Rooms.xml
		switch (roomXMLString) {
		case "^":
			moveDir = Vector3.up;
			break;
		case "v":
			moveDir = Vector3.down;
			break;
		case "{":
			moveDir = Vector3.left;
			break;
		case "}":
			moveDir = Vector3.right;
			break;
		}
	}

	void FixedUpdate () {// happends every physiscs step (50 times a second)
		rigidbody.velocity = moveDir * speed;
	}

	//this has the same structure as the damage method in EnemyBug
	public void Damage (float amt, ElementType eT, bool damageOverTime = false){
		//nothing damages the enemySpiker

	}

	void OnTriggerEnter(Collider other){
		//check to see if a wall was hit
		GameObject go = Utils.FindTaggedParent (other.gameObject);
		if(go == null) return; // in case nothing was tagged

		if (go.tag == "Ground") {
			// make sure that the ground tile is in the direction we're moving
			// a dot product will help us with this  (see useful concepts reference)
			float dot = Vector3.Dot (moveDir, go.transform.position - pos);
			if(dot > 0) { // if spiker is moving towards the block it hit
				moveDir *= -1;   //reverse direction

			}
		}

		if (go.tag == "Water") {
			this.speed = speed / 2f;
		}

	}

	void OnTriggerExit(Collider other){
		GameObject go = Utils.FindTaggedParent (other.gameObject);
		if (go.tag == "Water") {
			speed = 5f;	
		}

	}
}
