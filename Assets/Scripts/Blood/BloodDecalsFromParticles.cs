using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BloodDecalsFromParticles : MonoBehaviour
{
    BloodDecalPool Pool => BloodDecalPool.Instance;
    public LayerMask environmentMask;

    [Header("Limits")]
    [Range(0f, 1f)] public float spawnChancePerHit = 0.35f; // D«Dæ D§DøDD'D_Dæ ¥?¥,D_D¯D§D«D_DýDæD«D,Dæ
    public int maxDecalsPerCollisionMessage = 4;            // D_D3¥?DøD«D,¥ØD,D¬ ¥?D¨DøDýD« DúDø Dý¥<DúD_Dý

    [Header("Type mix")]
    [Range(0f, 1f)] public float sprayChance = 0.15f;
    [Range(0f, 1f)] public float smearChance = 0.10f;

    [Header("Strong hit decals")]
    public Transform sourceTransform;
    public float strongHitMinSpeed = 1.5f;
    public float strongHitMaxSpeed = 8f;
    public float strongHitMaxElongation = 1.5f;
    [Range(0f, 1f)] public float floorNormalMin = 0.7f;

    private ParticleSystem _ps;
    private readonly List<ParticleCollisionEvent> _events = new();

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        if (sourceTransform == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) sourceTransform = player.transform;
            else
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null) sourceTransform = pc.transform;
            }
        }
    }

/*void OnParticleCollision(GameObject other)
{
    if (BloodDecalPool.Instance == null) return;

    // ¥,D,D¯¥O¥,¥? D¨D_ ¥?D¯D_¥?D¬ (D_¥?¥,DøDý¥O D,D¯D, ¥ŸDñDæ¥?D,)
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
        if (Pool == null) return;

        if (((1 << other.layer) & environmentMask.value) == 0) return;

        if (maxDecalsPerCollisionMessage <= 0) return;

        int count = _ps.GetCollisionEvents(other, _events);
        if (count == 0) return;

        int spawned = 0;
        int step = Mathf.Max(1, count / maxDecalsPerCollisionMessage);
        int start = Random.Range(0, step);

        for (int i = start; i < count && spawned < maxDecalsPerCollisionMessage; i += step)
        {
            if (Random.value > spawnChancePerHit) continue;

            var e = _events[i];
            float speed = e.velocity.magnitude;
            float t = Mathf.InverseLerp(strongHitMinSpeed, strongHitMaxSpeed, speed);
            bool strongHit = t > 0f;
            Vector3 dirOnPlane = Vector3.zero;
            float stretch = 1f;

            if (strongHit && e.normal.y >= floorNormalMin)
            {
                Vector3 sourcePos = sourceTransform ? sourceTransform.position : _ps.transform.position;
                Vector3 away = e.intersection - sourcePos;
                away = Vector3.ProjectOnPlane(away, e.normal);
                if (away.sqrMagnitude < 0.0001f)
                {
                    away = Vector3.ProjectOnPlane(e.velocity, e.normal);
                    if (away.sqrMagnitude < 0.0001f)
                    {
                        away = Vector3.ProjectOnPlane(_ps.transform.forward, e.normal);
                    }
                }

                dirOnPlane = away;
                stretch = Mathf.Lerp(1f, Mathf.Max(1f, strongHitMaxElongation), t);
            }

            Pool.SpawnWithMerge(e.intersection, e.normal, PickType(), dirOnPlane, stretch);
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
