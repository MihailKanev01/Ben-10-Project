using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public float destroyDelay = 3f;
    public bool disableColliderOnDeath = true;
    public bool disableRigidbodyOnDeath = true;

    [Header("Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public ParticleSystem hitEffect;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;

    private bool isDead = false;
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && (hitSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // Apply damage
        currentHealth -= damage;

        // Trigger damage event
        OnDamageTaken?.Invoke(damage);

        // Play hit sound if available
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Play hit effect if available
        if (hitEffect != null)
        {
            hitEffect.Play();
        }

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // Trigger death event
        OnDeath?.Invoke();

        // Play death sound if available
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Spawn death effect if available
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Disable colliders if configured
        if (disableColliderOnDeath)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }

        // Disable rigidbody if configured
        if (disableRigidbodyOnDeath)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }
        }

        // Try to find and trigger Animator death state
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Try common death parameter names
            string[] deathParams = new string[] { "Death", "Die", "IsDead", "Dead" };

            foreach (string param in deathParams)
            {
                try
                {
                    // Try as trigger
                    animator.SetTrigger(param);
                    break;
                }
                catch
                {
                    try
                    {
                        // Try as bool
                        animator.SetBool(param, true);
                        break;
                    }
                    catch
                    {
                        // Parameter not found, try next one
                        continue;
                    }
                }
            }
        }

        // Destroy after delay if configured
        if (destroyDelay > 0)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    // Helper method for healing
    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // Helper for setting health directly
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
}