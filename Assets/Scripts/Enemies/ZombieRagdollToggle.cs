using System.Collections;
using UnityEngine;

public class ZombieRagdollToggle : MonoBehaviour
{
    [Header("Alive")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    [Header("Ragdoll")]
    [SerializeField] private float ragdollTime = 3f;

    private Rigidbody[] rbs;
    private Collider[] cols;

    private bool _dead;

    private void Awake()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        rbs  = GetComponentsInChildren<Rigidbody>(true);
        cols = GetComponentsInChildren<Collider>(true);

        // На старте держим ragdoll "в ноль" (оптимизация)
        DisableRagdollHardAlive();
    }

    // Вызывай при смерти
    public void PlayDeathRagdoll()
    {
        if (_dead) return;
        _dead = true;

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
        }
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
}
