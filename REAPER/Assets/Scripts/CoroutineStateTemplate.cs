using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineStateTemplate : MonoBehaviour
{

    public enum State { Idle } // States

    [Header("State ")]
    public State state;

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
        yield return null;
    }

    ////////// UNITY FUNCTIONS //////////
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
