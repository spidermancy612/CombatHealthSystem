using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthTester : MonoBehaviour {

    private HealthController controller;

    // Use this for initialization
    void Start()
    {
        controller = GetComponent<HealthController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) controller.applyHealth(10);
        if (Input.GetKeyDown(KeyCode.W)) controller.applyHealth(10, SegmentType.health);
        if (Input.GetKeyDown(KeyCode.E)) controller.applyHealth(10, "test1");
        if (Input.GetKeyDown(KeyCode.R)) controller.applyHealth(10, new string[] { "test1", "test2" });
        if (Input.GetKeyDown(KeyCode.T)) controller.applyHealth(10, 0);
    }
}
