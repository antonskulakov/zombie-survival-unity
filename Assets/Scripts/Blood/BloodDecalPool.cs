using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // DecalProjector

public enum BloodDecalType { Spray, Smear, Splat }

[System.Serializable]
public struct BloodDecalVariant
{
    public BloodDecalType type;
    public Material material;
    public Vector2 sizeX;   // D'D,DøD¨DøDúD_D« ¥?DøDúD¬Dæ¥?D_Dý X
    public Vector2 sizeY;   // D'D,DøD¨DøDúD_D« ¥?DøDúD¬Dæ¥?D_Dý Y
    public Vector2 sizeZ;   // D3D¯¥ŸDñD,D«Dø D¨¥?D_DæD§¥+D,D, (Z)
}

public class BloodDecalPool : MonoBehaviour
{
    [Header("Pool")]
    public DecalProjector decalPrefab;
    public int maxActiveDecals = 150;

    [Header("Variants")]
    public BloodDecalVariant[] variants;

    [Header("Spawn")]
    public float surfaceOffset = 0.01f;     // ¥Ø¥Ÿ¥,¥O D_¥, D¨D_DýDæ¥?¥.D«D_¥?¥,D,, ¥Ø¥,D_Dñ¥< D«Dæ z-fight
    public bool randomYaw = true;           // ¥?DøD«D'D_D¬ DýD_D§¥?¥ŸD3 D«D_¥?D¬DøD¯D,

    [Header("Growth")]
    public bool enableGrowth = true;
    public Vector2 randomScaleMultiplier = new Vector2(0.9f, 1.1f);
    public float growDuration = 0.15f;
    [Range(0.5f, 1f)] public float growStartScale = 0.85f;

    [Header("Merge")]
    public bool enableMerge = true;
    public float mergeRadius = 0.25f;
    [Range(0f, 1f)] public float mergeNormalDotMin = 0.85f;
    public int mergeMinCount = 2;
    public int mergeMaxCount = 4;
    public float mergeSizePerDecal = 0.15f;
    public float mergeMaxScale = 2f;

    // DoD_DD«D_ D'D_DñDøDýD,¥,¥O ƒ?oD,¥?¥ØDæDúD«D_DýDæD«D,Dæƒ?? D¨D_ Dý¥?DæD¬DæD«D,, D«D_ ¥,¥< D¨¥?D_¥?D,D¯ DñDøDú¥Ÿ + D¯D,D¬D,¥,¥<
    // D¥?D¯D, DúDø¥.D_¥ØDæ¥^¥O ƒ?" D'D_DñDøDýD,D¬ lifetime + fade.

    private readonly List<DecalProjector> _active = new();
    private readonly Stack<DecalProjector> _inactive = new();
    private readonly List<int> _mergeIndices = new();
    private readonly List<float> _mergeDistances = new();

    public static BloodDecalPool Instance { get; private set; }

    void Awake()
    {
        Warmup(maxActiveDecals);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Warmup(int count)
    {
        count = Mathf.Max(0, count);
        for (int i = 0; i < count; i++)
        {
            var d = Instantiate(decalPrefab, transform);
            if (enableGrowth) EnsureGrowComponent(d);
            d.gameObject.SetActive(false);
            _inactive.Push(d);
        }
    }

    public void Spawn(Vector3 position, Vector3 normal, BloodDecalType type)
    {
        Spawn(position, normal, type, Vector3.zero, 1f, 1f);
    }

    public void Spawn(Vector3 position, Vector3 normal, BloodDecalType type, Vector3 directionOnPlane, float stretchFactor)
    {
        Spawn(position, normal, type, directionOnPlane, stretchFactor, 1f);
    }

    public void SpawnWithMerge(Vector3 position, Vector3 normal, BloodDecalType type, Vector3 directionOnPlane, float stretchFactor)
    {
        if (TryMergeAndSpawn(position, normal, type, directionOnPlane, stretchFactor)) return;
        Spawn(position, normal, type, directionOnPlane, stretchFactor, 1f);
    }

    public void Spawn(Vector3 position, Vector3 normal, BloodDecalType type, Vector3 directionOnPlane, float stretchFactor, float sizeMultiplier)
    {
        var variant = GetVariant(type);
        if (variant.material == null) return;

        var decal = GetDecal();

        decal.transform.position = position + normal * surfaceOffset;

        var rot = Quaternion.LookRotation(-normal, Vector3.up);

        Vector3 up = Vector3.ProjectOnPlane(directionOnPlane, normal);
        bool hasDirection = up.sqrMagnitude > 0.0001f;
        if (hasDirection)
        {
            rot = Quaternion.LookRotation(-normal, up.normalized);
        }

        if (randomYaw && !hasDirection)
        {
            rot = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * rot;
        }

        decal.transform.rotation = rot;

        float sx = Random.Range(variant.sizeX.x, variant.sizeX.y);
        float sy = Random.Range(variant.sizeY.x, variant.sizeY.y);
        float sz = Random.Range(variant.sizeZ.x, variant.sizeZ.y);
        float scaleMultiplier = Random.Range(randomScaleMultiplier.x, randomScaleMultiplier.y);
        Vector3 targetSize = new Vector3(sx, sy, sz) * scaleMultiplier * sizeMultiplier;
        if (stretchFactor > 1f)
        {
            targetSize = new Vector3(targetSize.x, targetSize.y * stretchFactor, targetSize.z);
        }

        if (enableGrowth && growDuration > 0f && growStartScale > 0f && growStartScale < 1f)
        {
            var grow = EnsureGrowComponent(decal);
            grow.Play(targetSize, growStartScale, growDuration);
        }
        else
        {
            decal.size = targetSize;
        }

        decal.material = variant.material;

        decal.gameObject.SetActive(true);

        _active.Add(decal);

        while (_active.Count > maxActiveDecals)
        {
            var old = _active[0];
            _active.RemoveAt(0);
            Recycle(old);
        }
    }

    private DecalProjector GetDecal()
    {
        if (_inactive.Count > 0)
            return _inactive.Pop();

        // D¥?D¯D, D¨¥ŸD¯ D¨¥Ÿ¥?¥,, D¨Dæ¥?DæD,¥?D¨D_D¯¥ODú¥ŸDæD¬ ¥?DøD¬¥<D1 ¥?¥,Dø¥?¥<D1 DøD§¥,D,DýD«¥<D1
        if (_active.Count > 0)
        {
            var old = _active[0];
            _active.RemoveAt(0);
            old.gameObject.SetActive(false);
            return old;
        }

        // fallback: ¥?D_DúD'Dø¥,¥O (D«D_ Dý D,D'DæDøD¯Dæ D«Dæ D«DøD'D_)
        var d = Instantiate(decalPrefab, transform);
        if (enableGrowth) EnsureGrowComponent(d);
        d.gameObject.SetActive(false);
        return d;
    }

    private void Recycle(DecalProjector decal)
    {
        decal.gameObject.SetActive(false);
        _inactive.Push(decal);
    }

    private BloodDecalVariant GetVariant(BloodDecalType type)
    {
        for (int i = 0; i < variants.Length; i++)
            if (variants[i].type == type)
                return variants[i];

        // Dæ¥?D¯D, D«Dæ D«Dø¥^D¯D, ƒ?" DýDæ¥?D«¥`D¬ D¨¥Ÿ¥?¥,D_Dæ
        return default;
    }

    private static BloodDecalGrow EnsureGrowComponent(DecalProjector decal)
    {
        if (decal.TryGetComponent(out BloodDecalGrow grow)) return grow;
        return decal.gameObject.AddComponent<BloodDecalGrow>();
    }

    private bool TryMergeAndSpawn(Vector3 position, Vector3 normal, BloodDecalType type, Vector3 directionOnPlane, float stretchFactor)
    {
        if (!enableMerge || mergeMinCount <= 0 || mergeMaxCount <= 0) return false;

        float radiusSqr = mergeRadius * mergeRadius;
        _mergeIndices.Clear();
        _mergeDistances.Clear();

        for (int i = 0; i < _active.Count; i++)
        {
            var decal = _active[i];
            if (decal == null || !decal.gameObject.activeInHierarchy) continue;

            Vector3 to = decal.transform.position - position;
            float d = to.sqrMagnitude;
            if (d > radiusSqr) continue;

            float dot = Vector3.Dot(-decal.transform.forward, normal);
            if (dot < mergeNormalDotMin) continue;

            if (_mergeIndices.Count < mergeMaxCount)
            {
                _mergeIndices.Add(i);
                _mergeDistances.Add(d);
            }
            else
            {
                int farIndex = 0;
                float farDist = _mergeDistances[0];
                for (int j = 1; j < _mergeDistances.Count; j++)
                {
                    if (_mergeDistances[j] > farDist)
                    {
                        farDist = _mergeDistances[j];
                        farIndex = j;
                    }
                }

                if (d < farDist)
                {
                    _mergeIndices[farIndex] = i;
                    _mergeDistances[farIndex] = d;
                }
            }
        }

        if (_mergeIndices.Count < mergeMinCount) return false;

        Vector3 avgPos = position;
        for (int i = 0; i < _mergeIndices.Count; i++)
        {
            avgPos += _active[_mergeIndices[i]].transform.position;
        }
        avgPos /= (_mergeIndices.Count + 1);

        _mergeIndices.Sort();
        for (int i = _mergeIndices.Count - 1; i >= 0; i--)
        {
            int idx = _mergeIndices[i];
            if (idx < 0 || idx >= _active.Count) continue;
            var old = _active[idx];
            _active.RemoveAt(idx);
            Recycle(old);
        }

        float sizeMultiplier = Mathf.Min(mergeMaxScale, 1f + _mergeIndices.Count * mergeSizePerDecal);
        Spawn(avgPos, normal, type, directionOnPlane, stretchFactor, sizeMultiplier);
        return true;
    }
}

public class BloodDecalGrow : MonoBehaviour
{
    private DecalProjector _decal;
    private Vector3 _start;
    private Vector3 _target;
    private float _duration;
    private float _elapsed;
    private bool _playing;

    void Awake()
    {
        _decal = GetComponent<DecalProjector>();
    }

    public void Play(Vector3 targetSize, float startScale, float duration)
    {
        if (_decal == null) _decal = GetComponent<DecalProjector>();
        _target = targetSize;
        _start = targetSize * startScale;
        _duration = Mathf.Max(0.0001f, duration);
        _elapsed = 0f;
        _playing = true;
        _decal.size = _start;
    }

    void Update()
    {
        if (!_playing) return;
        if (_decal == null) _decal = GetComponent<DecalProjector>();

        _elapsed += Time.deltaTime;
        float u = Mathf.Clamp01(_elapsed / _duration);
        _decal.size = Vector3.Lerp(_start, _target, u);

        if (u >= 1f) _playing = false;
    }
}
