using UnityEngine;

// Policía: patrulla, persecución y paredes
public class PoliceAgent : MonoBehaviour
{
    public enum PatrolMode { Waypoints, Radius }

    // Estado compartido de policías
    public static bool AlarmActive;
    public static bool Caught;

    public PatrolMode mode = PatrolMode.Radius;
    public Transform[] waypoints;
    public float patrolRadius = 6f;
    public float moveSpeed = 2.4f;
    public float waitTime = 1.5f;

    [Header("Visión")]
    public float viewRange = 16f;
    public float viewAngle = 120f;
    public float loseSightTime = 3f;
    public float chaseSpeed = 5.6f;
    public float catchDistance = 1.1f;

    private Vector3 home;
    private Vector3 target;
    private Vector3 lastKnown;
    private int index;
    private float waitTimer;
    private Transform player;
    private float lastSeenTimer;
    private bool chasing;
    private bool seenNow;
    private bool knowsPlayer;

    // Direcciones de patrulla radial
    private static readonly Vector3[] radiusDirections =
    {
        Vector3.forward, Vector3.right, Vector3.back, Vector3.left
    };

    void Start()
    {
        home = transform.position;
        target = GetCurrentTarget();
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (Caught || player == null)
        {
            Patrol();
            return;
        }

        UpdateDetection();

        if (chasing)
            Chase();
        else
            Patrol();
    }

    void UpdateDetection()
    {
        seenNow = CanSeePlayer();
        if (seenNow)
        {
            lastSeenTimer = loseSightTime;
            lastKnown = player.position;
        }
        else if (lastSeenTimer > 0f)
        {
            lastSeenTimer -= Time.deltaTime;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        bool closeDuringAlarm = AlarmActive && distToPlayer < 25f;
        bool veryClose = distToPlayer < 1.5f;
        knowsPlayer = seenNow || closeDuringAlarm || veryClose;

        bool hunting = lastSeenTimer > 0f || knowsPlayer;
        if (hunting && !chasing)
        {
            FX.Burst(transform.position + Vector3.up * 2f, Color.red, 12);
            Debug.Log($"{name}: te detectó (distancia {distToPlayer:0.0} m)");
        }
        chasing = hunting;
    }

    bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * 0.7f;
        Vector3 aim = player.position + Vector3.up * 0.8f;
        Vector3 toPlayer = aim - eye;
        float dist = toPlayer.magnitude;

        bool seen = false;
        if (dist <= viewRange && Vector3.Angle(transform.forward, toPlayer) <= viewAngle * 0.5f)
        {
            // Ver al jugador con raycast
            RaycastHit[] hits = Physics.RaycastAll(eye, toPlayer / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in hits)
            {
                if (h.collider.GetComponentInParent<PoliceAgent>() != null) continue;
                seen = h.collider.GetComponentInParent<PlayerController>() != null;
                if (!seen && Time.frameCount % 120 == 0) Debug.Log($"{name} no te ve: lo tapa {h.collider.name}");   // DIAGNOSTICO
                break;
            }
        }

        // Rayo de depuración
        Debug.DrawLine(eye, aim, seen ? Color.green : Color.red);
        return seen;
    }

    public bool IsChasing => chasing;

    void Chase()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude < catchDistance)
        {
            if (!Caught) GameData.Instance.PlayerCaught();
            return;
        }

        // Perseguir o ir a la última posición
        Vector3 goal = knowsPlayer ? player.position : lastKnown;
        Vector3 toGoal = goal - transform.position;
        toGoal.y = 0f;

        if (!knowsPlayer && toGoal.magnitude < 1f)
        {
            // Dejar de buscar
            lastSeenTimer = 0f;
            return;
        }

        Vector3 dir = Steer(toGoal.normalized, chaseSpeed * Time.deltaTime + 0.2f);
        if (dir == Vector3.zero) return;

        transform.position += dir * chaseSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }

    void Patrol()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < 0.2f)
        {
            waitTimer = waitTime;
            AdvanceTarget();
            return;
        }

        Vector3 dir = Steer(toTarget.normalized, moveSpeed * Time.deltaTime + 0.2f);
        if (dir == Vector3.zero)
        {
            // Saltar destino bloqueado
            AdvanceTarget();
            return;
        }

        transform.position += dir * moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
    }

    // Buscar camino libre
    Vector3 Steer(Vector3 dir, float reach)
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        if (!Blocked(origin, dir, reach)) return dir;

        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f };
        foreach (float a in angles)
        {
            Vector3 d = Quaternion.Euler(0f, a, 0f) * dir;
            if (!Blocked(origin, d, reach)) return d;
        }
        return Vector3.zero;
    }

    // Detectar obstáculos
    bool Blocked(Vector3 origin, Vector3 dir, float reach)
    {
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.35f, dir, reach + 0.35f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<PoliceAgent>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            return true;
        }
        return false;
    }

    bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    Vector3 GetCurrentTarget()
    {
        if (mode == PatrolMode.Waypoints && HasWaypoints)
            return waypoints[index].position;

        return home + radiusDirections[index] * patrolRadius;
    }

    void AdvanceTarget()
    {
        int count = (mode == PatrolMode.Waypoints && HasWaypoints) ? waypoints.Length : radiusDirections.Length;
        index = (index + 1) % count;
        target = GetCurrentTarget();
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar cono de visión
        Vector3 eyeG = transform.position + Vector3.up * 0.7f;
        float half = viewAngle * 0.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyeG, eyeG + Quaternion.Euler(0f, -half, 0f) * transform.forward * viewRange);
        Gizmos.DrawLine(eyeG, eyeG + Quaternion.Euler(0f, half, 0f) * transform.forward * viewRange);

        Vector3 center = Application.isPlaying ? home : transform.position;

        if (mode == PatrolMode.Radius)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 dir in radiusDirections)
                Gizmos.DrawLine(center, center + dir * patrolRadius);
        }
        else if (HasWaypoints)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }
    }
}
