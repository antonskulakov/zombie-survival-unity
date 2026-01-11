using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform attackPoint;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float rotateSpeed = 12f;
    public float gravity = -20f;

    [Header("Attack")]
    public float attackCooldown = 0.6f;
    public float attackRange = 1.7f;
    public LayerMask enemyMask;
    public int damage = 25;

    [Header("Animation")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string attackIndexParam = "AttackIndex";
    public string hitTrigger = "Hit";
    public bool blockMoveWhileAttacking = false;

    // ===================== BLOOD VFX =====================
    public enum WeaponBloodLevel { Low, Medium, High, Extreme }

    [Header("VFX Blood")]
    public WeaponBloodLevel bloodLevel = WeaponBloodLevel.Medium;

    public GameObject bloodSmallPrefab;
    public GameObject bloodMediumPrefab;
    public GameObject bloodHeavyPrefab;

    [Tooltip("Сдвиг точки крови относительно attackPoint (чтобы не спавнилось в центре игрока).")]
    public Vector3 bloodSpawnOffset = new Vector3(0f, 1.0f, 0.2f);

    [Tooltip("Доп. масштаб крови (общий).")]
    public float bloodScale = 1f;

    [Tooltip("Шанс спавна крови, если удар попал (1 = всегда).")]
    [Range(0f, 1f)] public float bloodSpawnChance = 1f;

    [Tooltip("Если включено — при Extreme добавим второй спавн для 'кровищи'.")]
    public bool doubleSpawnOnExtreme = true;

[Tooltip("Задержка крови после нанесения урона (сек). 0.06-0.12 обычно идеально.")]
public float bloodDelay = 0.08f;

[Tooltip("Небольшой разброс задержки, чтобы выглядело живее.")]
public float bloodDelayJitter = 0.02f;
    // ======================================================

    private CharacterController _cc;
    private Vector3 _velocity;
    private float _nextAttackTime;
    private bool _isHitLock;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!attackPoint) attackPoint = transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private void Update()
    {
        HandleMove();
        HandleAttackInput();
    }

    bool IsInAttack()
    {
        var st = animator.GetCurrentAnimatorStateInfo(0); // 0 = Base Layer
        return st.IsTag("Attack");
    }

    private void HandleMove()
    {
        //Debug.Log($"H={Input.GetAxisRaw("Horizontal")} V={Input.GetAxisRaw("Vertical")}");

        if (_isHitLock) { SetSpeedAnim(0f); ApplyGravityOnly(); return; }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, 0f, z);
        float mag = Mathf.Clamp01(move.magnitude);
        if (mag > 0.001f) move.Normalize();

        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        if (blockMoveWhileAttacking && IsInAttack())
            return;

        if (!(blockMoveWhileAttacking && Time.time < _nextAttackTime - (attackCooldown * 0.9f)))
        {
            _cc.Move(move * moveSpeed * Time.deltaTime);
        }

        SetSpeedAnim(mag);
        ApplyGravityOnly();
    }

    private void ApplyGravityOnly()
    {
        if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    private void SetSpeedAnim(float value01)
    {
        if (animator) animator.SetFloat(speedParam, value01, 0.08f, Time.deltaTime);
    }

    private void HandleAttackInput()
    {
        bool attackPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!attackPressed) return;
        if (Time.time < _nextAttackTime) return;

        int idx = 0; // Random.Range(0, 3);
        animator.SetInteger(attackIndexParam, idx);
        animator.SetTrigger(attackTrigger);

        // MVP: наносим урон сразу.
        //DealMeleeDamage();

        _nextAttackTime = Time.time + attackCooldown;
    }

private void DealMeleeDamage()
{
    Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);

    Vector3 hitDir = transform.forward;
    hitDir.y = 0f;
    if (hitDir.sqrMagnitude < 0.0001f) hitDir = Vector3.forward;

    for (int i = 0; i < hits.Length; i++)
    {
        var col = hits[i];

        // 1) урон сразу
        var hp = col.GetComponent<Health>();
        if (hp) hp.TakeDamage(damage);

        // 2) кровь с задержкой (только если реально попали)
        if (Random.value <= bloodSpawnChance)
        {
            Vector3 targetPoint = col.ClosestPoint(attackPoint.position);

            float delay = Mathf.Max(0f, bloodDelay + Random.Range(-bloodDelayJitter, bloodDelayJitter));
            StartCoroutine(SpawnBloodDelayed(targetPoint, hitDir, delay));
        }
    }
}

private System.Collections.IEnumerator SpawnBloodDelayed(Vector3 hitPoint, Vector3 hitDir, float delay)
{
    yield return new WaitForSeconds(delay);
    SpawnBlood(hitPoint, hitDir);
}


    private void SpawnBlood(Vector3 hitPoint, Vector3 hitDir)
    {
        GameObject prefab = null;

        switch (bloodLevel)
        {
            case WeaponBloodLevel.Low:     prefab = bloodSmallPrefab; break;
            case WeaponBloodLevel.Medium:  prefab = bloodMediumPrefab; break;
            case WeaponBloodLevel.High:
            case WeaponBloodLevel.Extreme: prefab = bloodHeavyPrefab; break;
        }

        if (!prefab) return;

        // поворот по направлению удара
        hitDir.y = 0f;
        if (hitDir.sqrMagnitude < 0.0001f) hitDir = Vector3.forward;

        Quaternion rot = Quaternion.LookRotation(hitDir.normalized, Vector3.up);

        // точка спавна чуть выше, чтобы не “внутри пола/меша”
        Vector3 spawnPos = hitPoint + (attackPoint ? attackPoint.TransformVector(bloodSpawnOffset) : new Vector3(0f, 1f, 0.2f));

        var go = Instantiate(prefab, spawnPos, rot);
        go.transform.localScale *= bloodScale;

        // Extreme = чуть больше "кровищи"
        if (bloodLevel == WeaponBloodLevel.Extreme && doubleSpawnOnExtreme)
        {
            Vector3 extraPos = spawnPos + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
            var go2 = Instantiate(prefab, extraPos, rot);
            go2.transform.localScale *= (bloodScale * 1.15f);
        }
    }

    // Вызывать когда зомби укусил:
    public void OnHit()
    {
        if (animator) animator.SetTrigger(hitTrigger);
        if (gameObject.activeInHierarchy) StartCoroutine(HitLockRoutine(0.2f));
    }

    private System.Collections.IEnumerator HitLockRoutine(float t)
    {
        _isHitLock = true;
        yield return new WaitForSeconds(t);
        _isHitLock = false;
    }
}
