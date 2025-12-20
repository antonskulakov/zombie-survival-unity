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
        Debug.Log($"H={Input.GetAxisRaw("Horizontal")} V={Input.GetAxisRaw("Vertical")}");

        // если хочешь стопорить движение во время атаки/хита — включи флаги
        if (_isHitLock) { SetSpeedAnim(0f); ApplyGravityOnly(); return; }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, 0f, z);
        float mag = Mathf.Clamp01(move.magnitude);
        if (mag > 0.001f) move.Normalize();

        // поворот по направлению движения
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

if (blockMoveWhileAttacking && IsInAttack())
        return;
  
        // движение
        if (!(blockMoveWhileAttacking && Time.time < _nextAttackTime - (attackCooldown * 0.9f)))
        {
            _cc.Move(move * moveSpeed * Time.deltaTime);
        }

        // анимация скорости (0..1)
        SetSpeedAnim(mag);

        // гравитация
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
        // Для MVP: Space или ЛКМ
        bool attackPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!attackPressed) return;
        if (Time.time < _nextAttackTime) return;

        // выбрать случайную анимацию атаки (0..2)
        int idx = 0; //Random.Range(0, 3);
        animator.SetInteger(attackIndexParam, idx);
        animator.SetTrigger(attackTrigger);

        // урон — для MVP сразу при нажатии.
        // Потом сделаем Animation Event "DealDamage" в нужном кадре.
        //DealMeleeDamage();

        _nextAttackTime = Time.time + attackCooldown;
    }

    private void DealMeleeDamage()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hp = hits[i].GetComponent<Health>();
            if (hp) hp.TakeDamage(damage);
        }
    }

    // Вызывать когда зомби укусил:
    public void OnHit()
    {
        if (animator) animator.SetTrigger(hitTrigger);
        // короткая блокировка управления (подстроишь)
        if (gameObject.activeInHierarchy) StartCoroutine(HitLockRoutine(0.2f));
    }

    private System.Collections.IEnumerator HitLockRoutine(float t)
    {
        _isHitLock = true;
        yield return new WaitForSeconds(t);
        _isHitLock = false;
    }

}
