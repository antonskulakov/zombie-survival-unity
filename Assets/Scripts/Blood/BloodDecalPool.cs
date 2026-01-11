using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // DecalProjector

public enum BloodDecalType { Spray, Smear, Splat }

[System.Serializable]
public struct BloodDecalVariant
{
    public BloodDecalType type;
    public Material material;
    public Vector2 sizeX;   // диапазон размеров X
    public Vector2 sizeY;   // диапазон размеров Y
    public Vector2 sizeZ;   // глубина проекции (Z)
}

public class BloodDecalPool : MonoBehaviour
{
    [Header("Pool")]
    public DecalProjector decalPrefab;
    public int maxActiveDecals = 150;

    [Header("Variants")]
    public BloodDecalVariant[] variants;

    [Header("Spawn")]
    public float surfaceOffset = 0.01f;     // чуть от поверхности, чтобы не z-fight
    public bool randomYaw = true;           // рандом вокруг нормали

    // Можно добавить “исчезновение” по времени, но ты просил базу + лимиты
    // Если захочешь — добавим lifetime + fade.

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
            d.gameObject.SetActive(false);
            _inactive.Push(d);
        }
    }

    public void Spawn(Vector3 position, Vector3 normal, BloodDecalType type)
    {
        var variant = GetVariant(type);
        if (variant.material == null) return;

        var decal = GetDecal();

        // Позиция + небольшой отступ по нормали
        decal.transform.position = position + normal * surfaceOffset;

        // Ориентация: проекция “в поверхность”
        // DecalProjector смотрит вдоль своего forward (Z). Нам надо forward = -normal.
        var rot = Quaternion.LookRotation(-normal, Vector3.up);

        if (randomYaw)
        {
            // Вращаем вокруг нормали (чтобы не было одинаковых паттернов)
            rot = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * rot;
        }

        decal.transform.rotation = rot;

        // Размеры
        float sx = Random.Range(variant.sizeX.x, variant.sizeX.y);
        float sy = Random.Range(variant.sizeY.x, variant.sizeY.y);
        float sz = Random.Range(variant.sizeZ.x, variant.sizeZ.y);
        decal.size = new Vector3(sx, sy, sz);
        

        // Материал формы
        decal.material = variant.material;

        decal.gameObject.SetActive(true);

        // FIFO-учёт активных
        _active.Enqueue(decal);

        // Если превысили лимит — переиспользуем самый старый
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

        // Если пул пуст, переиспользуем самый старый активный
        if (_active.Count > 0)
        {
            var old = _active.Dequeue();
            old.gameObject.SetActive(false);
            return old;
        }

        // fallback: создать (но в идеале не надо)
        var d = Instantiate(decalPrefab, transform);
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

        // если не нашли — вернём пустое
        return default;
    }
}
