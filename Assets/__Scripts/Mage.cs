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

//the elementType enum
public enum ElementType{
	earth,
	water,
	air,
	fire,
	aether,
	none
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

	public GameObject[] elementPrefabs; //the Element_Sphere Prefabs
	public float		elementRotDist = 0.5f; //Radius of rotation
	public float		elementRotSpeed = 0.5f; //period of rotation
	public int			maxNumSelectedElements = 1;  //change later it want to alow multiple element type in a single spell cast
	public Color[]		elementColors;

	//these set the min and max distances between two line points
	public float		lineMinDelta = .1f;
	public float		lineMaxDelta = .5f;
	public float		lineMaxLength = 8f;

	public GameObject	fireGroundSpellPrefab;
	public GameObject	earthGroundSpellPrefab;
	public GameObject	airGroundSpellPrefab;
	public GameObject	waterGroundSpellPrefab;

	public bool 		_______________;

	protected Transform	spellAnchor;   //the parent transform for all spells

	public float			totalLineLength;
	public List<Vector3>	linePts; //points to be shown in the line
	protected LineRenderer	liner;  //ref to the LineRenderer Component
	protected float			lineZ = -0.1f; //z depth of the line
	// ^ protected variables are between public and private
	//   public variables can be seen by everyone
	//   private variables can only be seen by this class
	//   protected variables can be seen by this class or any subclass
	//   only public variables appear in the Inspector
	//   (or those with [SerializeField] in the preceding line)

	public MPhase		mPhase = MPhase.idle;
	public List<MouseInfo> mouseInfos = new List<MouseInfo>();
	public string		actionStartTag; //["Mage", "Ground", "Enemy"]

	public bool 		walking = false;
	public Vector3		walkTarget;
	public Transform	characterTrans;

	public List<Element>  selectedElements = new List<Element>();


	void Awake () {
		S = this; // set the mage singleton
		mPhase = MPhase.idle;

		//find the charaterTrans to rotate with Face()
		characterTrans = transform.Find("CharacterTrans");

		//Get the LineRenderer component and disable it
		liner = GetComponent<LineRenderer> ();
		liner.enabled = false;

		GameObject saGO = new GameObject("Spell Anchor");
		//^ create an empty GameObject named "Spell Anchor". when you create a 
		//  new GameObject this way, it is at p[0,0,0] r[0,0,0] s[1,1,1]
		spellAnchor = saGO.transform;  //get its transform
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

				//however, drag will immediately start after mTapTime if there
				//are no elements selected
				if (selectedElements.Count == 0){
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
		OrbitSelectedElements ();
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

		GameObject clickedGO = mouseInfos [0].hitInfo.collider.gameObject;
		// ^ if the mouse wasn't clicked on anything, this would throw an error
		// because hitInfo would be null. However, we know that MouseDown()
		// is only called when the mouse WAS clicking on something, so
		// hitInfo is guaranteed to be defined

		GameObject taggedParent = Utils.FindTaggedParent (clickedGO);
		if (taggedParent == null) {
			actionStartTag = "";	
		} else {
			actionStartTag = taggedParent.tag;
			// ^ this should be either "Ground", "Mage", or "Enemy"
		}
	}

	void MouseTap(){
		// something was tapped like a button
		if (DEBUG)print ("Mage.MouseTap()");

		//now this cares what was tapped
		switch (actionStartTag) {
		case "Mage":
			//do nothing
			break;
		case "Ground":
			//move to tapped point @ z=0 whether or not an element is selected
			WalkTo (lastMouseInfo.loc); //walk to the latest mouseInfo pos
			ShowTap (lastMouseInfo.loc); //show where the player tapped
			break;
		}
	}

	void MouseDrag(){
		// mouse is being dragged across something 
		if (DEBUG)print ("Mage.MouseDrag()");

		//drag is meaningless unless the mouse started on the ground
		if (actionStartTag != "Ground") return;

		//if there is no element selected, the player should follow the mouse
		if (selectedElements.Count == 0) {
			//continuously walk toward the current mouseInfo pos
			WalkTo (mouseInfos [mouseInfos.Count - 1].loc);	
		}else{
			//this is a ground spell, so we need to draw a line
			AddPointToLiner(mouseInfos[mouseInfos.Count-1].loc);
			// ^ add the most recent MouseInfo.loc to liner 
		}		
	}
	void MouseDragUp(){
		// the mouse is released after being dragged 
		if (DEBUG)print ("Mage.MouseDragUp()");

		//drag is meaningless unless the mouse started on the ground
		if (actionStartTag != "Ground") return;

		//if there is no element selected, stop walking now
		if (selectedElements.Count == 0) {
			//stop walking when the drag is stopped
			StopWalking();
		}else{
			CastGroundSpell();

			//clear the liner
			ClearLiner();
		}
	}

	void CastGroundSpell(){
		//there is not a no-element ground spell so return
		if (selectedElements.Count == 0) return;

		//because this version of the prototype only allows a single element to 
		//  be selected, we can use that 0th element to pick the spell.
		switch (selectedElements [0].type) {
		case ElementType.fire:
			GameObject fireGO;
			foreach(Vector3 pt in linePts){//for each vector3 in linePts...
				//...create an instance of fireGroundSpellPrefab
				fireGO = Instantiate(fireGroundSpellPrefab) as GameObject;
				fireGO.transform.parent = spellAnchor;
				fireGO.transform.position = pt;
			}
			break;
			//other spells

		case ElementType.air:
			GameObject airGO;
			foreach(Vector3 pt in linePts){//for each vector3 in linePts...
				//...create an instance of airGroundSpellPrefab
				airGO = Instantiate(airGroundSpellPrefab) as GameObject;
				airGO.transform.parent = spellAnchor;
				airGO.transform.position = pt;
			}
			break;

		case ElementType.earth:
			GameObject earthGO;
			foreach(Vector3 pt in linePts){//for each vector3 in linePts...
				//...create an instance of earthGroundSpellPrefab
				earthGO = Instantiate(earthGroundSpellPrefab) as GameObject;
				earthGO.transform.parent = spellAnchor;
				earthGO.transform.position = pt;
			}
			break;


		case ElementType.water:
			GameObject waterGO;
			foreach(Vector3 pt in linePts){//for each vector3 in linePts...
				//...create an instance of waterGroundSpellPrefab
				waterGO = Instantiate(waterGroundSpellPrefab) as GameObject;
				waterGO.transform.parent = spellAnchor;
				waterGO.transform.position = pt;
			}
			break;
		}

		//clear the selectedElements; they are consumed by the spell
		ClearElements ();
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

	//chooses an element_Sphere of elType and adds it to selectedElements
	public void SelectElement(ElementType elType){
		if (elType == ElementType.none) { // if it's the none element...
			ClearElements();			  // then clear all elements
			return;						  // and return
		}

		if (maxNumSelectedElements == 1) {
			// if only one can be selected, clear the existing one..
			ClearElements();  //...so it can be replaced
		}

		//can't select more than maxNumSelectedElements simultaneously
		if (selectedElements.Count >= maxNumSelectedElements) return;

		//it is okay to add this element
		GameObject go = Instantiate (elementPrefabs [(int)elType]) as GameObject;
		// ^note the typecast from ElementType to int in line above
		Element el = go.GetComponent<Element> ();
		el.transform.parent = this.transform;

		selectedElements.Add (el); //add el to the list of selectedElements
	}

	//clears all elements from selectedElements and destroys their GameObjects
	public void ClearElements(){
		foreach (Element el in selectedElements) {
			//destroy each GameObject in the list
			Destroy(el.gameObject);
		}
		selectedElements.Clear (); //and clear the list
	}

	//called every Update() to orbit the elements around
	void OrbitSelectedElements(){
		//if there are none selected, just return
		if (selectedElements.Count == 0) return;

		Element el;
		Vector3 vec;
		float theta0, theta;
		float tau = Mathf.PI * 2; //tau is 360 deg in radians (i.e. 6.283...)

		//divide the circle into the number of elements that are orbiting
		float rotPerElement = tau / selectedElements.Count;

		//the base rotation angle (theta0) is set based on time
		theta0 = elementRotSpeed * Time.time * tau;

		for (int i = 0; i < selectedElements.Count; i++) {
			//determine the rotation angle for each element
			theta = theta0 + i*rotPerElement;
			el = selectedElements[i];
			//use simple trigonometry to turn the angle into a unit vector
			vec = new Vector3(Mathf.Cos(theta),Mathf.Sin (theta), 0);
			//multiply that unit vector by the elementRotDist
			vec *= elementRotDist;
			//raaise the element to waist height
			vec.z = -0.5f;
			el.lPos = vec;  // set the position of the Element_Sphere
		}
	}

	//----------------------- LineRenderer Code ----------------------------//

	//add a new point to the line. this ignores the point if its to close to
	// existing ones and adds extra points if its too far away
	void AddPointToLiner(Vector3 pt){
		pt.z = lineZ; //set the z of the pt to lineZ to elevate it slightly
					  // above the ground
		//linePts.Add (pt);   //not needed now that useing min and max for
		//UpdateLiner ();    // makeing the line smoother    

		//alawys add the point if linePts is empty...
		if (linePts.Count == 0) {
			linePts.Add (pt);
			totalLineLength = 0;
			return; // ...but wait for a second point to enable the LineRenderer
		}

		//if there is a previous point (pt0), then find how far pt is from it
		Vector3 pt0 = linePts [linePts.Count - 1]; //get the last point in linePts
		Vector3 dir = pt - pt0;
		float delta = dir.magnitude;
		dir.Normalize ();

		totalLineLength += delta;

		//if it's less than the min distance
		if (delta < lineMinDelta) {
			// ...then its too close, don't add it
			return;
		}

		//if its further than the max distance then add extra points...
		if (delta > lineMaxDelta) {
			// ...then add extra points in between
			float numToAdd = Mathf.Ceil (delta/lineMaxDelta);
			float midDelta = delta/numToAdd;
			Vector3 ptMid;
			for (int i = 0; i < numToAdd; i++){
				ptMid = pt0+(dir*midDelta*i);
				linePts.Add (ptMid);
			}
		}
		linePts.Add (pt); //add the point
		UpdateLiner ();  // and finally update the line
	}

	//Update the LineRenderer with the new points
	public void UpdateLiner(){
		//get the type of the selectedElement
		int el = (int)selectedElements [0].type;

		//set the line color based on that type
		liner.SetColors (elementColors [el], elementColors [el]);

		//Update the representation of the ground spell about to be cast
		liner.SetVertexCount (linePts.Count); //set the number of vertices
		for (int i = 0; i < linePts.Count; i++) {
			liner.SetPosition(i, linePts[i]); //set each vertex	
		}
		liner.enabled = true;  				 //enable the LineRenderer
	}

	public void ClearLiner(){
		liner.enabled = false;  //disable the LineRenderer
		linePts.Clear ();		// and clear all linePts
	}

	//stop any active drag or other mouse input
	public void ClearInput(){
		mPhase = MPhase.idle;
	}
}
