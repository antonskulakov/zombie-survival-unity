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

/*void OnParticleCollision(GameObject other)
{
    if (BloodDecalPool.Instance == null) return;

    // фильтр по слоям (оставь или убери)
    if (((1 << other.layer) & environmentMask.value) == 0) return;

    int count = _ps.GetCollisionEvents(other, _events);
    for (int i = 0; i < count; i++)
    {
        var e = _events[i];
        BloodDecalPool.Instance.Spawn(e.intersection, e.normal, BloodDecalType.Splat);
    }
}*/

void OnParticleCollision(GameObject other)
{
    Debug.Log($"OnParticleCollision: {other.name} layer={LayerMask.LayerToName(other.layer)}");

    if (BloodDecalPool.Instance == null)
    {
        Debug.LogError("BloodDecalPool.Instance == null (нет пула в сцене)");
        return;
    }

    if (((1 << other.layer) & environmentMask.value) == 0)
    {
        Debug.LogWarning("Hit filtered by environmentMask");
        return;
    }

    int count = _ps.GetCollisionEvents(other, _events);
    Debug.Log($"Collision events: {count}");

    for (int i = 0; i < count; i++)
    {
        var e = _events[i];
        //BloodDecalPool.Instance.Spawn(e.intersection, e.normal, BloodDecalType.Splat);
        BloodDecalPool.Instance.Spawn(e.intersection, e.normal, BloodDecalType.Spray);

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
