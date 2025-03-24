
// Basic enemy health script for the stomp attack to work with
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Add death effects or logic here
        Destroy(gameObject);
    }
}