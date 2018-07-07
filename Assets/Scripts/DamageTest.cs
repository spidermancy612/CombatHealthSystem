using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTest : MonoBehaviour {

    private Health health;
    public float damage;

	// Use this for initialization
	void Start () {
        health = GetComponent<Health>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Space))
        {
            //health.applyDamage(damage);
            //Debug.Log("Dealing damage");
        }
	}
}
