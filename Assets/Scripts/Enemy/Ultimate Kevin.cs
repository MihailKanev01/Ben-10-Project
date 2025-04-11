using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class UltimateKevinController : MonoBehaviour
{
    [Header("Basic Stats")]
    public float attackRange = 3f;
    public float sightRange = 15f;
    public float rotationSpeed = 5f;

    [Header("Movement Settings")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 6f;

    [Header("Attack Settings")]
    public float attackCooldown = 2f;
    public float attackDamage = 20f;

    [Header("Special Abilities")]
    [Tooltip("FireBlast - Heatblast ability")]
    public GameObject fireBlastPrefab;
    public Transform fireBlastSpawnPoint;
    public float fireBlastCooldown = 8f;
    public float fireBlastDamage = 30f;

    [Tooltip("Crystal Shards - Diamondhead ability")]
    public GameObject crystalShardPrefab;
    public Transform crystalShardSpawnPoint;
    public float crystalShardCooldown = 6f;
    public float crystalShardDamage = 15f;

    [Tooltip("Electric Shock - Feedback ability")]
    public GameObject electricShockPrefab;
    public float electricShockRange = 8f;
    public float electricShockCooldown = 10f;
    public float electricShockDamage = 25f;

    [Header("Animation Control")]
    public string walkAnimationName = "Walk";
    public string runAnimationName = "Run";
    public string hitAnimationName = "Hit";
    public float hitAnimationDuration = 1.0f;  // Duration of hit animation in seconds

    [Header("Effects")]
    public ParticleSystem hitEffect;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip fireBlastSound;
    public AudioClip crystalSound;
    public AudioClip electricSound;

    [Header("AI Settings")]
    public float patrolWaitTime = 2f;
    public float aggroTime = 10f;
    public float minPatrolDistance = 5f;
    public float maxPatrolDistance = 15f;
    public Transform[] patrolPoints;

    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    public string playerTag = "Player";

    // References
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private AudioSource audioSource;
    private EnemyHealth healthComponent;

    // State tracking
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool isAttacking;
    private bool isHit = false;
    private float lastAttackTime;
    private float lastFireBlastTime;
    private float lastCrystalShardTime;
    private float lastElectricShockTime;
    private int currentPatrolIndex = -1;
    private float currentAggroTime;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDead = false;
    private float patrolPointCheckTimer = 0f;

    // Animation hashes
    private int speedHash;
    private int attackHash;
    private int fireBlastHash;
    private int crystalShardHash;
    private int electricShockHash;
    private int hitHash;
    private int deathHash;
    private int isWalkingHash;
    private int isRunningHash;

    void Awake()
    {
        // Get reference to the health component
        healthComponent = GetComponent<EnemyHealth>();
        if (healthComponent == null)
        {
            Debug.LogWarning("No EnemyHealth component found on Ultimate Kevin. Adding one automatically.");
            healthComponent = gameObject.AddComponent<EnemyHealth>();
            healthComponent.maxHealth = 500f; // Default value
            healthComponent.currentHealth = healthComponent.maxHealth;
        }

        // Subscribe to health events
        healthComponent.OnDamageTaken.AddListener(OnDamageTaken);
        healthComponent.OnDeath.AddListener(OnDeath);
    }

    void Start()
    {
        if (showDebugInfo)
            Debug.Log("Initializing Ultimate Kevin...");

        // Initialize components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Look for player
        FindPlayer();

        // Set initial values
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = attackRange * 0.8f;

            if (!agent.isOnNavMesh)
            {
                Debug.LogError("Ultimate Kevin is not on a NavMesh! Make sure the NavMesh is baked and Kevin is on it.");
            }
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found on Ultimate Kevin!");
        }

        // Set up animation hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            attackHash = Animator.StringToHash("Attack");
            fireBlastHash = Animator.StringToHash("FireBlast");
            crystalShardHash = Animator.StringToHash("CrystalShard");
            electricShockHash = Animator.StringToHash("ElectricShock");
            hitHash = Animator.StringToHash("Hit");
            deathHash = Animator.StringToHash("Death");
            isWalkingHash = Animator.StringToHash("IsWalking");
            isRunningHash = Animator.StringToHash("IsRunning");
        }
        else
        {
            Debug.LogWarning("Animator component not found on Ultimate Kevin!");
        }

        // Check patrol points
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned to Ultimate Kevin. He will stay in place.");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"Found {patrolPoints.Length} patrol points");

            // Validate patrol points
            int validPoints = 0;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    validPoints++;
                }
                else
                {
                    Debug.LogWarning($"Patrol point {i} is null!");
                }
            }

            if (validPoints > 0)
            {
                if (showDebugInfo)
                    Debug.Log($"Found {validPoints} valid patrol points");

                // Start patrol behavior
                GoToNextPatrolPoint();
            }
            else
            {
                Debug.LogError("All patrol points are null! Add valid patrol points to the array.");
            }
        }
    }

    void FindPlayer()
    {
        // Try finding player by tag first
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

        // If not found, try finding common player names
        if (playerObj == null)
        {
            string[] commonPlayerNames = { "Player", "Character", "Ben", "Ben10", "Hero" };
            foreach (string name in commonPlayerNames)
            {
                playerObj = GameObject.Find(name);
                if (playerObj != null) break;
            }
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            if (showDebugInfo)
                Debug.Log("Found player: " + player.name);
        }
        else
        {
            Debug.LogWarning($"Player not found! Make sure your player has the '{playerTag}' tag or a recognizable name.");
        }
    }

    void Update()
    {
        if (isDead) return;

        // Skip regular updates if currently playing hit animation
        if (isHit) return;

        // Check if player exists
        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                PatrolBehavior();
                return;
            }
        }

        // Check player distance
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInSightRange = distanceToPlayer <= sightRange;
        playerInAttackRange = distanceToPlayer <= attackRange;

        if (showDebugInfo && Time.frameCount % 60 == 0) // Log only every 60 frames to reduce spam
        {
            Debug.Log($"Distance to player: {distanceToPlayer:F2}, " +
                      $"In sight range: {playerInSightRange}, " +
                      $"In attack range: {playerInAttackRange}");

            if (agent != null)
            {
                Debug.Log($"Agent state: isOnNavMesh={agent.isOnNavMesh}, " +
                          $"hasPath={agent.hasPath}, " +
                          $"remainingDistance={agent.remainingDistance:F2}, " +
                          $"velocity magnitude={agent.velocity.magnitude:F2}");
            }
        }

        // Update AI behavior
        if (playerInSightRange && playerInAttackRange)
        {
            AttackBehavior();
        }
        else if (playerInSightRange)
        {
            ChaseBehavior();
        }
        else
        {
            PatrolBehavior();
        }

        // Update animator movement parameters
        UpdateAnimations();

        // If we're patrolling and not moving, check if we need to go to the next point
        if (!playerInSightRange && agent != null && agent.velocity.magnitude < 0.1f)
        {
            patrolPointCheckTimer += Time.deltaTime;
            if (patrolPointCheckTimer > 3f) // If stuck for more than 3 seconds
            {
                patrolPointCheckTimer = 0f;
                if (showDebugInfo)
                    Debug.Log("Kevin seems stuck, trying next patrol point");
                GoToNextPatrolPoint();
            }
        }
        else
        {
            patrolPointCheckTimer = 0f;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null || agent == null) return;

        // Check if agent is moving
        bool isMoving = agent.velocity.magnitude > 0.1f;

        // Update the appropriate animation state based on current behavior
        if (playerInSightRange)
        {
            // Chasing - play run animation
            animator.SetBool(isWalkingHash, false);
            animator.SetBool(isRunningHash, isMoving);

            // Directly play run animation if needed
            if (isMoving && !string.IsNullOrEmpty(runAnimationName))
            {
                animator.Play(runAnimationName);
            }
        }
        else
        {
            // Patrolling - play walk animation
            animator.SetBool(isRunningHash, false);
            animator.SetBool(isWalkingHash, isMoving);

            // Directly play walk animation if needed
            if (isMoving && !string.IsNullOrEmpty(walkAnimationName))
            {
                animator.Play(walkAnimationName);
            }
        }

        // Update generic speed parameter for blend trees if used
        animator.SetFloat(speedHash, agent.velocity.magnitude);
    }

    void PatrolBehavior()
    {
        // Reset aggro timer
        currentAggroTime = 0;

        // Set appropriate speed
        if (agent != null && agent.speed != patrolSpeed)
        {
            agent.speed = patrolSpeed;
        }

        // If no patrol points or agent, just stay in place
        if (patrolPoints == null || patrolPoints.Length == 0 || agent == null) return;

        // If we've arrived at the patrol point, wait then go to next
        if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        if (showDebugInfo)
            Debug.Log($"Reached patrol point {currentPatrolIndex}, waiting for {patrolWaitTime} seconds");

        // Wait at patrol point
        yield return new WaitForSeconds(patrolWaitTime);

        // Go to next point
        GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned!");
            return;
        }

        // Find the next valid patrol point
        int nextPointIndex = currentPatrolIndex;
        bool foundValidPoint = false;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            nextPointIndex = (currentPatrolIndex + i + 1) % patrolPoints.Length;

            if (patrolPoints[nextPointIndex] != null)
            {
                foundValidPoint = true;
                break;
            }
        }

        if (!foundValidPoint)
        {
            Debug.LogWarning("No valid patrol points found!");
            return;
        }

        currentPatrolIndex = nextPointIndex;

        if (showDebugInfo)
            Debug.Log("Moving to patrol point: " + currentPatrolIndex);

        // Check if agent is valid
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent is null!");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("NavMeshAgent is not on NavMesh!");
            return;
        }

        try
        {
            // Set destination - wrap in try-catch to handle any NavMesh errors
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting navigation destination: {e.Message}");
        }
    }

    void ChaseBehavior()
    {
        // Update aggro timer
        currentAggroTime += Time.deltaTime;

        // If aggro has lasted too long, return to patrol
        if (currentAggroTime >= aggroTime)
        {
            playerInSightRange = false;
            currentAggroTime = 0;
            PatrolBehavior();
            return;
        }

        // Set appropriate speed
        if (agent != null && agent.speed != chaseSpeed)
        {
            agent.speed = chaseSpeed;
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
            Debug.Log("Chasing player");

        // Chase player
        if (agent != null && agent.isOnNavMesh && player != null)
        {
            try
            {
                agent.SetDestination(player.position);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting chase destination: {e.Message}");
            }
        }

        // Try special abilities while chasing
        TryUseSpecialAbility();
    }

    void AttackBehavior()
    {
        // Reset aggro timer when in attack range
        currentAggroTime = 0;

        if (showDebugInfo && Time.frameCount % 60 == 0)
            Debug.Log("In attack range of player");

        // Face the player
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        // Stop moving when attacking
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }

        // Basic attack if cooldown has elapsed
        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }

        // Try special abilities in attack range
        TryUseSpecialAbility();
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (showDebugInfo)
            Debug.Log("Performing basic attack");

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }

        // Play attack sound
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Wait until the animation reaches the point where damage should be applied
        yield return new WaitForSeconds(0.5f);

        // Apply damage if player is still in range
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            if (showDebugInfo)
                Debug.Log($"Dealing {attackDamage} damage to player");

            // Get player health component and apply damage
            // This assumes the player has a component that can take damage. Adjust as needed.
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // If your PlayerController has a TakeDamage method, uncomment this
                // playerController.TakeDamage(attackDamage);
            }

            // Try to find any component with TakeDamage method
            var components = player.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                var methodInfo = component.GetType().GetMethod("TakeDamage");
                if (methodInfo != null)
                {
                    if (showDebugInfo)
                        Debug.Log($"Found TakeDamage method on {component.GetType().Name}");

                    try
                    {
                        methodInfo.Invoke(component, new object[] { attackDamage });
                        break;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error calling TakeDamage: {e.Message}");
                    }
                }
            }
        }

        // Wait for the attack animation to finish
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
    }

    void TryUseSpecialAbility()
    {
        // Only use special abilities if not currently attacking
        if (isAttacking) return;

        // Randomly choose an ability based on cooldowns
        List<int> availableAbilities = new List<int>();

        if (Time.time >= lastFireBlastTime + fireBlastCooldown)
            availableAbilities.Add(0);

        if (Time.time >= lastCrystalShardTime + crystalShardCooldown)
            availableAbilities.Add(1);

        if (Time.time >= lastElectricShockTime + electricShockCooldown)
            availableAbilities.Add(2);

        // If any abilities are available, randomly choose one (chance-based)
        if (availableAbilities.Count > 0 && Random.value < 0.02f) // 2% chance per frame
        {
            int abilityIndex = availableAbilities[Random.Range(0, availableAbilities.Count)];

            switch (abilityIndex)
            {
                case 0:
                    StartCoroutine(UseFireBlast());
                    break;
                case 1:
                    StartCoroutine(UseCrystalShard());
                    break;
                case 2:
                    StartCoroutine(UseElectricShock());
                    break;
            }
        }
    }

    IEnumerator UseFireBlast()
    {
        isAttacking = true;
        lastFireBlastTime = Time.time;

        if (showDebugInfo)
            Debug.Log("Using Fire Blast ability");

        // Trigger fire blast animation
        if (animator != null)
        {
            animator.SetTrigger(fireBlastHash);
        }

        // Play sound
        if (audioSource != null && fireBlastSound != null)
        {
            audioSource.PlayOneShot(fireBlastSound);
        }

        // Wait for animation to reach point where blast should spawn
        yield return new WaitForSeconds(0.7f);

        // Spawn fire blast projectile
        if (fireBlastPrefab != null && player != null)
        {
            Vector3 spawnPosition = fireBlastSpawnPoint != null ?
                                   fireBlastSpawnPoint.position :
                                   transform.position + transform.forward + Vector3.up;

            Vector3 targetDirection = (player.position - spawnPosition).normalized;
            GameObject fireball = Instantiate(fireBlastPrefab, spawnPosition, Quaternion.LookRotation(targetDirection));

            // Assuming the fireball has a projectile script
            Projectile projectile = fireball.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = fireBlastDamage;
                projectile.direction = targetDirection;
                projectile.speed = 15f; // Adjust as needed
                projectile.targetLayers = LayerMask.GetMask("Player");
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("Fire Blast prefab does not have a Projectile component");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("Fire Blast prefab is null or player is null");
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
    }

    IEnumerator UseCrystalShard()
    {
        isAttacking = true;
        lastCrystalShardTime = Time.time;

        if (showDebugInfo)
            Debug.Log("Using Crystal Shard ability");

        // Trigger crystal shard animation
        if (animator != null)
        {
            animator.SetTrigger(crystalShardHash);
        }

        // Play sound
        if (audioSource != null && crystalSound != null)
        {
            audioSource.PlayOneShot(crystalSound);
        }

        // Wait for animation to reach point where shards should spawn
        yield return new WaitForSeconds(0.5f);

        // Spawn multiple crystal shards in a spread pattern
        if (crystalShardPrefab != null && player != null)
        {
            Vector3 spawnPosition = crystalShardSpawnPoint != null ?
                                   crystalShardSpawnPoint.position :
                                   transform.position + transform.forward + Vector3.up;

            Vector3 baseDirection = (player.position - spawnPosition).normalized;

            // Spawn 3 shards in a spread pattern
            for (int i = -1; i <= 1; i++)
            {
                Quaternion spreadRotation = Quaternion.Euler(0, i * 15f, 0);
                Vector3 spreadDirection = spreadRotation * baseDirection;

                GameObject shard = Instantiate(crystalShardPrefab, spawnPosition, Quaternion.LookRotation(spreadDirection));

                // Assuming the shard has a projectile script
                Projectile projectile = shard.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.damage = crystalShardDamage;
                    projectile.direction = spreadDirection;
                    projectile.speed = 20f; // Adjust as needed
                    projectile.targetLayers = LayerMask.GetMask("Player");
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning("Crystal Shard prefab does not have a Projectile component");
                }
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("Crystal Shard prefab is null or player is null");
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
    }

    IEnumerator UseElectricShock()
    {
        isAttacking = true;
        lastElectricShockTime = Time.time;

        if (showDebugInfo)
            Debug.Log("Using Electric Shock ability");

        // Trigger electric shock animation
        if (animator != null)
        {
            animator.SetTrigger(electricShockHash);
        }

        // Play sound
        if (audioSource != null && electricSound != null)
        {
            audioSource.PlayOneShot(electricSound);
        }

        // Wait for animation to reach point where shock should occur
        yield return new WaitForSeconds(0.6f);

        // Create electric shock effect
        if (electricShockPrefab != null)
        {
            GameObject shockEffect = Instantiate(electricShockPrefab, transform.position, Quaternion.identity);
            shockEffect.transform.parent = transform;
            Destroy(shockEffect, 2f);
        }

        // Apply damage to player if in range
        if (player != null && Vector3.Distance(transform.position, player.position) <= electricShockRange)
        {
            if (showDebugInfo)
                Debug.Log($"Dealing {electricShockDamage} electric damage to player");

            // Get player health component and apply damage
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // If your PlayerController has a TakeDamage method, uncomment this
                // playerController.TakeDamage(electricShockDamage);
            }

            // Try to find any component with TakeDamage method
            var components = player.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                var methodInfo = component.GetType().GetMethod("TakeDamage");
                if (methodInfo != null)
                {
                    try
                    {
                        methodInfo.Invoke(component, new object[] { electricShockDamage });
                        break;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error calling TakeDamage: {e.Message}");
                    }
                }
            }
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(1.2f);

        isAttacking = false;
    }

    // Called when enemy takes damage - handles reactions
    public void OnDamageTaken(float damage)
    {
        // If already playing hit animation or dead, skip
        if (isHit || isDead) return;

        if (showDebugInfo)
            Debug.Log($"Kevin took {damage} damage! Current health: {healthComponent.currentHealth}");

        // Start hit animation sequence
        StartCoroutine(PlayHitAnimation());

        // Play hurt sound
        if (audioSource != null && hurtSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // If we weren't chasing the player before, start chasing now
        if (!playerInSightRange && player != null)
        {
            playerInSightRange = true;
            currentAggroTime = 0;

            // Set agent destination to player position
            if (agent != null && agent.isOnNavMesh)
            {
                try
                {
                    agent.SetDestination(player.position);
                    agent.speed = chaseSpeed;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error setting destination after damage: {e.Message}");
                }
            }
        }
    }

    IEnumerator PlayHitAnimation()
    {
        // Set hit state
        isHit = true;

        if (showDebugInfo)
            Debug.Log("Playing hit animation");

        // Stop agent temporarily during hit reaction
        float originalSpeed = 0f;
        if (agent != null)
        {
            originalSpeed = agent.speed;
            agent.speed = 0;
        }

        // Play hit animation by name or trigger
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(hitAnimationName))
            {
                // Direct play by name
                animator.Play(hitAnimationName);
            }
            else
            {
                // Play by trigger
                animator.SetTrigger(hitHash);
            }
        }

        // Wait for hit animation duration
        yield return new WaitForSeconds(hitAnimationDuration);

        // Resume agent movement
        if (agent != null)
        {
            agent.speed = originalSpeed;
        }

        // Clear hit state
        isHit = false;
    }

    // Called when enemy dies - handles death actions
    public void OnDeath()
    {
        if (isDead) return;

        isDead = true;

        if (showDebugInfo)
            Debug.Log("Kevin has been defeated!");

        // Stop all coroutines
        StopAllCoroutines();

        // Disable Nav Mesh Agent
        if (agent != null)
        {
            agent.enabled = false;
        }

        // Trigger death animation if not already triggered
        if (animator != null)
        {
            animator.SetTrigger(deathHash);
        }

        // Clean up components that we don't need anymore
        this.enabled = false;
    }

    // Visualization gizmos for debugging
    void OnDrawGizmos()
    {
        // Always draw ranges to see them in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, electricShockRange);

        // Draw lines to patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawLine(transform.position, patrolPoints[i].position);
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.5f);
                }
            }
        }

        // If player is found, draw line to player
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= sightRange)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.blue;

            Gizmos.DrawLine(transform.position, player.position);
        }

        // Highlight current patrol point
        if (patrolPoints != null && currentPatrolIndex >= 0 && currentPatrolIndex < patrolPoints.Length &&
            patrolPoints[currentPatrolIndex] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(patrolPoints[currentPatrolIndex].position, 0.7f);
        }
    }
}