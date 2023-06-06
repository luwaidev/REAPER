using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRaycasting : MonoBehaviour
{

    [SerializeField] private float range;
    [SerializeField] private LayerMask playerLayer;

    private Interactables currentTarget;
    private Camera mainCamera;

    private Vector3 mouseInput;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // public void OnInteract(InputValue value)
    // {
    //     if (currentTarget != null)
    //     {
    //         currentTarget.OnInteract();
    //     }
    // }

    public void OnLeft()
    {
        if (currentTarget != null)
        {
            currentTarget.OnInteract(0);
        }
    }

    public void OnRight()
    {
        if (currentTarget != null)
        {
            currentTarget.OnInteract(1);
        }
    }

    /**
    public void OnLook(InputValue value)
    {

        mouseInput = value.Get<Vector2>() * new Vector2(Screen.width, Screen.height);

    }
    **/
    private void Update()
    {
        RaycastForInteractables();

    }

    private void RaycastForInteractables()
    {
        RaycastHit hit;


        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out hit, range))
        {
            Interactables interactable = hit.collider.GetComponent<Interactables>();

            if (interactable != null)
            {
                if (interactable == currentTarget)
                {
                    return;
                }
                else if (currentTarget != null)
                {
                    currentTarget.OnEndHover();
                    currentTarget = interactable;
                    currentTarget.OnStartHover();
                    return;
                }
                else
                {
                    currentTarget = interactable;
                    currentTarget.OnStartHover();
                    return;
                }
            }
            else
            {
                if (currentTarget != null)
                {
                    currentTarget.OnEndHover();
                    currentTarget = null;
                    return;
                }
            }
        }
        else
        {
            if (currentTarget != null)
            {
                currentTarget.OnEndHover();
                currentTarget = null;
                return;
            }
        }
    }
}