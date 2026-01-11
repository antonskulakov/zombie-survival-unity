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

    // DoD_DD«D_ D'D_DñDøDýD,¥,¥O ƒ?oD,¥?¥ØDæDúD«D_DýDæD«D,Dæƒ?? D¨D_ Dý¥?DæD¬DæD«D,, D«D_ ¥,¥< D¨¥?D_¥?D,D¯ DñDøDú¥Ÿ + D¯D,D¬D,¥,¥<
    // D¥?D¯D, DúDø¥.D_¥ØDæ¥^¥O ƒ?" D'D_DñDøDýD,D¬ lifetime + fade.

    private readonly Queue<DecalProjector> _active = new();
    private readonly Stack<DecalProjector> _inactive = new();

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
        var variant = GetVariant(type);
        if (variant.material == null) return;

        var decal = GetDecal();

        // DYD_DúD,¥+D,¥? + D«DæDñD_D¯¥O¥^D_D1 D_¥,¥?¥,¥ŸD¨ D¨D_ D«D_¥?D¬DøD¯D,
        decal.transform.position = position + normal * surfaceOffset;

        // Dz¥?D,DæD«¥,Dø¥+D,¥?: D¨¥?D_DæD§¥+D,¥? ƒ?oDý D¨D_DýDæ¥?¥.D«D_¥?¥,¥Oƒ??
        // DecalProjector ¥?D¬D_¥,¥?D,¥, DýD'D_D¯¥O ¥?DýD_DæD3D_ forward (Z). D?DøD¬ D«DøD'D_ forward = -normal.
        var rot = Quaternion.LookRotation(-normal, Vector3.up);

        if (randomYaw)
        {
            // D'¥?Dø¥%DøDæD¬ DýD_D§¥?¥ŸD3 D«D_¥?D¬DøD¯D, (¥Ø¥,D_Dñ¥< D«Dæ Dñ¥<D¯D_ D_D'D,D«DøD§D_Dý¥<¥. D¨Dø¥,¥,Dæ¥?D«D_Dý)
            rot = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * rot;
        }

        decal.transform.rotation = rot;

        // DÿDøDúD¬Dæ¥?¥<
        float sx = Random.Range(variant.sizeX.x, variant.sizeX.y);
        float sy = Random.Range(variant.sizeY.x, variant.sizeY.y);
        float sz = Random.Range(variant.sizeZ.x, variant.sizeZ.y);
        float scaleMultiplier = Random.Range(randomScaleMultiplier.x, randomScaleMultiplier.y);
        Vector3 targetSize = new Vector3(sx, sy, sz) * scaleMultiplier;

        if (enableGrowth && growDuration > 0f && growStartScale > 0f && growStartScale < 1f)
        {
            var grow = EnsureGrowComponent(decal);
            grow.Play(targetSize, growStartScale, growDuration);
        }
        else
        {
            decal.size = targetSize;
        }

        // DoDø¥,Dæ¥?D,DøD¯ ¥,D_¥?D¬¥<
        decal.material = variant.material;

        decal.gameObject.SetActive(true);

        // FIFO-¥Ÿ¥Ø¥`¥, DøD§¥,D,DýD«¥<¥.
        _active.Enqueue(decal);

        // D¥?D¯D, D¨¥?DæDý¥<¥?D,D¯D, D¯D,D¬D,¥, ƒ?" D¨Dæ¥?DæD,¥?D¨D_D¯¥ODú¥ŸDæD¬ ¥?DøD¬¥<D1 ¥?¥,Dø¥?¥<D1
        while (_active.Count > maxActiveDecals)
        {
            var old = _active.Dequeue();
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
            var old = _active.Dequeue();
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
