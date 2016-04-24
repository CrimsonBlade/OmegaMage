using UnityEngine;
using System.Collections;

public class Portal : PT_MonoBehaviour {

	public string 		toRoom;
	public bool 		justArrived = false;
	// ^ true if mage has just teleported here

	void OnTriggerEnter(Collider other){
		if (justArrived) return;
		// ^ since the mage has just arrived, don't teleport them back

		//get the GameObject of the collider
		GameObject go = other.gameObject;
		//search up for a tagged parent
		GameObject goP = Utils.FindTaggedParent (go);
		if (goP != null) go = goP;

		//if this isn't the mage return
		if (go.tag != "Mage") return;

		// go ahead and build the next room
		LayoutTiles.S.BuildRoom (toRoom);
	}

	void OnTriggerExit(Collider other){
		//once the mage leaves this Portal set justArrived to false
		if (other.gameObject.tag == "Mage") {
			justArrived = false;	
		}
	}	
}
