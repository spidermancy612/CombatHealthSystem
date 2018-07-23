using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTester : MonoBehaviour {

    private HealthController controller;

	// Use this for initialization
	void Start () {
        controller = GetComponent<HealthController>();
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.A)) controller.applyDamage(10);
        if (Input.GetKeyDown(KeyCode.S)) controller.applyDamage(10, true);
        if (Input.GetKeyDown(KeyCode.D)) controller.applyDamage(10, true, SegmentType.health);
        if (Input.GetKeyDown(KeyCode.F)) controller.applyDamage(10, true, "test1");
        if (Input.GetKeyDown(KeyCode.G)) controller.applyDamage(10, true, new string[] { "test1", "test2" });
        if (Input.GetKeyDown(KeyCode.H)) controller.applyDamage(10, true, 0);
    }
}
