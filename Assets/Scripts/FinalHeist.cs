using UnityEngine;
using UnityEngine.SceneManagement;

// Desafío final: caja fuerte y alarma
public class FinalHeist : MonoBehaviour
{
    public float holdTime = 3f;
    public float alarmDuration = 60f;
    public int bonus = 5000;

    public static bool Active;
    public static float TimeLeft;
    public static bool VictoryShown;

    private static FinalHeist instance;
    private bool playerNear;
    private bool opened;
    private float holdTimer;
    private AudioSource alarmSource;

    void Start()
    {
        // Reinicio al cargar la escena
        instance = this;
        Active = false;
        opened = false;
        TimeLeft = 0f;
        PoliceAgent.AlarmActive = false;
        PoliceAgent.Caught = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerNear = false;
            holdTimer = 0f;
        }
    }

    void Update()
    {
        if (Active)
        {
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) Fail("La alarma se acabó: misión fallida");
            return;
        }

        if (opened || !playerNear) return;
        if (!GameData.Instance.heistUnlocked) return;

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdTime) Open();
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void Open()
    {
        opened = true;
        Active = true;
        TimeLeft = alarmDuration;
        holdTimer = 0f;
        PoliceAgent.AlarmActive = true;

        alarmSource = SoundFX.StartAlarm(gameObject);
        FX.Burst(transform.position + Vector3.up, new Color(1f, 0.6f, 0f), 80);
        GameData.Instance.ShowToast("¡Alarma! Llegá a la salida antes de que se acabe el tiempo");
    }

    // Completar el golpe al escapar
    public static bool Complete()
    {
        if (!Active || instance == null) return false;

        Active = false;
        PoliceAgent.AlarmActive = false;
        GameData.Instance.money += instance.bonus;
        instance.StopAlarm();
        VictoryShown = true;
        GameData.Instance.heistCompleted = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }

    void Fail(string message)
    {
        Active = false;
        PoliceAgent.AlarmActive = false;
        StopAlarm();
        GameData.Instance.ShowToast(message);
    }

    void StopAlarm()
    {
        if (alarmSource != null) Destroy(alarmSource);
        alarmSource = null;
    }

    void OnGUI()
    {
        if (VictoryShown)
        {
            float w = 420f, h = 200f;
            GUI.Box(new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h), "¡VICTORIA!");
            GUI.Label(new Rect(Screen.width / 2f - w / 2f + 20f, Screen.height / 2f - 30f, w - 40f, 40f),
                      $"Escapaste con el golpe final. Bonus: ${bonus}");
            if (GUI.Button(new Rect(Screen.width / 2f - 100f, Screen.height / 2f + 40f, 200f, 35f), "Continuar"))
            {
                VictoryShown = false;
                GameData.Instance.pendingExtraction = true;
                SceneManager.LoadScene("Hub");
            }
            return;
        }

        if (Active)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, TimeLeft));
            GUI.Box(new Rect(Screen.width / 2f - 180f, 60f, 360f, 30f),
                    $"ALARMA  {s / 60:00}:{s % 60:00}  -  llegá a la salida");
            return;
        }

        if (opened || !playerNear) return;

        // Aviso de caja bloqueada
        if (!GameData.Instance.heistUnlocked)
        {
            GUI.Label(new Rect(Screen.width / 2f - 200f, 20f, 400f, 25f), "Caja fuerte bloqueada: pagá la deuda en el Hub");
            return;
        }

        string text = holdTimer > 0f
            ? $"Forzando la caja fuerte... {(int)(holdTimer / holdTime * 100f)}%"
            : "Mantené [E] para forzar la caja fuerte (activa la alarma)";
        GUI.Label(new Rect(Screen.width / 2f - 200f, Screen.height / 2f + 60f, 400f, 25f), text);
    }
}
