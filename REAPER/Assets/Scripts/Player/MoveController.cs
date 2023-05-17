using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;


public class MoveController : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    public Animator anim;
    public Transform cam;

    public enum State { Idle, Running, Dash, InAir, Jumping, Slide, Crouch, Dead } // Player movement state
    [Header("State ")]
    public State state;
    public Vector2 input;
    public Vector3 vel;
    public bool onWall = false;
    public int stamina;


    [FoldoutGroup("Inputs", expanded: false)]
    [SerializeField] bool jumping = false;

    [FoldoutGroup("Inputs", expanded: false)]
    [SerializeField] bool sliding = false;

    [Header("View Settings")]
    public Vector2 sensModifier;
    [SerializeField] float mouseSens;

    [Space(10)]
    [Title("Movement Settings")]
    [Space(10)]


    [Header("Running and Jumping")]
    public float movmentSpeed = 10f;
    [SerializeField] float jumpForce = 15f;
    [SerializeField] Vector2 ledgeDetectionPosition;
    [SerializeField] Vector2 ledgeJumpVelocity;
    float ledgeDetectionDist = 0.8f;

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

    [TitleGroup("Wall Interaction")]
    [Space(10)]
    [SerializeField] Vector3 wallContactPosition;
    [SerializeField] int maxStamina;
    public LayerMask wallRunMask;

    [Header("Wall Jumping")]
    [SerializeField] int maxWallJumps;
    [SerializeField] Vector2 wallJumpForce;
    [SerializeField] float wallSlideFall = -8;

    [Header("Wall Running")]
    [SerializeField] float wallRunTriggerSpeed = 10f;
    [SerializeField] float wallRunSpeed = 12f;




    ////////// INPUTS //////////

    void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        // Handle look input
        Vector2 lookInput = v * mouseSens * Time.deltaTime * sensModifier;
        lookInput.y = cam.transform.localEulerAngles.x - lookInput.y;
        if (lookInput.y > 270)
        {
            lookInput.y = lookInput.y - 360;
        }

        if (lookInput.y > 90f)
        {
            lookInput.y = -90f;
        }
        lookInput.y = Mathf.Clamp(lookInput.y, -90f, 90f);

        cam.transform.localEulerAngles = new Vector3(lookInput.y, 0f, 0f);
        transform.Rotate(Vector3.up * lookInput.x);
    }

    void OnJump(InputValue value)
    {
        jumping = true;
    }

    void OnCrouch(InputValue value)
    {
        sliding = !sliding;
    }

    void OnDash(InputValue value)
    {
        if (stamina > 0)
        {
            state = State.Dash;
            stamina--;
        }
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
            vel.y = rb.velocity.y;
            SetVelocity();

            // Set Stamina to max
            stamina = maxStamina;

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

            // Set Stamina to max
            stamina = maxStamina;

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
        vel.z = input.y * dashForce;
        SetVelocity();

        // Wait dash time
        yield return new WaitForSeconds(dashTime);

        if (IsGrounded()) state = State.Idle;
        else state = State.InAir;

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

            // Set Stamina to max
            stamina = maxStamina;

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

        yield return 0.25f;

        state = State.InAir;

        NextState();
    }

    IEnumerator InAirState()
    {
        /* In Air State
        * Enter State when not grounded
        * Exit state when grounded, or wall running, or jumping
        
        Behavior:
        Fall with gravity
        Move slower in direction of input

        When player is in contact with wall, if jump button is pressed, jump off wall
        Velocity is added perpendicular to wall and in direction of input

        When player is in contact with wall on left or right, and is holding input in direction of wall and forwards
        and player is moving at a certain speed, and player is falling:
        Player will enter wall running state
         
        */

        vel = rb.velocity;
        int wallJumpCount = 0;
        // Enter State
        while (state == State.InAir)
        {
            Vector3 oVel = vel;

            // CAN ADD INPUT CURVE
            float yAccel = input.y * movmentSpeed * airControl * Time.deltaTime;
            float xAccel = input.x * movmentSpeed * airControl * Time.deltaTime;
            // Add input velocity
            vel += transform.forward * yAccel;
            vel += transform.right * xAccel;

            // If starting from less than movement speed, do not allow
            // velocity caused by regular movement to exceed movement speed
            if (Mathf.Abs(new Vector2(oVel.x, oVel.z).magnitude) < movmentSpeed * 1.5f)
            {
                vel.x = Mathf.Clamp(vel.x, -movmentSpeed, movmentSpeed);
                vel.z = Mathf.Clamp(vel.z, -movmentSpeed, movmentSpeed);
            }

            // Clamp all speed to absolute max airspeed
            vel.x = Mathf.Clamp(vel.x, -maxAirSpeed, maxAirSpeed);
            vel.z = Mathf.Clamp(vel.z, -maxAirSpeed, maxAirSpeed);

            // vel.x = Mathf.Clamp(rb.velocity.x * airDrag + input.x * movmentSpeed * airControl * Time.deltaTime, -maxAirSpeed, maxAirSpeed);
            // vel.z = Mathf.Clamp(rb.velocity.z * airDrag * Time.deltaTime + input.y * movmentSpeed * airControl * Time.deltaTime, -maxAirSpeed, maxAirSpeed);

            vel.y = rb.velocity.y;


            // Wall Behaviour
            if (onWall)
            {
                // WALL JUMPING
                // Triggered if player is in contact with wall and jump button is pressed
                if (jumping && wallJumpCount < maxWallJumps)
                {
                    // Calculate direction away from wall
                    Vector3 wallDir = transform.position - wallContactPosition;
                    wallDir.y = 0;

                    // Set velocity
                    vel = wallDir.normalized * wallJumpForce.x;

                    // Add vertical velocity
                    vel.y = jumpForce;

                    wallJumpCount++;
                }

                // LEDGE CLIMBING
                // Trigger if there is a ledge in front of player, player is holding forwards button, and player touching wall
                // Raycast to check if there is a ledge in front of player
                // A ledge exists if there is a wall at ledgeDetectionPosition.y relative to player, but no wall at ledgeDetectionPosition.x
                bool upper = Physics.Raycast(transform.position + transform.up * ledgeDetectionPosition.y, transform.forward, ledgeDetectionDist, wallRunMask);
                bool lower = Physics.Raycast(transform.position + transform.up * ledgeDetectionPosition.x, transform.forward, ledgeDetectionDist, wallRunMask);
                if (upper && !lower)
                {
                    Debug.Log("Player Ledge Jump");
                    // Add vertical velocity
                    vel.y = ledgeJumpVelocity.y;
                }

                // WALL SLIDING/RUNNING

                // Caculate input angle and angle to wall
                float inputAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
                float angleToWall = transform.eulerAngles.y - (Mathf.Atan2(wallContactPosition.z - transform.position.z, wallContactPosition.x - transform.position.x) * Mathf.Rad2Deg);

                // WALL RUNNING AND SLIDING
                // Triggered if player is in contact with wall holding input in wall direction
                if ((Mathf.Clamp(Mathf.Abs(inputAngle - angleToWall), 0, 360) - 180) < 45 && vel.y < 0)
                {
                    // WALL SLIDING
                    // Player slides down at a slower velocity 
                    // When wall sliding, don't use air acceleration 
                    vel = (input.x * transform.right + input.y * transform.forward) * movmentSpeed;

                    // Set velocity
                    vel.y = wallSlideFall;

                    // WALL RUNNING
                    // If player reaches requirements for wall running
                    // Wall Run if moving faster than required distance and pressing wall input
                    if (input.y > 0 && new Vector2(rb.velocity.x, rb.velocity.z).magnitude > wallRunTriggerSpeed)
                    {
                        // VELOCITY CHANGES HERE OVERRIDE ALL OTHER VELOCITY CHANGES MADE PREVIOUSLY
                        Debug.Log("Wall Running");

                        // remove y velocity
                        vel.y = 0;

                        // Add wall run velocity
                        // Calculate wall run direction
                        // Wall run direction is perpendicular direction of wall direction
                        // Vector3 wallRunDir = new Vector3(wallContactPosition.z - transform.position.z, 0, transform.position.x - wallContactPosition.x).normalized;\
                        // nvm might be too complicated


                        // Move at wall run speed
                        vel = (input.x * transform.right + input.y * transform.forward) * wallRunSpeed;
                    }
                }


            }

            // Stomping
            // if (sliding)
            // {
            //     vel.y = -slideFallIncrease;
            // }

            SetActualVelocity();

            yield return null;

            // Check States
            if (IsGrounded())
            {
                print("Player Grounded");
                state = State.Idle;
                break;
            }
        }
        // Exit State
        NextState();
    }

    IEnumerator WallRunState()
    {
        yield return null;
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

    private void LateUpdate()
    {
        // Reset inputs
        jumping = false;
    }
    private void OnCollisionEnter(Collision other)
    {
        // print("Collided with " + other.gameObject.name);
        if (other.gameObject.layer == 7)
        {
            // Collided with wall
            onWall = true;
            wallContactPosition = other.GetContact(0).point;

        }
    }
    private void OnCollisionStay(Collision other)
    {
        // print("Collided with " + other.gameObject.name);
        if (other.gameObject.layer == 7)
        {
            // Collided with wall
            onWall = true;
            wallContactPosition = other.GetContact(0).point;
        }
    }
    private void OnCollisionExit(Collision other)
    {
        // print("Collided with " + other.gameObject.name);
        if (other.gameObject.layer == 7)
        {
            // Collided with wall
            onWall = false;
        }
    }

    private void OnDrawGizmos()
    {

        // GROUND DETECTION RAYCAST
        Gizmos.DrawLine(transform.position, transform.position - transform.up * 1.025f);

        // LEDGE DETECTION RAYCASTS
        Gizmos.DrawLine(transform.position + transform.up * ledgeDetectionPosition.y, transform.position + transform.up * ledgeDetectionPosition.y + transform.forward * ledgeDetectionDist);
        Gizmos.DrawLine(transform.position + transform.up * ledgeDetectionPosition.x, transform.position + transform.up * ledgeDetectionPosition.x + transform.forward * ledgeDetectionDist);

        // WALL HIT POSITION RAYCAST
        if (onWall)
        {
            Gizmos.DrawWireSphere(wallContactPosition, 0.1f);
        }
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

        return Physics.Raycast(transform.position, Vector3.down, 1.025f, layerMask);
    }

    void SetVelocity()
    {
        if (this.enabled)
        {
            rb.velocity = transform.forward * vel.z + transform.right * vel.x + transform.up * vel.y;
        }
    }

    void SetActualVelocity()
    {
        if (this.enabled)
        {
            rb.velocity = vel;
        }
    }
    void AddVelocity()
    {

        rb.velocity += transform.forward * vel.z + transform.right * vel.x + transform.up * vel.y;
    }


}
