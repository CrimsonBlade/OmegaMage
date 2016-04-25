using UnityEngine;
using System.Collections;

public class AirGroundSpell : PT_MonoBehaviour {

	public float 		duration = .1f;  // Lifetime of this GameObject
	public float		durationVariance = 0.1f;
	// ^ this allows the duration to range from 
	public float		fadeTime = .1f; //length of time to fade
	public float		timeStart;     //birth time of this GameObject
	public float 		damagePerSecond = 50;
	// Use this for initialization
	void Start () {
		timeStart = Time.time;
		duration = Random.Range (duration - durationVariance,
		                         duration + durationVariance);
		// ^ set teh duration to  a number between 3.5 and 4.5 (defaults)
	}
	
	// Update is called once per frame
	void Update () {
		//determine a number [0..1] (between 0 and 1 ) that stores the
		//  percentage of duration that has passed
		float u = (Time.time - timeStart) / duration;
		
		//at what u value should this start fading
		float fadePercent = 1 - (fadeTime / duration);
		if (u > fadePercent) {//if its after the time to start fading...
			//...then sink into the ground
			float u2 = (u-fadePercent)/(1-fadePercent);
			// ^ u2 is a number  [0..1] for just the fadeTime
			Vector3 loc = pos;
			loc.z = u2*2;      // move lower over time
			pos = loc;
		}
		
		if (u > 1) {//if this has lived longer than duration...
			Destroy(gameObject); //...destroy it
		}
	}
	
	void OnTriggerEnter(Collider other){
		//announce when another object enters the collider
		GameObject go = Utils.FindTaggedParent (other.gameObject);
		if (go == null) {
			go = other.gameObject;	
		}
		Utils.tr ("Air hit", go.name);
	}
	
	void OnTriggerStay(Collider other){
		//Actually damage the other
		//get a reference to the EnemyBug script component of the other
		EnemyBug recipient = other.GetComponent<EnemyBug> ();
		//if there is an EnemyBug component, damage it with fire
		if(recipient != null){
			recipient.Damage(damagePerSecond, ElementType.air, true);
		}
	}
	//TODO Actual damage and status effects
}
