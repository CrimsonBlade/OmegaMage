using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Mage : PT_MonoBehaviour {
	static public Mage S;


	void Awake () {
		S = this; // set the mage singleton
	}
}
