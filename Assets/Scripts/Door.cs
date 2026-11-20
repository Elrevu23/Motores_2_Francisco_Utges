using UnityEngine;

// Puerta: abrir y cerrar con E
public class Door : MonoBehaviour
{
    public bool locked;
    // Sentido de giro
    public float openAngle = -95f;
    public float speed = 180f;

    private bool isOpen;
    private bool playerNear;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            playerNear = false;
    }

    void Update()
    {
        // Puerta junto a la PC: E vende
        if (playerNear && !locked && !SafeHouseStation.PlayerNearBlackMarket && Input.GetKeyDown(KeyCode.E))
            isOpen = !isOpen;

        Quaternion goal = Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, goal, speed * Time.deltaTime);
    }

    void OnGUI()
    {
        if (!playerNear || locked) return;

        string text = isOpen ? "[E] Cerrar puerta" : "[E] Abrir puerta";
        GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height - 150f, 200f, 30f), text);
    }
}
