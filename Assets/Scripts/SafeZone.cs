using UnityEngine;

// Zona de venta invisible
public class SafeZone : MonoBehaviour
{
    public static bool PlayerInside { get; private set; }
    public static bool PanelOpen { get; private set; }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnDisable()
    {
        if (PlayerInside) PlayerInside = false;
        if (PanelOpen) ClosePanel();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) PlayerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInside = false;
            if (PanelOpen) ClosePanel();
        }
    }

    void OpenPanel()
    {
        PanelOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePanel()
    {
        PanelOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}
