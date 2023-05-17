using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemy : MonoBehaviour, EnemyInterface
{

    public MeshRenderer meshRenderer;
    public bool destroySelf;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator Hit()
    {
        yield return new WaitForSeconds(0.1f);

        meshRenderer.material.color = Color.gray;
        if (destroySelf)
        {
            Destroy(gameObject);
        }
    }

    public void OnHit(int damage)
    {
        Debug.Log("EnemyInterface.OnHit() called");
        StopAllCoroutines();
        meshRenderer.material.color = Color.red;
        StartCoroutine(Hit());
    }
}
