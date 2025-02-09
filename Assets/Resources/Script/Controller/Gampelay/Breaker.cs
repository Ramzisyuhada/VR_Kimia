using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRAccelerator.Gameplay;

public class Breaker : ContainerController
{
    [Header("Breaker")]
    [Tooltip("Reference to the audioSource component")]
    [SerializeField] AudioSource AudioSource;

    [Tooltip("TO DETECT SUBSTANCE")]
    [HideInInspector] public bool IsParticleTrigger;
    
    protected override void ExecuteRecipe()
    {
        base.ExecuteRecipe();
       IsParticleTrigger = false;
    }


    void Start()
    {
    
    }
    
    // Update is called once per frame
    void Update()
    {
        if (IsParticleTrigger)
        {
/*            Debug.Log(CurrentIngredients.Count);
*/            //
              ExecuteRecipe();

        }
    }
}
