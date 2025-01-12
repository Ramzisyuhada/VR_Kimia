using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PourController : MonoBehaviour
{
    
    ParticleSystem particle;
    void Start()
    {
        particle = GetComponent<ParticleSystem>();    
    }

    // Update is called once per frame
    void Update()
    {
         if(Vector3.Angle(Vector3.down,transform.forward) >= 115)
        {
            particle.Play();
        }
        else
        {
            particle.Stop();
        }
    }

    private void OnParticleTrigger()
    {
        Debug.Log("Test");
    }
}
