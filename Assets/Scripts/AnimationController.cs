using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    // Animation States - Add more as needed
    private const string IDLE = "Idle";
    private const string WALK = "Walk";
    private const string RUN = "Run";
    private const string JUMP = "Jump";
    private const string FALL = "Fall";

    // State tracking
    private string currentState;

    void Start()
    {
        animator = GetComponent<Animator>();
        ChangeAnimationState(IDLE);
    }

    // Call this from PlayerController to change animations without interruption
    public void ChangeAnimationState(string newState)
    {
        // Prevent same animation interrupting itself
        if (currentState == newState) return;

        // Play the animation
        animator.Play(newState);

        // Update current state
        currentState = newState;
    }

    // Helper method for blended animations like locomotion
    public void UpdateLocomotionAnimation(float speed)
    {
        if (animator == null) return;

        animator.SetFloat("Speed", speed);
    }

    // Helper method for triggering jumps, etc.
    public void TriggerAnimation(string triggerName)
    {
        if (animator == null) return;

        animator.SetTrigger(triggerName);
    }

    // Helper method for setting booleans like Grounded
    public void SetAnimationBool(string paramName, bool value)
    {
        if (animator == null) return;

        animator.SetBool(paramName, value);
    }
}