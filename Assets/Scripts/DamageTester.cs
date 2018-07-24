using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTester : MonoBehaviour {

    private HealthController controller;

    public float damage;
    public SegmentType type;

	// Use this for initialization
	void Start () {
        controller = GetComponent<HealthController>();
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.A)) controller.applyDamage(damage);
        if (Input.GetKeyDown(KeyCode.S)) controller.applyDamage(damage, true);
        if (Input.GetKeyDown(KeyCode.D)) controller.applyDamage(damage, true, type);
        if (Input.GetKeyDown(KeyCode.F)) controller.applyDamage(damage, true, "test1");
        if (Input.GetKeyDown(KeyCode.G)) controller.applyDamage(damage, true, new string[] { "test1", "test2" });
        if (Input.GetKeyDown(KeyCode.H)) controller.applyDamage(damage, true, 0);
    }
}
