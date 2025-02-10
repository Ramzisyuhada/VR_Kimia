using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRAccelerator.Gameplay;

namespace XRAccelerator.Configs
{
    [CreateAssetMenu(fileName = "new Liquid Ingredient Config", menuName = "Configs/Liquid Ingredient", order = 0)]
    public class LiquidIngredientConfig : IngredientConfig
    {
        [SerializeField]
        [Tooltip("The material used by the liquid whilst inside a container.")]
        public Material liquidInsideContainerMaterial;

        public static List<IngredientAmount> GetLiquidIngredientsForVolume(List<IngredientAmount> list, float liquidVolume, float currentLiquidVolume = -1)
        {
            if (currentLiquidVolume < 0)
            {
                currentLiquidVolume = IngredientAmount.TotalListAmount(list);
            }

            var newList = new List<IngredientAmount>();

            foreach (var ingredient in list)
            {
                if (ingredient.Ingredient is LiquidIngredientConfig)
                {
                    newList.Add(new IngredientAmount
                    {
                        Ingredient = ingredient.Ingredient,
                        Amount = liquidVolume * ingredient.Amount / currentLiquidVolume
                    });
                }
            }

            return newList;
        }

        public static LiquidIngredientConfig GetLiquidWithMostVolume(List<IngredientAmount> list)
        {
            LiquidIngredientConfig ingredientWithMostLiquid = null;
            float maxVolume = Mathf.NegativeInfinity;

            foreach (var ingredientAmount in list)
            {
                
                if (ingredientAmount.Ingredient is LiquidIngredientConfig)
                {
                    if (ingredientAmount.Amount > maxVolume)
                    {
                        maxVolume = ingredientAmount.Amount;
                        ingredientWithMostLiquid = (LiquidIngredientConfig)ingredientAmount.Ingredient;
                    }
                }
            }

            return ingredientWithMostLiquid;
        }

        public static LiquidIngredientConfig GetLiquidWithType(List<IngredientAmount> list)
        {
            LiquidIngredientConfig ingredientWithMostLiquid = null;

            foreach (var ingredientAmount in list)
            {
               Debug.Log( ingredientAmount.Ingredient.IngredientTypes);
                if (ingredientAmount.Ingredient is LiquidIngredientConfig)
                {

                    ingredientWithMostLiquid = (LiquidIngredientConfig)ingredientAmount.Ingredient;
                }
            }
            return ingredientWithMostLiquid;
        }


        public static Material GetIngredientMaterialFromList(List<IngredientAmount> list)
        {
            foreach (var ingredientAmount in list)
            {
                if (ingredientAmount.Ingredient is LiquidIngredientConfig liquidIngredient)
                {
                    return liquidIngredient.liquidInsideContainerMaterial;
                }
            }
            return null; // Jika tidak ada material yang ditemukan
        }

    }
}