using UnityEngine;

// Velocidad del policía al Animator
[RequireComponent(typeof(PoliceAgent))]
public class PoliceAnimatorDriver : MonoBehaviour
{
    private PoliceAgent agent;
    private Animator animator;
    private Vector3 lastPosition;

    void Start()
    {
        agent = GetComponent<PoliceAgent>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null) return;

        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        // Estados de animación
        float walk = Mathf.Clamp01(agent.moveSpeed > 0f ? speed / agent.moveSpeed : 0f);
        float target = agent.IsChasing ? 2f : walk;

        // Suavizar cambios
        animator.SetFloat("Speed", target, 0.15f, Time.deltaTime);
    }
}
