using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerController : MonoBehaviour
{


    [SerializeField] MeshCollider mesh;
    [SerializeField] LiquidContainer container;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("test");
        container.AddLiquid();
    }
}
