using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRAccelerator.Configs;
using XRAccelerator.Services;

namespace XRAccelerator.Gameplay
{
    public class ContainerController : Appliance
    {


        [SerializeField]
        [Tooltip("LiquidContainer component reference, responsible for the liquid visuals")]
        protected LiquidContainer liquidContainer;
        [SerializeField]
        [Tooltip("LiquidPourOrigin component reference, responsible for the liquid pouring visuals")]
        protected LiquidPourOrigin liquidPourOrigin;
        [SerializeField]    
        [Tooltip("The collider that detects liquid collision and adds it to the container.\nMust be a trigger collider and on Container layer")]
        private Collider liquidCollider;
        [SerializeField]
        [Tooltip("What ingredient to create on a failed activation")]
        private IngredientConfig failedRecipeIngredient;
        [SerializeField]
        [Tooltip("Reference to the grabInteractable component of this container")]
        private XRGrabInteractable grabInteractable;

        protected readonly List<IngredientAmount> CurrentIngredients = new List<IngredientAmount>();
        protected readonly List<IngredientGraphics> CurrentIngredientGraphics = new List<IngredientGraphics>();
        protected RecipeConfig CurrentRecipeConfig;

        [HideInInspector]public  float currentLiquidVolume;
        [HideInInspector] public bool Isfull;
        private  bool isBeingGrabbed;
        LiquidIngredientConfig currentconfig;
        private bool Succes;
        public void AddLiquidIngredient(List<IngredientAmount> addedIngredients )
        {
            //Debug.Log(CurrentIngredients.Count);
             OnIngredientsEnter(addedIngredients);
             var newlyAddedVolume = IngredientAmount.TotalListAmount(addedIngredients);
            currentLiquidVolume += newlyAddedVolume;
            var ingredientWithMostLiquid = LiquidIngredientConfig.GetLiquidWithMostVolume(CurrentIngredients);

            if (CurrentRecipeConfig == null) { 
               liquidContainer.AddLiquid(newlyAddedVolume, ingredientWithMostLiquid.liquidInsideContainerMaterial);
                return;
            }

            liquidContainer.VolumeLiquid(newlyAddedVolume);
        }

        protected virtual void ReactionChemistry()
        {
            
            SetCurrentRecipe();
            if (CurrentRecipeConfig != null) {
                IngredientConfig newIngredientConfig = GetOutputIngredientConfig();

                if (currentconfig != (LiquidIngredientConfig)newIngredientConfig)
                {

                   
                    liquidContainer.SetMaterial(ConvertLiquidIngrident((LiquidIngredientConfig)newIngredientConfig).liquidInsideContainerMaterial);
                    Debug.Log(ConvertLiquidIngrident((LiquidIngredientConfig)newIngredientConfig).liquidInsideContainerMaterial.name);
                    currentconfig = (LiquidIngredientConfig)newIngredientConfig;
                }
                    
                
            }

        }


        protected virtual void ExecuteRecipe()
        {
            if(CurrentIngredients.Count <= 1) return;
            var currentAmount = IngredientAmount.TotalListAmount(CurrentIngredients);
            if (currentAmount == 0)
            {
                return;
            }

            DestroyCurrentIngredients();
            CreateRecipeIngredient(currentAmount);
        }

        private void CreateRecipeIngredient(float amount)
        {
            IngredientConfig newIngredientConfig = GetOutputIngredientConfig();

            if (newIngredientConfig is LiquidIngredientConfig)
            {
                CreateLiquidIngredient((LiquidIngredientConfig)newIngredientConfig, amount);
            }
/*            else
            {
                CreateIngredientGraphics(newIngredientConfig.IngredientPrefab, amount);
            }*/
        }

        public void CreateLiquidIngredient(LiquidIngredientConfig config, float amount)
        {
            AddLiquidIngredient(new List<IngredientAmount>
            {
                new IngredientAmount {Ingredient = config, Amount = amount}
            });
        }

        public LiquidIngredientConfig ConvertLiquidIngrident(LiquidIngredientConfig config)
        {
            return config;
        }

        private void CreateIngredientGraphics(IngredientGraphics prefab, float amount)
        {
            var ingredientGraphics = Instantiate(prefab, transform.position, Quaternion.identity);
            ingredientGraphics.SetCurrentIngredientAmount(amount);
        }

        protected virtual bool WasRecipeSuccessful()
        {
            return CurrentRecipeConfig != null;
        }

        private IngredientConfig GetOutputIngredientConfig()
        {
            if (!WasRecipeSuccessful())
            {
                return failedRecipeIngredient;
            }

            return CurrentRecipeConfig.OutputIngredient;
        }

        public void DestroyCurrentIngredients()
        {
            // Destroy solids
            foreach (var ingredientGraphics in CurrentIngredientGraphics)
            {
                // [XRToolkitWorkaround] XRDirectInteractor for some reason is keeping a reference to the XRGrabInteractable
                // this way we can kill the object and prevent missing references from the colliders access.
                ingredientGraphics.GetComponent<XRGrabInteractable>().colliders.Clear();
                Destroy(ingredientGraphics.gameObject);
            }

            // Empty container
            CurrentRecipeConfig = null;
            currentLiquidVolume = 0;
            liquidContainer.Empty();
            currentconfig = null;
            Succes = false;
            CurrentIngredientGraphics.Clear();
            CurrentIngredients.Clear();
        }

        public virtual void OnIngredientsEnter(List<IngredientAmount> addedIngredients)
        {
            AddIngredients(addedIngredients);
            SetCurrentRecipe();
        }



        protected virtual void OnIngredientsExit(List<IngredientAmount> removedIngredients)
        {
            RemoveIngredients(removedIngredients);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            var ingredientGraphics = other.gameObject.GetComponent<IngredientGraphics>();
            if (ingredientGraphics != null)
            {
                CurrentIngredientGraphics.Add(ingredientGraphics);
                OnIngredientsEnter(ingredientGraphics.CurrentIngredients);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var ingredientGraphics = other.gameObject.GetComponent<IngredientGraphics>();
            if (ingredientGraphics != null)
            {
                CurrentIngredientGraphics.Remove(ingredientGraphics);
                OnIngredientsExit(ingredientGraphics.CurrentIngredients);
            }
        }

        public void Spill(float volumeSpilled)
        {
            List<IngredientAmount> spilledIngredients =
                LiquidIngredientConfig.GetLiquidIngredientsForVolume(CurrentIngredients, volumeSpilled, currentLiquidVolume);
            RemoveIngredients(spilledIngredients);

            liquidPourOrigin.AddIngredientsToPour(spilledIngredients);
        }

        public void AddIngredients(List<IngredientAmount> addedIngredients)
        {
            IngredientAmount.AddToIngredientsList(CurrentIngredients, addedIngredients);
        }

        private void RemoveIngredients(List<IngredientAmount> removedIngredients)
        {
            foreach (var removedIngredient in removedIngredients)
            {

                var oldIngredient = CurrentIngredients.Find(ingredientEntry =>
                    ingredientEntry.Ingredient == removedIngredient.Ingredient);

                Debug.Assert(oldIngredient != null, $"Trying to remove inexistent ingredient: {removedIngredient.Ingredient.name}");

                oldIngredient.Amount -= removedIngredient.Amount;

                if (oldIngredient.Ingredient is LiquidIngredientConfig)
                {
                    currentLiquidVolume -= removedIngredient.Amount;
                }

                if (oldIngredient.Amount <= 0)
                {
                    CurrentIngredients.Remove(oldIngredient);
                }
            }
        }

        protected void ForceIngredientsStayInContainer()
        {
            var transformRef = transform;
            foreach (var ingredientGraphic in CurrentIngredientGraphics)
            {
                ingredientGraphic.transform.parent = transformRef;
                ingredientGraphic.GetComponent<Rigidbody>().isKinematic = true;
            }
        }

        protected IEnumerator DisableForceIngredientsStayInContainer(float waitTime = 0.1f)
        {
            // Wait reposition or whatever finish before disabling
            yield return new WaitForSeconds(waitTime);

            foreach (var ingredientGraphic in CurrentIngredientGraphics)
            {
                ingredientGraphic.transform.parent = null;
                var body = ingredientGraphic.GetComponent<Rigidbody>();
                body.position = transform.position;
                body.isKinematic = false;
            }
        }

        private void OnRigStartLocomotion(LocomotionSystem locomotionSystem)
        {
            if (isBeingGrabbed)
            {
                ForceIngredientsStayInContainer();
            }
        }

        private void OnRigEndLocomotion(LocomotionSystem locomotionSystem)
        {
            if (isBeingGrabbed)
            {
                StartCoroutine(DisableForceIngredientsStayInContainer());
            }
        }

        private void OnGrab(XRBaseInteractor interactor)
        {
            isBeingGrabbed = true;
            ForceIngredientsStayInContainer();
            StartCoroutine(DisableForceIngredientsStayInContainer());
        }

        private void OnGrabRelease(XRBaseInteractor interactor)
        {
            isBeingGrabbed = false;
        }

        public void SetCurrentRecipe()
        {
            CurrentRecipeConfig = GetRecipeForIngredients(CurrentIngredients);
        }

        private void Start()
        {
            liquidPourOrigin.RegisterParticleColliders(liquidCollider);
            liquidPourOrigin.TrackContainer(liquidContainer);
        }

        private void Update()
        {
          //  ForceIngredientsStayInContainer();
        }
        protected override void Awake()
        {
            base.Awake();

            liquidContainer.Spilled += Spill;

            ServiceLocator.GetService<ComponentReferencesProvider>().RegisterContainerCollider(liquidCollider);

            var referencesProvider = ServiceLocator.GetService<ComponentReferencesProvider>();
            foreach (var locomotionProvider in referencesProvider.registeredLocomotionProviders)
            {
                locomotionProvider.beginLocomotion += OnRigStartLocomotion;
                locomotionProvider.endLocomotion += OnRigEndLocomotion;
            }

            if (grabInteractable != null)
            {
                grabInteractable.onSelectEntered.AddListener(OnGrab);
                grabInteractable.onSelectExited.AddListener(OnGrabRelease);
            }
        }
    }
}
