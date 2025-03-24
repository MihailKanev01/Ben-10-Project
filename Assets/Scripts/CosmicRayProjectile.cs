using UnityEngine;

public class CosmicRayProjectile : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 100f;
    public float lifetime = 5f;
    public float explosionRadius = 5f;
    public GameObject impactEffect;
    public Light rayLight;

    private void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifetime);

        // Optional: Add light that fades over time
        if (rayLight != null)
        {
            StartCoroutine(FadeLight());
        }
    }

    private void Update()
    {
        // Move forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Create impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, transform.rotation);
        }

        // Check for enemies in explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            // Apply damage to enemy
            EnemyHealth enemy = hitCollider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // Apply force to rigidbodies
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(1000f, transform.position, explosionRadius);
            }
        }

        // Destroy the projectile
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator FadeLight()
    {
        float initialIntensity = rayLight.intensity;

        for (float t = 0; t < lifetime; t += Time.deltaTime)
        {
            rayLight.intensity = Mathf.Lerp(initialIntensity, 0, t / lifetime);
            yield return null;
        }
    }

    // Draw gizmos for visualization in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
