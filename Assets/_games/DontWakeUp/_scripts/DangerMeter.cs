﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DangerMeter : MonoBehaviour
{

    float intensity;
    public float speed = 0.2f;
    bool inDanger;
    public CanvasGroup dangerCanvas;

    // Use this for initialization
    void Start() {
        intensity = 0f;
    }
	
    // Update is called once per frame
    void Update() {
        if (inDanger) {
            intensity = intensity + speed * Time.deltaTime;
            if (intensity > 1f)
                intensity = 1f;
        } else {
            intensity = intensity - speed * Time.deltaTime;
            if (intensity < 0)
                intensity = 0;
        }

        dangerCanvas.alpha = intensity;
    }

    public void InDanger(bool status) {
        inDanger = status;
    }

}
