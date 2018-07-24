using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthTester : MonoBehaviour {

    private HealthController controller;

    public float health;
    public SegmentType type;

    // Use this for initialization
    void Start()
    {
        controller = GetComponent<HealthController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) controller.applyHealth(health);
        if (Input.GetKeyDown(KeyCode.W)) controller.applyHealth(health, type);
        if (Input.GetKeyDown(KeyCode.E)) controller.applyHealth(health, "test1");
        if (Input.GetKeyDown(KeyCode.R)) controller.applyHealth(health, new string[] { "test1", "test2" });
        if (Input.GetKeyDown(KeyCode.T)) controller.applyHealth(health, 1);
    }
}
