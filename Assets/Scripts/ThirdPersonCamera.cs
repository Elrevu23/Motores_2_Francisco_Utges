using UnityEngine;
using Unity.Cinemachine;

// Cámara sobre el hombro
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    // Ajustes de la cámara
    public float cameraDistance = 2.4f;
    public float cameraHeight = 1.5f;
    public float shoulderOffset = 0.6f;
    public float mouseSensitivity = 2f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float fieldOfView = 60f;

    // Capas que bloquean la cámara
    public LayerMask collisionMask = ~0;

    // Objeto apuntado por el punto de mira
    public static Lootable Aimed { get; private set; }
    const float aimRange = 3.5f;

    private float yaw;
    private float pitch = 8f;
    private Transform pivot;
    private CinemachineFollow follow;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetupCinemachine();
        // Ambiente de ciudad en bucle
        SoundFX.StartAmbience(gameObject);

        // Plano cercano
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.nearClipPlane = 0.1f;
    }

    // Crear rig de Cinemachine
    void SetupCinemachine()
    {
        pivot = new GameObject("CamPivot").transform;

        GameObject vcamGo = new GameObject("CM Camera");
        CinemachineCamera vcam = vcamGo.AddComponent<CinemachineCamera>();
        vcam.Follow = pivot;
        vcam.LookAt = pivot;

        vcam.Lens.FieldOfView = fieldOfView;

        if (target != null) yaw = target.eulerAngles.y;

        follow = vcamGo.AddComponent<CinemachineFollow>();
        follow.FollowOffset = new Vector3(0f, 0f, -cameraDistance);

        vcamGo.AddComponent<CinemachineHardLookAt>();

        if (GetComponent<CinemachineBrain>() == null)
            gameObject.AddComponent<CinemachineBrain>();
    }

    void Update()
    {
        UpdateAim();
    }

    // Rayo del punto de mira
    void UpdateAim()
    {
        Aimed = null;
        if (target == null || Camera.main == null) return;
        if (Cursor.lockState != CursorLockMode.Locked || FinalHeist.VictoryShown) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (Vector3.Distance(target.position, h.point) > aimRange) return;

            Lootable l = h.collider.GetComponentInParent<Lootable>();
            if (l != null)
            {
                Aimed = l;
                return;
            }
            if (!h.collider.isTrigger) return;
        }
    }

    void OnGUI()
    {
        if (Cursor.lockState != CursorLockMode.Locked || FinalHeist.VictoryShown) return;

        // Dibujar punto de mira
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        Color prev = GUI.color;
        GUI.color = Aimed != null ? Color.yellow : Color.white;
        GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
        GUI.color = prev;

        // Una sola etiqueta de aviso
        if (Aimed != null)
            GUI.Label(new Rect(cx - 150f, cy + 60f, 300f, 25f), Aimed.Prompt());
    }

    void LateUpdate()
    {
        if (target == null || pivot == null) return;

        // Liberar o bloquear el mouse
        if (Input.GetMouseButtonDown(0) && !GameData.Paused && Cursor.lockState == CursorLockMode.None && !SafeZone.PanelOpen && !SafeHouseStation.MenuOpen && !FinalHeist.VictoryShown)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        // Altura de la cámara
        CharacterController cc = target.GetComponent<CharacterController>();
        float feetY = cc != null ? cc.bounds.min.y : target.position.y;
        Vector3 center = new Vector3(target.position.x, feetY + cameraHeight, target.position.z);

        // Punto sobre el hombro
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        float side = shoulderOffset;
        float sideLen = Mathf.Abs(shoulderOffset);
        if (sideLen > 0.01f)
        {
            Vector3 sideDir = right * Mathf.Sign(shoulderOffset);
            float sideHit = NearestHit(center, sideDir, sideLen);
            if (sideHit < float.MaxValue)
                side = Mathf.Sign(shoulderOffset) * Mathf.Max(0f, sideHit - 0.3f);
        }
        Vector3 shoulder = center + right * side;

        // Pivote de la cámara
        pivot.position = shoulder;
        pivot.rotation = Quaternion.identity;

        // Acercar cámara si hay pared
        float dist = cameraDistance;
        float nearest = NearestHit(shoulder, rot * Vector3.back, cameraDistance);
        if (nearest < float.MaxValue)
            dist = Mathf.Clamp(nearest - 0.3f, 0.6f, cameraDistance);

        // Órbita con el mouse
        follow.FollowOffset = rot * new Vector3(0f, 0f, -dist);
    }

    // Distancia a obstáculos
    float NearestHit(Vector3 from, Vector3 dir, float maxDistance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(from, 0.3f, dir, maxDistance, collisionMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(target)) continue;
            if (h.distance < nearest) nearest = h.distance;
        }
        return nearest;
    }
}
