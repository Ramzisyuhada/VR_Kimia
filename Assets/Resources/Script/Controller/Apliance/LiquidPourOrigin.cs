using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using XRAccelerator.Configs;
using XRAccelerator.Services;

namespace XRAccelerator.Gameplay
{
    public class LiquidPourOrigin : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The container from where the liquid will pour")]
        private float liquidVolumePerParticle = 20;

        [HideInInspector]  public List<IngredientAmount> pouringIngredients;
        private float currentLiquidVolume;
        private float particlesRemovedFromCollision;

        private int particlesRemainingToSpawn;
        private bool isPouringActive;

        public ParticleSystem _particleSystem;
        private ParticleSystem.EmissionModule emission;
        private ParticleSystem.MinMaxCurve emissionPerTime;

        private readonly Collider[] sphereCastColliders = new Collider[30];
        private readonly List<ParticleSystem.Particle> triggerEnterParticles = new List<ParticleSystem.Particle>();
        private ParticleSystem.Particle[] particles;
        private HashSet<uint> aliveParticles;

        private LiquidContainer trackedLiquidContainer;
        private Transform _transform;
        List<IngredientAmount> addedIngredients;

        public void AddIngredientsToPour(List<IngredientAmount> newIngredientsToPour)
        {
            addedIngredients = newIngredientsToPour;
            IngredientAmount.AddToIngredientsList(pouringIngredients, newIngredientsToPour);
            var addedLiquidVolume = IngredientAmount.TotalListAmount(newIngredientsToPour);
            currentLiquidVolume += addedLiquidVolume;
            particlesRemainingToSpawn = Mathf.CeilToInt((currentLiquidVolume / liquidVolumePerParticle) - aliveParticles.Count);

            // TODO Arthur: Change particles color based on ingredients
            emission.rateOverTime = emissionPerTime;
            isPouringActive = true;

            if (!_particleSystem.isPlaying)
            {
                _particleSystem.Play();
            }
        }

        public void EndPour()
        {
            emission.rateOverTime = 0;
            isPouringActive = false;
        }

        public void TrackContainer(LiquidContainer liquidContainer)
        {
            trackedLiquidContainer = liquidContainer;
        }

        public void RegisterParticleColliders(Collider selfCollider = null)
        {
            var registeredColliders = ServiceLocator.GetService<ComponentReferencesProvider>().registeredColliders;
            var skippedColliderOffset = 0;
            for (var index = 0; index < registeredColliders.Count; index++)
            {
                var newCollider = registeredColliders[index];
                if (newCollider == selfCollider)
                {
                    skippedColliderOffset = -1;
                    continue;
                }

                _particleSystem.trigger.SetCollider(index + skippedColliderOffset, newCollider);
            }
        }

        private void RemoveLiquidVolume(float liquidVolume)
        {
            if (liquidVolume <= 0)
            {
                return;
            }
            foreach (var pouringIngredient in pouringIngredients.ToList())
            {
                pouringIngredient.Amount -= pouringIngredient.Amount * liquidVolume / currentLiquidVolume;

                if (pouringIngredient.Amount <= 0.001f)
                {
                    pouringIngredients.Remove(pouringIngredient);
                }
            }

            currentLiquidVolume -= liquidVolume;
        }

        private int AddNewlyInstantiatedParticles(int numParticlesAlive)
        {
            int newlyAdded = 0;
            for (var index = 0; index < numParticlesAlive; index++)
            {
                var particle = particles[index];
                if (!aliveParticles.Contains(particle.randomSeed))
                {
                    newlyAdded++;
                    aliveParticles.Add(particle.randomSeed);
                }
            }

            return newlyAdded;
        }

        private int RemoveNewlyDeletedParticles(int numParticlesAlive)
        {
            int newlyRemoved = 0;
            foreach (var key in aliveParticles.ToList())
            {
                var particleIndex = GetParticleIndex(key);
                if (particleIndex == -1 || particleIndex >= numParticlesAlive)
                {
                    newlyRemoved++;
                    aliveParticles.Remove(key);
                }
            }
            
            if (aliveParticles.Count == 0 && !isPouringActive)
            {
                _particleSystem.Stop();

            }

            return newlyRemoved;
        }

        private int GetParticleIndex(uint particleSeed)
        {
            for (var index = 0; index < particles.Length; index++)
            {
                var particle = particles[index];
                if (particle.randomSeed == particleSeed)
                {
                    return index;
                }
            }

            return -1;
        }

        public void TrackContainer()
        {
            if (trackedLiquidContainer == null)
            {
                return;
            }

            var newParent = trackedLiquidContainer.GetCurrentPourPoint;
            if (newParent == _transform.parent)
            {
                return;
            }

            _transform.parent = newParent;
            _transform.localPosition = Vector3.zero;
            _transform.localScale = Vector3.one;
            _transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!_particleSystem.isPlaying)
            {
                return;
            }

            // TODO Arthur: Position particle system on lowest container edge
            int numParticlesAlive = _particleSystem.GetParticles(particles);
            var newlyAdded = AddNewlyInstantiatedParticles(numParticlesAlive);

            // Remove particles that ended their lifetime
            var newlyRemoved = RemoveNewlyDeletedParticles(numParticlesAlive);
            var volumeRemoved = Mathf.Min((
                    newlyRemoved - particlesRemovedFromCollision) * liquidVolumePerParticle,
                currentLiquidVolume);
            RemoveLiquidVolume(volumeRemoved);
            particlesRemovedFromCollision = Mathf.Max(particlesRemovedFromCollision - newlyRemoved, 0);

            particlesRemainingToSpawn -= newlyAdded;
            if (particlesRemainingToSpawn <= 0 && isPouringActive)
            {
                EndPour();
            }

            TrackContainer();
        }
        private Breaker breaker;

        private void OnParticleTrigger()
        {
            int numEnter = _particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, triggerEnterParticles);
            var containerCollisions = new Dictionary<ContainerController, int>();
            // Get containers that were hit
            for (int i = 0; i < numEnter; i++)
            {
                ParticleSystem.Particle particle = triggerEnterParticles[i];
                particle.remainingLifetime = 0;
                triggerEnterParticles[i] = particle;

                // TODO Arthur: Get the particle collider radius and substitute the 0.1f
                var layerMask = LayerMask.GetMask("Container");
                var size = Physics.OverlapSphereNonAlloc(particle.position, 0.1f, sphereCastColliders, layerMask, QueryTriggerInteraction.Collide);
                Debug.Assert(size == 1, $"Particle collided but OverlapSphere got {size} hits");

                var container = sphereCastColliders[0].GetComponentInParent<ContainerController>();
                Debug.Assert(container != null, "LiquidContainer has no Container Component");

                containerCollisions[container] =
                    containerCollisions.ContainsKey(container) ? containerCollisions[container] + 1 : 1;

                Debug.Log(sphereCastColliders[0].transform.gameObject.name);

                if (sphereCastColliders[0].transform.GetComponentInParent<Breaker>() != null)
                {

                   if(breaker == null) breaker = sphereCastColliders[0].transform.GetComponentInParent<Breaker>();
                    breaker.IsParticleTrigger = true;

                   breaker.AddIngredients(addedIngredients);
                   



                    // Breaker breaker = sphereCastColliders[0].transform.gameObject.GetComponentInChildren<Breaker>();
                }
            }

            _particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, triggerEnterParticles);
            
            // Add ingredients to hit containers
            foreach (var entry in containerCollisions)
            {
                var volumeRemoved = Mathf.Min(entry.Value * liquidVolumePerParticle, currentLiquidVolume);
                var ingredients = LiquidIngredientConfig.GetLiquidIngredientsForVolume(pouringIngredients, volumeRemoved, currentLiquidVolume);
                RemoveLiquidVolume(volumeRemoved);
                particlesRemovedFromCollision += entry.Value;

                entry.Key.AddLiquidIngredient(ingredients);
            }
        }

        private void Awake()
        {
            pouringIngredients = new List<IngredientAmount>();

            _transform = transform;
            _particleSystem = GetComponent<ParticleSystem>();
            emission = _particleSystem.emission;
            emissionPerTime = emission.rateOverTime;

            EndPour();
            _particleSystem.Stop();

            particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
            aliveParticles = new HashSet<uint>();
        }
    }
}