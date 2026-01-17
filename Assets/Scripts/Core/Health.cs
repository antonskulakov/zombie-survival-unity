using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHp = 100;
    public int hp;

    public Animator animator;
    public bool destroyOnDeath = true;

    private bool _dead;

    private void Awake()
    {
        hp = maxHp;
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public void TakeDamage(int amount, Vector3 hitDir)
    {
        if (_dead) return;

        hp -= amount;
        if (hp <= 0)
        {
            _dead = true;

            // выключаем AI/движение
            var chase = GetComponent<ZombieChase>();
            if (chase) chase.enabled = false;

            var ragdoll = GetComponentInChildren<ZombieRagdollToggle>();
            if (ragdoll)
            {
                ragdoll.PlayDeathRagdoll(hitDir);
                if (destroyOnDeath) Destroy(gameObject, 3.2f);
            }
            else
            {
                if (animator) animator.SetTrigger("Die");
                if (destroyOnDeath) Destroy(gameObject, 0.25f);
            }
        }


        else
        {
            if (animator) animator.SetTrigger("Hit");
            // уведомляем движение / AI
            var chase = GetComponent<ZombieChase>();
            if (chase) chase.OnHit();   // ← ТОЛЬКО СТАН
        }


    }
}
