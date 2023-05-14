using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatController : MonoBehaviour
{
    public enum State { Idle, Grappling } // States

    [Header("State ")]
    public State state;
    public PlayerController movement;
    public Rigidbody rb;
    public Vector3 vel;

    [Header("Inputs")]
    [SerializeField] bool grapple;

    [Header("Attack")]
    [SerializeField] float attackDamange;

    [Header("Grappling")]
    [SerializeField] Transform grappleObject;
    [SerializeField] float grappleObjSpeed = 10f;
    [SerializeField] float grappleSpeed = 10f;
    [SerializeField] float grappleRange = 100f;
    [SerializeField] float maxGrappleTime = 100f;
    [SerializeField] float grappleCancelDistance = 1f;

    ////////// INPUT //////////


    void OnAttack(InputValue value)
    {
        // Attack
    }

    void OnGrapple(InputValue value)
    {
        // Grapple
        grapple = true;
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
        while (state == State.Idle)
        {
            // Do nothing
            yield return null;
            if (grapple)
            {
                state = State.Grappling;
            }
        }
        NextState();
    }

    IEnumerator AttackState()
    {
        yield return null;
        NextState();
    }

    IEnumerator GrapplingState()
    {
        /* Grapple State
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


        // Move grapple object towards direction
        // Record direction and starting position
        Vector3 start = transform.position;
        Vector3 direction = movement.cam.transform.forward;

        bool found = false;

        // Move grapple object\
        while (Vector3.Distance(start, grappleObject.position) < grappleRange)
        {
            grappleObject.position += direction * grappleObjSpeed * Time.deltaTime;
            yield return null;

            // Check for collision
            // If collision, stop grapple   
            if (Physics.Raycast(grappleObject.position, direction, out RaycastHit hit, grappleObjSpeed * Time.deltaTime))
            {
                // If collision is with player, ignore
                if (hit.collider.gameObject == gameObject)
                {
                    continue;
                }

                // If collision is with enemy, damage enemy
                // if (hit.collider.gameObject.CompareTag("Enemy"))
                // {
                //     hit.collider.gameObject.GetComponent<EnemyController>().Damage(attackDamange);
                // }

                // If collision is with wall, stop grapple
                if (hit.collider.gameObject.CompareTag("Wall"))
                {
                    print("Collided ");
                    grappleObject.position = hit.point;
                    found = true;
                    break;
                }
            }

        }

        // If no grapple position, stop grapple
        if (!found)
        {
            state = State.Idle;
            NextState();
            yield break;
        }

        // THIS WILL RUN IF A GRAPPLE OBJECT IS FOUND 

        // Disable player movement
        movement.enabled = false;

        // Record time
        float time = 0;

        vel = rb.velocity;

        // Move player towards grapple object
        // Once player is close to grapple object, or time is exceeded, stop grapple
        while (!grapple && Vector3.Distance(transform.position, grappleObject.position) > grappleCancelDistance && time < maxGrappleTime)
        {
            print("Grappling");
            // Move player towards grapple object
            vel = Vector3.Lerp(vel, (grappleObject.position - transform.position).normalized * grappleSpeed, 0.125f);
            time += Time.deltaTime;

            rb.velocity = vel;
            yield return null;
        }

        // Enable player movement
        movement.enabled = true;
        state = State.Idle;
        NextState();
    }

    ////////// UNITY FUNCTIONS //////////
    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<PlayerController>();
        NextState();
    }

    // Update is called once per frame
    void Update()
    {
        if (state != State.Grappling)
        {
            grappleObject.position = transform.position;
        }
    }

    void LateUpdate()
    {
        grapple = false;
    }
}
