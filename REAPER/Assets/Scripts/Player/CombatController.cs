using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class CombatController : MonoBehaviour
{
    public enum State { Idle, Attack, Grappling } // States

    [Header("State ")]
    public State state;
    public MoveController movement;
    public Rigidbody rb;
    public Vector3 vel;

    [Header("Inputs")]
    [SerializeField] bool attack;
    [SerializeField] bool grapple;


    [Header("Attack")]
    [SerializeField] float attackVelocity;
    [SerializeField] float attackTime;
    [SerializeField] int attackDamage;
    [SerializeField] float attackRange;
    [SerializeField] LayerMask enemyLayer;

    [Header("Grappling")]
    [SerializeField] Transform grappleObject;
    [SerializeField] LineRenderer grappleLine;
    [SerializeField] float grappleAirControl = 0.2f;
    [SerializeField] float grappleObjSpeed = 10f;
    [SerializeField] float grapplePullSpeed = 10f;
    [SerializeField] Vector2 startingPullSpeed = new Vector2(10f, 10f);
    [SerializeField] float grapplePullLerp = 0.125f;
    [SerializeField] float grappleRange = 100f;
    [SerializeField] float grappleCancelDistance = 1f;

    [Header("Spring Joint Settings")]
    [SerializeField] Vector2 sprintJointMinMaxDistance = new Vector2(0.25f, 0.7f);
    [SerializeField] float springJointSpring = 4.5f;
    [SerializeField] float springJointDamper = 7f;
    [SerializeField] float springJointMassScale = 4.5f;

    ////////// INPUT //////////


    void OnAttack(InputValue value)
    {
        // Attack
        attack = true;
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
            if (attack)
            {
                state = State.Attack;
            }
        }

        NextState();
    }

    IEnumerator AttackState()
    {
        /* Attack State
         * Enter State when idling and attack button is pressed
         * Exit state when attack is finished
         * 
         * Behavior:
         * Player moves forward at attackVelocity for attackTime
         * Player deals attackDamage to enemy if enemy is hit
         * 
         * 
         */

        // Record time
        float time = 0;
        while (time < attackTime)
        {
            // Move player forward
            rb.velocity = movement.cam.transform.forward * attackVelocity;

            // Check for collision
            RaycastHit hit;
            if (Physics.Raycast(movement.cam.transform.position, movement.cam.transform.forward, out hit, attackRange, enemyLayer))
            {
                // If collision is with enemy, damage enemy
                hit.collider.gameObject.GetComponent<EnemyInterface>().OnHit(attackDamage);
            }

            // ADD COMBOS HERE
            time += Time.deltaTime;
            yield return null;
        }

        state = State.Idle;
        NextState();
    }

    IEnumerator GrapplingState()
    {
        /* Grapple State
        * Enter State when idling and grapple button is pressed
        * Exit state when grapple doesn't hit anything, or player is close to grapple object, or time is exceeded
        
        Behavior:
        grapple object moves towards direction looking when entering state
        The grapple object stops when it hits a wall
        if grapple object hits enemy, damage enemy

        If grapple object does not hit a wall within range, stop grapple, exit state
        Otherwise continue

        Then, player moves towards grapple object
        TODO CONTINUE
         
        */


        // Move grapple object towards direction
        // Record direction and starting position
        Vector3 start = transform.position;
        Vector3 direction = movement.cam.transform.forward;

        bool found = false;

        grappleObject.position = movement.cam.transform.position;
        // Move grapple object\
        while (Vector3.Distance(start, grappleObject.position) < grappleRange)
        {
            grappleObject.position += direction * grappleObjSpeed * Time.deltaTime;

            // Set line renderer to grapple object position
            grappleLine.SetPosition(0, transform.position);
            grappleLine.SetPosition(1, grappleObject.position);

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
            // Hide grapple line
            grappleLine.SetPosition(0, Vector3.zero);
            grappleLine.SetPosition(1, Vector3.zero);

            state = State.Idle;
            NextState();
            yield break;
        }

        // THIS WILL RUN IF A GRAPPLE OBJECT IS FOUND 

        // Disable player movement
        movement.enabled = false;

        vel = rb.velocity;

        Vector3 grappleDir = (grappleObject.position - transform.position).normalized;
        float grappleDist = Vector3.Distance(grappleObject.position, transform.position);

        // Add starting pull speed
        rb.velocity += new Vector3(grappleDir.x, 0, grappleDir.z).normalized * startingPullSpeed.x + Vector3.up * startingPullSpeed.y;

        // Set spring joint object
        SpringJoint joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grappleObject.position;
        joint.anchor = new Vector3(0, -0.3f, 0);

        joint.maxDistance = grappleDist * sprintJointMinMaxDistance.y;
        joint.minDistance = grappleDist * sprintJointMinMaxDistance.x;

        joint.spring = springJointSpring;
        joint.damper = springJointDamper;
        joint.massScale = springJointMassScale;

        // Set movement state to in air
        movement.state = MoveController.State.InAir;

        // Move player towards grapple object
        while (!grapple)
        {
            // Set line renderer to grapple object position
            grappleLine.SetPosition(0, transform.position);
            grappleLine.SetPosition(1, grappleObject.position);

            yield return null;
        }

        // Conserve Velocity
        // Set player velocity to grapple velocity
        movement.vel = rb.velocity;

        // Destroy spring joint
        Destroy(joint);

        // Hide grapple line
        grappleLine.SetPosition(0, Vector3.zero);
        grappleLine.SetPosition(1, Vector3.zero);

        yield return new WaitForSeconds(0.1f);

        // Enable player movement
        movement.enabled = true;
        state = State.Idle;
        NextState();
    }

    ////////// UNITY FUNCTIONS //////////
    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<MoveController>();
        NextState();
    }

    // Update is called once per frame
    void Update()
    {
        if (state != State.Grappling)
        {
            grappleObject.position = movement.cam.transform.position;
        }
    }

    void LateUpdate()
    {
        grapple = false;
        attack = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(movement.cam.transform.position, movement.cam.transform.forward * attackRange);
    }
}
