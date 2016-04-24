using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyBug : PT_MonoBehaviour {
	public float 		speed = 0.5f;
	public float		health = 10;
	public float		damageScale = 0.8f;
	public float		damageScaleDuration = 0.25f;

	public bool 		__________________;

	private float		damageScaleStartTime;
	private float		maxHealth;
	public Vector3 		walkTarget;
	public bool 		walking;
	public Transform	characterTrans;
	//stores damage for each element each frame
	public Dictionary<ElementType,float>  damageDict;
	//NOTE dictionaries do not appear in the Unity Inspector
	
	void Awake () {
		characterTrans = transform.Find("CharacterTrans");
		maxHealth = health;
		ResetDamageDict ();
	}

	//resets the values for the damageDict
	void ResetDamageDict(){
		if (damageDict == null) {
			damageDict = new Dictionary<ElementType, float>();	
		}
		damageDict.Clear ();
		damageDict.Add (ElementType.earth, 0);
		damageDict.Add (ElementType.water, 0);
		damageDict.Add (ElementType.air, 0);
		damageDict.Add (ElementType.fire, 0);
		damageDict.Add (ElementType.aether, 0);
		damageDict.Add (ElementType.none, 0);
	}

	void Update () {
		WalkTo (Mage.S.pos);
	}

	//------------------------ Walking Code ----------------------------//

	//all of this waling code is copied directly from mage

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

	// damage this instance. by default, the damage is instant, but it can also
	// be treated as damage over time, where the amt value would be the amount 
	// of damage done every second
	// NOTE this same code can be used to heal the instance
	public void Damage (float amt, ElementType eT, bool damageOverTime = false){
		//if it's a DOT, then only damage the fractional amount for this frame
		if (damageOverTime) {
			amt *= Time.deltaTime;	
		}

		//treat different damage types differently (most are default)
		switch (eT) {

		case ElementType.fire:
			//only the max damage from fire source addects this instance
			damageDict[eT] = Mathf.Max (amt, damageDict[eT]);
			break;

		case ElementType.earth:
			//earth does not damage bugs
			break;

		default:
			// by default, damage is added to the other damage by same element
			damageDict[eT] += amt;
			break;
		}
	}

	//lateUpdate() is automatically called by Unity every frame Once all the 
	//Updates() on all instances have been called, than LateUpdate() is called
	// on all instances
	void LateUpdate(){
		//apply damage from the different element types

		//iteration through a dictionart uses a KeyValuePair
		//entry.Key is the ElementType, While entry.value is the float
		float dmg = 0;
		foreach (KeyValuePair<ElementType,float> entry in damageDict) {
			dmg +=	entry.Value;
		}

		if (dmg > 0) {// if this took damage...
			//and if it is at full scale now (& not already damage scaling)...	
			if(characterTrans.localScale  == Vector3.one){
				//start the damage scale animation
				damageScaleStartTime = Time.time;
			}
		}

		//the damage scale animation
		float damU = (Time.time - damageScaleStartTime) / damageScaleDuration;
		damU = Mathf.Min (1, damU);  //limit the max localScale to 1
		float sc1 = (1 - damU) * damageScale + damU * 1;
		characterTrans.localScale = sc1 * Vector3.one;
	
		health -= dmg;
		health = Mathf.Min (maxHealth, health);  //limit health if healing

		ResetDamageDict ();  // prepare for next frame

		if (health <= 0) {
			Die();	
		}
	}

	//making Die() a seperate function allows us to add thing later like
	// different death animations, dropping sothing for player, etc.
	public void Die(){
		Destroy (gameObject);
	}
}
