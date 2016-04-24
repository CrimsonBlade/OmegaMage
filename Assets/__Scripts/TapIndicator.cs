using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//uses PT_Mover allows use of bezier curve to alter position rotaion scale etc.

public class TapIndicator : PT_Mover {

	public float 		lifeTime = 0.4f;  //how long it will last
	public float[]		scales;				//the scales it interpolates
	public Color[]		colors;				// the colors is interpolates

	void Awake(){
		scale = Vector3.zero; // this initially hides the indicator
	}
	
	void Start () {
		//PT_Mover works based on PT_Loc class which contains information
		// about position rotaion and scale.  it is similir to a transform but
		// simpler (and unity won't let use create Transforms at will)

		PT_Loc pLoc;
		List<PT_Loc> locs = new List<PT_Loc> ();

		//the position is always the same at z=-0.1f
		Vector3 tPos = pos;
		tPos.z = -0.1f;

		//you must have an equal number of scales and colors in the Inspector
		for (int i =0; i <scales.Length; i++) {
			pLoc = new PT_Loc();
			pLoc.scale = Vector3.one * scales[i]; //each scale
			pLoc.pos = tPos;
			pLoc.color = colors[i];				//and each color

			locs.Add (pLoc);					//is added to locs
		}

		//callback is a function delegate that can call a void function() when
		// the move is done
		callback = CallbackMethod;    // call CallbackMethod() wehn finished

		// initiate the move by passing in a series of PT_Locs and duration for
		// the Bezier curve
		PT_StartMove (locs, lifeTime);
	}

	void CallbackMethod(){
		Destroy (gameObject);		//when the move is done, destroy(gameObject)
	}
}
