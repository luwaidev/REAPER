using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    private Transform mainCam;
    public CinemachineVirtualCamera playerCam;
    public CinemachineBrain brain;
    private MoveController moveController;

    [Header("POV Camera Settings")]
    private CinemachinePOV pov;
    [SerializeField] private float mouseSens = 3f;
    [SerializeField] private float accelSpeed = 3f;
    [SerializeField] private float decelSpeed = 0.2f;

    public void Start()
    {
        mainCam = Camera.main.transform;
        pov = playerCam.GetCinemachineComponent<CinemachinePOV>();
        moveController = GetComponentInParent<MoveController>();

        // POV Camera Settings
        pov.m_HorizontalAxis.m_MaxSpeed = mouseSens;
        pov.m_HorizontalAxis.m_AccelTime = accelSpeed;
        pov.m_HorizontalAxis.m_DecelTime = decelSpeed;

        pov.m_VerticalAxis.m_MaxSpeed = mouseSens;
        pov.m_VerticalAxis.m_AccelTime = accelSpeed;
        pov.m_VerticalAxis.m_DecelTime = decelSpeed;
    }

    public void Update()
    {

        // Change player angles
        transform.eulerAngles = new Vector3(0, mainCam.eulerAngles.y, 0);

        //playerCam.m_Lens.Dutch = Mathf.Lerp(playerCam.m_Lens.Dutch, moveController.GetMoveInput().x * 10, 0.1f);
    }

    // Recommended not to put angle more than 5
    // If you don't want to call in update, just use this function to update angle 
    // and direction vars and update in Update function
    public void Tilt(int angle, int direction)
    {
        playerCam.m_Lens.Dutch = Mathf.Lerp(playerCam.m_Lens.Dutch, angle * -direction, 0.1f);
    }
}