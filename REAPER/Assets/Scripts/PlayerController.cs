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
    [SerializeField] float slideSpeed = 20f;
    [SerializeField] float slideDecay = 0.1f;
    [SerializeField] float slideFallIncrease = 5f;
    [SerializeField] float slideMovementMultiplier = 0.5f;
    [SerializeField] float slideFallMultiplier = 1.5f;
    [SerializeField] float dashForce = 30f;
    [SerializeField] float dashTime = 0.2f;

    [Header("Jumping and Air Movement")]
    [SerializeField] float airControl = 0.5f;
    [SerializeField] float maxAirSpeed = 10f;
    [SerializeField] float airDrag = 0.5f;

    [Header("Wall Running")]
    [SerializeField] float wallRunSpeed = 10f;
    [SerializeField] BoxCollider wallRunColliderLeft;
    [SerializeField] BoxCollider wallRunColliderRight;


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
            vel.z = 0;
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
            vel.z = input.y * movmentSpeed;
            vel.y = 0;
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
        vel.z = input.x * dashForce;

        // Wait dash time
        yield return new WaitForSeconds(dashTime);

        state = State.Idle;

        NextState();
    }

    IEnumerator SlideState()
    {
        /* Slide State
        * Enter State when grounded and holding sliding input
        * Exit state when let go of input or jumping or attacking
        
        Behavior:
        Slides in direction facing when entering state
        Slide velocity decays over time

        Positive y velocity (Going up) decreases slide velocity
        Negative y velocity (Falling) increases slide velocity

        Moves slowly in direction of input
        */

        print("Player State: Slide");

        // Record Direction
        Vector3 slideDir = transform.forward;
        slideDir.y = 0;

        // Set Velocity
        float slideVel = slideSpeed * input.y;

        // Hold until conditions
        while (state == State.Slide)
        {
            // Set Velocity
            // Change slide velocity depending on y velocity
            if (IsGrounded()) slideVel = Mathf.Max(slideVel - rb.velocity.y * slideFallMultiplier * Time.deltaTime, 0);

            // Decay slide velocity
            slideVel = Mathf.Lerp(slideVel, 0, slideDecay);

            // Set velocity
            rb.velocity = slideVel * slideDir + Vector3.up * rb.velocity.y;

            // Move slowly in direction of input
            vel.x = input.x * movmentSpeed * slideMovementMultiplier;
            vel.z = input.y * movmentSpeed * slideMovementMultiplier;
            rb.velocity += transform.forward * vel.z + transform.right * vel.x;


            rb.velocity -= Vector3.up * slideFallIncrease * Time.deltaTime;

            // Check States
            if (!sliding)
            {
                state = State.Idle;
                break;
            }
            else if (jumping && IsGrounded())
            {
                state = State.Jumping;
                break;
            }
            yield return null;
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
        vel.z = input.y * movmentSpeed;
        vel.y = jumpForce;
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

            // Decay velocity
            vel.x = Mathf.Lerp(vel.x, 0, airDrag);
            vel.z = Mathf.Lerp(vel.z, 0, airDrag);
            // vel.x = Mathf.Clamp(rb.velocity.x * airDrag + input.x * movmentSpeed * airControl * Time.deltaTime, -maxAirSpeed, maxAirSpeed);
            // vel.z = Mathf.Clamp(rb.velocity.z * airDrag * Time.deltaTime + input.y * movmentSpeed * airControl * Time.deltaTime, -maxAirSpeed, maxAirSpeed);
            if (input.x != 0) vel.x = input.x * movmentSpeed;
            if (input.y != 0) vel.z = input.y * movmentSpeed;

            vel.y = rb.velocity.y;

            // Check if 

            if (sliding)
            {
                vel.y = -slideFallIncrease;
            }

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
        // Stolen from raycast unity example
        // Bit shift the index of the layer (6) to get a bit mask (Player layer)
        int layerMask = 1 << 6;

        // This would cast rays only against colliders in layer 6.
        // But instead we want to collide against everything except layer 6. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        return Physics.Raycast(transform.position, Vector3.down, 1.05f, layerMask);
    }

    void SetVelocity()
    {

        rb.velocity = transform.forward * vel.z + transform.right * vel.x + transform.up * vel.y;
    }
    void AddVelocity()
    {

        rb.velocity += transform.forward * vel.z + transform.right * vel.x + transform.up * vel.y;
    }
}
