using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//the MPhase enum is used to track the phase of mouse interaction
public enum MPhase{
	idle,
	down,
	drag
}

//MouseInfo stores information about the mouse in each frame of interaction
[System.Serializable]
public class MouseInfo{
	public Vector3		loc; //3d loc of mouse near z=0
	public Vector3		screenLoc;//screen position of the mouse
	public Ray			ray; //ray from the mouse into 3d space
	public float 		time; //time this mouseInfo was recorded
	public RaycastHit	hitInfo; // Info about what was hit by the ray
	public bool			hit; //wether the mouse was over any collider

	//these methids see if the mosue Ray hits anything
	public RaycastHit Raycast(){
		hit = Physics.Raycast (ray, out hitInfo);
		return(hitInfo);
	}

	public RaycastHit Raycast(int mask){
		hit = Physics.Raycast (ray, out hitInfo, mask);
		return(hitInfo);
	}
}
	
public class Mage : PT_MonoBehaviour {
	static public Mage S;
	static public bool DEBUG = true;

	public float		mTapTime = 0.1f; //how long is considered a tap
	public GameObject	tapIndicatorPrefab; //prefab for the tap indicator
	public float		mDragDist = 5; // min dist in pixels to be a drag
	public float		activeScreenWidth = 1; // % of the screen to use

	public float 		speed = 2; //walk speed for the Mage

	public bool 		_______________;

	public MPhase		mPhase = MPhase.idle;
	public List<MouseInfo> mouseInfos = new List<MouseInfo>();

	public bool 		walking = false;
	public Vector3		walkTarget;
	public Transform	characterTrans;


	void Awake () {
		S = this; // set the mage singleton
		mPhase = MPhase.idle;

		//find the charaterTrans to rotate with Face()
		characterTrans = transform.Find("CharacterTrans");
	}

	void Update(){
		//find whether the mouse button 0 was pressed or released this frame
		bool b0Down = Input.GetMouseButtonDown (0);
		bool b0Up = Input.GetMouseButtonUp (0);

		//handle all input here (except for Inventory buttons)
		/*
		 * there are only a few possible actions:
		 * 1. Tap on the ground to move to that point
		 * 2. drag on the ground with no spell selected to move to the mage
		 * 3. drag on the ground with spell selected to cast along ground
		 * 4. tap on an enemy to attack (or force push away without an element)
		 * */

		//an example of useing < to return a bool value
		bool inActiveArea = (float)Input.mousePosition.x / Screen.width <
						activeScreenWidth;

		//this is handled as an if statement instead of switch because a tap
		// can sometimes happen within a single frame
		if (mPhase == MPhase.idle) {//if the mouse is idle
			if(b0Down && inActiveArea){
				mouseInfos.Clear (); //clear the mouseInfos
				AddMouseInfo();		//and add a first MouseInfo

				//if the mouse was clicked on something, it's a valid MouseDown
				if(mouseInfos[0].hit){ //something was hit
					MouseDown();		//call MouseDown()
					mPhase = MPhase.down; //and set the mPhase
				}
			}		
		}

		if (mPhase == MPhase.down){  //if the mouse is down
			AddMouseInfo();			//add a MouseInfo for this frame
			if(b0Up){				//the mouse button was released
				MouseTap();			//this was a tap
				mPhase = MPhase.idle;
			}else if (Time.time - mouseInfos[0].time > mTapTime){
				//if it's been down longer than a tap, this may be a drag, but
				// to be a drag, it must also have moved a certian number of
				// pixels on screen.
				float dragDist = (lastMouseInfo.screenLoc -
				                  mouseInfos[0].screenLoc).magnitude;
				if(dragDist >= mDragDist){
					mPhase = MPhase.drag;
				}
			}
		}

		if(mPhase == MPhase.drag){  //if the mouse is being dragged
			AddMouseInfo();
			if (b0Up){
				// the mouse button was released
				MouseDragUp();
				mPhase = MPhase.idle;
			}else{
				MouseDrag(); //Still dragging
			}
		}
	}

	//pulls info about the Mouse, adds it to mouseInfos, and returns it
	MouseInfo AddMouseInfo(){
		MouseInfo mInfo = new MouseInfo ();
		mInfo.screenLoc = Input.mousePosition;
		mInfo.loc = Utils.mouseLoc; //gets the position of the mouse at z=0
		mInfo.ray = Utils.mouseRay; //gets the ray from the main camera through the mouse pointer

		mInfo.time = Time.time;
		mInfo.Raycast ();          //default is to raycast with no mask

		if (mouseInfos.Count == 0) {
			// if this is the first mouseInfo
			mouseInfos.Add (mInfo); //add mInfo to mouseInfos
		} else {
			float lastTime = mouseInfos[mouseInfos.Count-1].time;
			if(mInfo.time != lastTime){
				//if time has passed since the last mouseInfos
				mouseInfos.Add (mInfo); // add mInfo to mouseInfos
			}
			//this time ties is necessary because AddMouseInfo() could be
			// called twice in one frame
		}
		return(mInfo); //Return mInfo as well
	}

	public MouseInfo lastMouseInfo{
		//Access to the latest MouseInfo
		get{
			if(mouseInfos.Count == 0) return (null);
			return(mouseInfos[mouseInfos.Count-1]);
		}
	}

	void MouseDown(){
		// the mouse was pressed on something (it could be a drag or tap)
		if (DEBUG)print ("Mage.MouseDown()");
	}

	void MouseTap(){
		// something was tapped like a button
		if (DEBUG)print ("Mage.MouseTap()");

		WalkTo (lastMouseInfo.loc); //walk to the latest mouseInfo pos
		ShowTap (lastMouseInfo.loc); //show where the player tapped
	}

	void MouseDrag(){
		// mouse is being dragged across something 
		if (DEBUG)print ("Mage.MouseDrag()");
	}
	void MouseDragUp(){
		// the mouse is released after being dragged 
		if (DEBUG)print ("Mage.MouseDragUp()");
	}

	//walk to a specific position. the position.z is always 0
	public void WalkTo(Vector3 xTarget){
		walkTarget = xTarget;  //set the point to walk to
		walkTarget.z = 0;  		//Froce z=0
		walking = true;			//now the mage is walking
		Face (walkTarget);		// look in the direction of the walkTarget
	}

	public void Face(Vector3 poi){ // face toward a point of interest
		Vector3 delta = poi - pos;  //find vector to the point of interest
		//use Atan2 to get the rotation around z that points the x-axis of
		// mage : characterTrans toward poi
		float rZ = Mathf.Rad2Deg * Mathf.Atan2 (delta.y, delta.x);
		//set the rotation of characterTrans (doesn't actually rotate mage)
		characterTrans.rotation = Quaternion.Euler (0, 0, rZ);
	}

	public void StopWalking(){ //stops the mage from walking
		walking = false;
		rigidbody.velocity = Vector3.zero;
	}

	void FixedUpdate() { //happens every physics step (50 times a second)
		if (walking) {// if mage is walking
			if ((walkTarget - pos).magnitude < speed * Time.fixedDeltaTime) {
				// if mage is very close to walkTarget, just stop there
				pos = walkTarget;
				StopWalking ();
			} else {
				//otherwise, move to walkTarget
				rigidbody.velocity = (walkTarget - pos).normalized * speed;
			}
		} else {
			//if not walking, velocity should be 0
			rigidbody.velocity = Vector3.zero;
		}
	}

	void onCollisionEnter(Collision coll){
		GameObject otherGO = coll.gameObject;
		//colliding with a wall can also stop walking
		Tile ti = otherGO.GetComponent<Tile> ();
		if (ti != null) {
			if(ti.height > 0){ // if ti.height is > 0
				// then thsi ti is a wall, and mage should stop
				StopWalking();
			}		
		}
	}

	//show where the player tapped
	public void ShowTap(Vector3 loc){
		GameObject go = Instantiate (tapIndicatorPrefab) as GameObject;
		go.transform.position = loc;
	}
}
