using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BloodDecalsFromParticles : MonoBehaviour
{
    BloodDecalPool Pool => BloodDecalPool.Instance;
    public LayerMask environmentMask;

    [Header("Limits")]
    [Range(0f, 1f)] public float spawnChancePerHit = 0.35f; // не каждое столкновение
    public int maxDecalsPerCollisionMessage = 4;            // ограничим спавн за вызов

    [Header("Type mix")]
    [Range(0f, 1f)] public float sprayChance = 0.15f;
    [Range(0f, 1f)] public float smearChance = 0.10f;

    private ParticleSystem _ps;
    private readonly List<ParticleCollisionEvent> _events = new();

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
   
         if (BloodDecalPool.Instance == null) return;
         int count = _ps.GetCollisionEvents(other, _events);
        if (count <= 0) return;

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (spawned >= maxDecalsPerCollisionMessage) break;
            if (Random.value > spawnChancePerHit) continue;

            var e = _events[i];

            // Доп. защита: спавним только если реально попали в окружение
            if (((1 << other.layer) & environmentMask.value) == 0) continue;

            var type = PickType();
            BloodDecalPool.Instance.Spawn(e.intersection, e.normal, type);

            spawned++;
        }
    }

    private BloodDecalType PickType()
    {
        float r = Random.value;
        if (r < smearChance) return BloodDecalType.Smear;
        if (r < smearChance + sprayChance) return BloodDecalType.Spray;
        return BloodDecalType.Splat;
    }
}
