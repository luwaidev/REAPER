using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBreathing : MonoBehaviour
{
    private float startPos;
    public float amplitude = 0.04f;
    public float period = 1f;
    private CameraController camController;


    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.localPosition.y;
        camController = GetComponentInParent<CameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        float xValue = Time.timeSinceLevelLoad / period;
        float distance = amplitude * Mathf.Sin(xValue);
        transform.localPosition = new Vector3(0, startPos + distance, 0);
    }
}