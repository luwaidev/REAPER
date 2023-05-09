using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;


public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    public Animator anim;
    public Transform cam;

    public enum State { Idle, Running, Dash, InAir, Jumping, Slide, Crouch, Dead } // Player movement state
    [Header("State ")]
    public State state;
    private Vector2 input;
    public Vector3 vel;


    [FoldoutGroup("Inputs", expanded: false)]
    [SerializeField] bool jumping = false;

    [FoldoutGroup("Inputs", expanded: false)]
    [SerializeField] bool sliding = false;

    [Header("View Settings")]
    public Vector2 sensModifier;
    [SerializeField] float mouseSens;

    [Space(10)]
    [Header("Movement Settings")]
    [Space(10)]

    [Header("Running and Jumping")]
    [SerializeField] float movmentSpeed = 10f;
    [SerializeField] float jumpForce = 15f;

    [Header("Sliding and Dashing")]
    [SerializeField] float slideForce = 20f;
    [SerializeField] float dashForce = 30f;
    [SerializeField] float dashTime = 0.2f;

    [Header("Jumping and Air Movement")]
    [SerializeField] float airControl = 0.5f;
    [SerializeField] float airDrag = 0.5f;


    ////////// INPUTS //////////

    void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        // Handle look input
        Vector2 lookInput = value.Get<Vector2>() * mouseSens * Time.deltaTime * sensModifier;
        lookInput.y = cam.transform.localEulerAngles.x - lookInput.y;
        if (lookInput.y > 270)
        {
            lookInput.y = lookInput.y - 360;
        }

        lookInput.y = Mathf.Clamp(lookInput.y, -87f, 90f);
        print(lookInput.y);
        // lookInput.y = Mathf.Clamp(lookInput.y, -90f, 90f);

        cam.transform.localEulerAngles = new Vector3(lookInput.y, 0f, 0f);
        transform.Rotate(Vector3.up * lookInput.x);
    }

    void OnJump(InputValue value)
    {
        jumping = value.isPressed;
    }

    void OnCrouch(InputValue value)
    {
        sliding = !sliding;
    }

    void OnDash(InputValue value)
    {
        state = State.Dash;
    }

    ////////// STATES //////////
    // Takes the current state, and calls the Coroutine for that state
    public void NextState()
    {
        // Get state name
        string methodName = state.ToString() + "State";

        // Get method
        System.Reflection.MethodInfo info =
            GetType().GetMethod(methodName,
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

        StartCoroutine((IEnumerator)info.Invoke(this, null)); // Call the next state
    }

    IEnumerator IdleState()
    {
        // Enter State
        while (state == State.Idle)
        {
            vel.x = 0;
            vel.y = 0;
            SetVelocity();

            yield return null;

            // Check States
            if (input != Vector2.zero)
            {
                state = State.Running;
                break;
            }
            else if (sliding && IsGrounded())
            {
                state = State.Slide;
                break;
            }
            else if (jumping && IsGrounded())
            {
                state = State.Jumping;
                break;
            }
        }
        // Exit State
        NextState();
    }

    IEnumerator RunningState()
    {
        // Enter State
        while (state == State.Running)
        {
            vel.x = input.x * movmentSpeed;
            vel.y = input.y * movmentSpeed;
            vel.z = 0;
            SetVelocity();

            yield return null;

            // Check States
            if (input == Vector2.zero)
            {
                state = State.Idle;
                break;
            }
            if (sliding && IsGrounded())
            {
                state = State.Slide;
                break;
            }
            if (jumping && IsGrounded())
            {
                state = State.Jumping;
                break;
            }
            if (!IsGrounded())
            {
                state = State.InAir;
                break;
            }
        }

        NextState();
    }

    IEnumerator DashState()
    {
        // Set Velocity
        vel.x = input.x * dashForce;

        // Wait dash time
        yield return new WaitForSeconds(dashTime);

        state = State.Idle;

        NextState();
    }

    IEnumerator SlideState()
    {
        // Set Velocity
        vel.x = input.x * slideForce;
        state = State.Idle;

        // Hold until let go of input
        while (sliding)
        {
            vel.x = input.x * rb.velocity.x;
            vel.y = input.y * rb.velocity.z;
            vel.z = rb.velocity.y;

            // Check States
            if (!sliding)
            {
                state = State.Idle;
                break;
            }
            else if (!IsGrounded())
            {
                state = State.InAir;
                break;
            }
            else if (input == Vector2.zero)
            {
                state = State.Crouch;
                break;
            }
            else if (jumping && IsGrounded())
            {
                state = State.Jumping;
                break;
            }
            yield return null;
            SetVelocity();
        }

        NextState();
    }

    IEnumerator CrouchState()
    {

        // Hold until let go of input
        while (state == State.Crouch)
        {
            vel.x = 0;
            vel.y = 0;
            vel.z = 0;

            // Check States
            if (!sliding)
            {
                state = State.Idle;
                break;
            }
            else if (!IsGrounded())
            {
                state = State.InAir;
                break;
            }
            yield return null;
            SetVelocity();
        }
        NextState();
    }

    IEnumerator JumpingState()
    {
        // Set Velocity
        vel.x = input.x * movmentSpeed;
        vel.y = input.y * movmentSpeed;
        vel.z = jumpForce;
        SetVelocity();

        yield return null;

        state = State.InAir;

        NextState();
    }

    IEnumerator InAirState()
    {
        // Enter State
        while (state == State.InAir)
        {
            vel.x = input.x * movmentSpeed * airControl;
            vel.y = input.y * movmentSpeed * airControl;
            vel.z = rb.velocity.y;
            SetVelocity();

            yield return null;

            // Check States
            if (IsGrounded())
            {
                state = State.Idle;
                break;
            }
        }
        // Exit State
        NextState();
    }
    ////////// UNITY FUNCTIONS //////////
    // Start is called before the first frame update
    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        NextState();
    }

    // Update is called once per frame
    void Update()
    {
    }

    ////////// Functions //////////
    void SetReferences()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        cam = GetComponentInChildren<Camera>().transform;
    }

    void MovementStateChecks()
    {
        if (input != Vector2.zero)
        {
            state = State.Running;
        }
        else if (sliding && IsGrounded())
        {
            state = State.Slide;
        }
        else if (jumping && IsGrounded())
        {
            state = State.Jumping;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    void SetVelocity()
    {

        rb.velocity = transform.forward * vel.y + transform.right * vel.x + transform.up * vel.z;
    }
}
