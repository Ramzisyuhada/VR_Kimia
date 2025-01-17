using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRAccelerator.Services;

public class PourController : MonoBehaviour
{

    private ParticleSystem _particleSystem;
    private readonly List<ParticleSystem.Particle> triggerEnterParticles = new List<ParticleSystem.Particle>();

    void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
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
    // Update is called once per frame
    void Update()
    {
        // Contoh pemeriksaan kondisi tertentu untuk memulai atau menghentikan partikel (boleh diaktifkan jika dibutuhkan)
        /*if(Vector3.Angle(Vector3.down,transform.forward) >= 115)
        {
            particle.Play();
        }
        else
        {
            particle.Stop();
        }*/
    }

    private void OnParticleTrigger()
    {
        // Mendapatkan partikel yang masuk ke dalam trigger (Trigger Event Type: Enter)
        int numEnter = _particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, triggerEnterParticles);

        // Menampilkan jumlah partikel yang masuk ke dalam trigger
        Debug.Log("Jumlah partikel yang masuk trigger: " + numEnter);

        // Debug: Menampilkan informasi tentang partikel yang terdeteksi
        foreach (var p in triggerEnterParticles)
        {
            // Tampilkan informasi ID partikel dan posisi
            Debug.Log("Partikel ID: " + p.randomSeed + " Posisi: " + p.position);
        }
    }
}
