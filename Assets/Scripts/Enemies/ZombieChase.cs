using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ZombieChase : MonoBehaviour
{
    public Transform target;
    public float speed = 2.2f;
    public float rotateSpeed = 8f;

    [Header("Animation")]
    public string walkStateName = "Walking";
    public int animLayer = 0;

    [Header("Hit Reaction")]
    public string hitTrigger = "Hit";     // Trigger в Animator
    public float hitStunTime = 0.25f;     // сколько стоит после удара

    private CharacterController _cc;
    private Animator _anim;

    private bool _isWalking;
    private bool _walkRandomized;

    private bool _isStunned;
    private Coroutine _stunCo;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStickForce = -2f;

    private float _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_isStunned)
        {
            // стоим, не двигаемся
            StopWalk();
            return;
        }

        if (!target)
        {
            StopWalk();
            return;
        }

        // ===== GRAVITY =====
        if (_cc.isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = groundedStickForce; // прижимаем к земле
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
        {
            StopWalk();
            return;
        }

        StartWalk();

        Vector3 move = dir.normalized;

        Quaternion rot = Quaternion.LookRotation(move, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);

        //_cc.Move(move * speed * Time.deltaTime);
        Vector3 finalMove = move * speed;
        finalMove.y = _verticalVelocity;

        _cc.Move(finalMove * Time.deltaTime);
    }

    // Вызвать при получении урона:
    public void OnHit()
    {
        if (_stunCo != null) StopCoroutine(_stunCo);
        _stunCo = StartCoroutine(StunRoutine(hitStunTime));
    }

    private IEnumerator StunRoutine(float t)
    {
        _isStunned = true;

        // на всякий: чтобы ходьба точно не тянула вперёд
        SetSpeed(0f);

        yield return new WaitForSeconds(t);

        _isStunned = false;
        _stunCo = null;
    }

    // ===== WALK CONTROL =====

    void StartWalk()
    {
        if (_isWalking)
        {
            SetSpeed(1f);
            return;
        }

        _isWalking = true;
        SetSpeed(1f);

        if (_anim && !_walkRandomized)
        {
            float tt = Random.value;
            _anim.Play(walkStateName, animLayer, tt);
            _anim.Update(0f);
            _anim.speed = Random.Range(0.9f, 1.1f);
            _walkRandomized = true;
        }
    }

    void StopWalk()
    {
        if (!_isWalking) return;

        _isWalking = false;
        SetSpeed(0f);
        _walkRandomized = false;
    }

    void SetSpeed(float v)
    {
        if (_anim) _anim.SetFloat("Speed", v);
    }

    
}
