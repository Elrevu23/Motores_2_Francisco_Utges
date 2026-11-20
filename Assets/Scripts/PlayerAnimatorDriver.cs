using UnityEngine;

// Estado del jugador al Animator
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimatorDriver : MonoBehaviour
{
    private PlayerController player;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;

        if (animator != null)
        {
            // Evitar que se congele la caminata
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Diagnóstico de clips en loop
            if (animator.runtimeAnimatorController != null)
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                    if (!clip.isLooping && clip.name.Contains("Loop"))
                        Debug.LogWarning($"El clip {clip.name} no tiene Loop Time activado. Usa Extraccion > Aplicar skins.");
        }
    }

    private Vector3 lastPosition;

    void Update()
    {
        if (animator == null) return;

        // Velocidad para animar
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        animator.SetFloat("Speed", speed / player.walkSpeed);

        animator.SetBool("Crouching", Input.GetKey(KeyCode.LeftControl));
        animator.SetBool("Grounded", controller.isGrounded);

        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            animator.SetTrigger("Jump");
    }
}
