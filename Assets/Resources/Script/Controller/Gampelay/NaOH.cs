using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRAccelerator.Configs;
using XRAccelerator.Gameplay;
namespace CookingSim.Scripts.Gameplay.Appliances
{
    public class NaOH : Appliance
    {

        [SerializeField]
        [Tooltip("LiquidPourOrigin component reference, responsible for the liquid pouring visuals")]
        private LiquidPourOrigin liquidPourOrigin;

        [SerializeField]
        [Tooltip("The liquid ingredient that will be created by the faucet")]
        private LiquidIngredientConfig liquidIngredientConfig;

        private LiquidContainer liquidContainer;

        private void EnableApliance()
        {

            liquidPourOrigin.AddIngredientsToPour(new List<IngredientAmount>
            {
                new IngredientAmount {Ingredient = liquidIngredientConfig, Amount = 1000000000}
            });


        }
        void Start()
        {
            liquidContainer = GetComponentInChildren<LiquidContainer>();

        }
      

        // Update is called once per frame
        void Update()
        {
            if (liquidContainer.spill)
            {
                EnableApliance();

            }
            else
            {

                liquidPourOrigin.EndPour();
            }
        }
    }
}
