using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 15f;
    public float lifetime = 5f;
    public Vector3 direction = Vector3.forward;
    public LayerMask targetLayers;
    public bool destroyOnHit = true;

    [Header("Effects")]
    public GameObject impactEffectPrefab;
    public AudioClip impactSound;
    public bool scaleWithDamage = false;

    private Rigidbody rb;
    private AudioSource audioSource;
    private float createTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && impactSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        createTime = Time.time;

        // Apply initial velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Scale projectile if needed
        if (scaleWithDamage)
        {
            float scaleFactor = 1.0f + (damage / 100f);
            transform.localScale *= scaleFactor;
        }
    }

    void Update()
    {
        // If no rigidbody, manually move the projectile
        if (rb == null)
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        // Destroy after lifetime expires
        if (Time.time - createTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if collided with valid target
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            HandleImpact(other);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if collided with valid target
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            HandleImpact(collision.collider);
        }
        else
        {
            // Hit something else - just create effect and destroy
            CreateImpactEffect(collision.contacts[0].point);
            Destroy(gameObject);
        }
    }

    void HandleImpact(Collider other)
    {
        // Apply damage if target has a health component
        // Try with PlayerController first (for player)
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // If your PlayerController has a TakeDamage method, uncomment this
            // playerController.TakeDamage(damage);
        }

        // Try with EnemyHealth (for enemies)
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }

        // Create impact effect
        CreateImpactEffect(transform.position);

        // Destroy projectile if configured to do so
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    void CreateImpactEffect(Vector3 position)
    {
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            Destroy(impact, 2f); // Auto-destroy after 2 seconds
        }

        // Play impact sound
        if (audioSource != null && impactSound != null)
        {
            // Detach audio source so sound plays even after projectile is destroyed
            audioSource.transform.parent = null;
            audioSource.PlayOneShot(impactSound);
            Destroy(audioSource.gameObject, impactSound.length);
        }
    }
}