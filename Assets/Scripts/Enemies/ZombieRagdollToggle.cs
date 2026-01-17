using System.Collections;
using UnityEngine;

public class ZombieRagdollToggle : MonoBehaviour
{
    [Header("Alive")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    [Header("Ragdoll")]
    [SerializeField] private float ragdollTime = 3f;

    [Header("Death Impulse")]
    [SerializeField] private float deathImpulse = 2.5f;
    [SerializeField] private float deathImpulseUp = 1.0f;
    [SerializeField] private Rigidbody impulseTarget;

    private Rigidbody[] rbs;
    private Collider[] cols;

    private bool _dead;
    private Vector3 _pendingImpulseDir;

    [Header("Layers")]
    [SerializeField] private string ragdollLayerName = "EnemyRagdoll";
    private int _ragdollLayer;

    private void Awake()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        rbs  = GetComponentsInChildren<Rigidbody>(true);
        cols = GetComponentsInChildren<Collider>(true);

        // На старте держим ragdoll "в ноль" (оптимизация)
        DisableRagdollHardAlive();
        _ragdollLayer = LayerMask.NameToLayer(ragdollLayerName);
    }

    // Вызывай при смерти
    public void PlayDeathRagdoll()
    {
        PlayDeathRagdoll(transform.forward);
    }

    public void PlayDeathRagdoll(Vector3 hitDir)
    {
        if (_dead) return;
        _dead = true;

        _pendingImpulseDir = hitDir;
        StopAllCoroutines();
        StartCoroutine(RagdollRoutine());
    }

    private IEnumerator RagdollRoutine()
    {
        EnableRagdollDeath();
        yield return new WaitForSeconds(ragdollTime);

        // ВАЖНО: не возвращаемся в "живое" состояние.
        FreezeCorpse();
    }

    // ---------- States ----------

    // Alive state (до смерти): ragdoll полностью выключен
    private void DisableRagdollHardAlive()
    {
        if (animator) animator.enabled = true;
        if (characterController) characterController.enabled = true;

        foreach (var rb in rbs)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var c in cols)
        {
            // CharacterController не Collider, но на всякий случай
            if (c is CharacterController) continue;
            c.enabled = false;
        }
    }

    // Death state: включаем физику
    private void EnableRagdollDeath()
    {
        if (animator) animator.enabled = false;
        if (characterController) characterController.enabled = false;

        foreach (var rb in rbs)
        {
            rb.isKinematic = false;          // <-- вот это должно стать false
            rb.detectCollisions = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        foreach (var c in cols)
        {
            if (c is CharacterController) continue;
            c.enabled = true;
            if (_ragdollLayer != -1)
            c.gameObject.layer = _ragdollLayer;
        }

        ApplyDeathImpulse();
    }

    // Corpse state: после 3 сек выключаем физику/коллизии (макс оптимизация)
    private void FreezeCorpse()
    {
        foreach (var rb in rbs)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var c in cols)
        {
            if (c is CharacterController) continue;
            c.enabled = false;
        }
    }

    private void ApplyDeathImpulse()
    {
        if (deathImpulse <= 0f) return;

        Vector3 dir = _pendingImpulseDir;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        Vector3 impulse = (dir * deathImpulse) + (Vector3.up * deathImpulseUp);

        Rigidbody target = impulseTarget;
        if (!target)
        {
            float maxMass = -1f;
            for (int i = 0; i < rbs.Length; i++)
            {
                if (!rbs[i]) continue;
                if (rbs[i].mass > maxMass)
                {
                    maxMass = rbs[i].mass;
                    target = rbs[i];
                }
            }
        }

        if (target) target.AddForce(impulse, ForceMode.Impulse);
    }
}
