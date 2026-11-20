using UnityEngine;
using UnityEngine.SceneManagement;

// Zona de salida (verde): vuelve al Hub
public class ExtractionZone : MonoBehaviour
{
    public string hubSceneName = "Hub";

    private bool playerInRange;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    void Update()
    {
        if (FinalHeist.VictoryShown) return;

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (FinalHeist.Complete()) return;
            GameData.Instance.pendingExtraction = true;
            SceneManager.LoadScene(hubSceneName);
        }
    }

    void OnGUI()
    {
        if (!playerInRange) return;
        GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height / 2f + 60f, 300f, 25f), "[E] Salir de la zona");
    }
}
