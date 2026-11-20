using UnityEngine;

public enum StationKind { SaveBed, Stash, BlackMarket }

// Estación de la casa de venta
public class SafeHouseStation : MonoBehaviour
{
    // Estado: junto a la PC
    public static bool PlayerNearBlackMarket { get; private set; }
    // Estado: panel abierto
    public static bool MenuOpen { get; private set; }

    public StationKind kind = StationKind.SaveBed;

    private bool playerNear;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        playerNear = true;
        if (kind == StationKind.BlackMarket) PlayerNearBlackMarket = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        playerNear = false;
        if (kind == StationKind.BlackMarket)
        {
            PlayerNearBlackMarket = false;
            if (MenuOpen) CloseMenu();
        }
    }

    void OnDisable()
    {
        if (kind == StationKind.BlackMarket)
        {
            PlayerNearBlackMarket = false;
            if (MenuOpen) CloseMenu();
        }
    }

    void Update()
    {
        if (!playerNear || !Input.GetKeyDown(KeyCode.E)) return;

        if (kind == StationKind.BlackMarket)
        {
            if (MenuOpen) CloseMenu();
            else OpenMenu();
        }
        else
        {
            Debug.Log($"Estación {kind}: acción pendiente de conectar.");
        }
    }

    void OpenMenu()
    {
        MenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenu()
    {
        MenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnGUI()
    {
        if (!playerNear || GameData.Instance == null) return;

        if (kind != StationKind.BlackMarket)
        {
            string text = kind == StationKind.SaveBed ? "[E] Guardar partida" : "[E] Guardar objetos";
            GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height - 120f, 200f, 30f), text);
            return;
        }

        if (!MenuOpen)
        {
            GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height - 120f, 200f, 30f), "[E] Mercado negro");
            return;
        }

        DrawMarketMenu();
    }

    // Vender lo de la mochila
    void DrawMarketMenu()
    {
        GameData d = GameData.Instance;
        float x = Screen.width / 2f - 200f;
        float boxH = 150f + Mathf.Max(1, d.carried.Count) * 30f;

        GUI.Box(new Rect(x, 100f, 400f, boxH), "Mercado negro: vender mochila");
        GUI.Label(new Rect(x + 15f, 130f, 370f, 20f), $"Dinero: ${d.money}");

        if (d.carried.Count == 0)
            GUI.Label(new Rect(x + 15f, 155f, 370f, 20f), "Tu mochila está vacía.");

        int sellIndex = -1;
        for (int i = 0; i < d.carried.Count; i++)
        {
            Item item = d.carried[i];
            float y = 155f + i * 30f;
            GUI.Label(new Rect(x + 15f, y, 220f, 25f), $"{item.name} ({item.weight} kg)");
            GUI.Label(new Rect(x + 245f, y, 60f, 25f), $"${item.value}");
            if (GUI.Button(new Rect(x + 315f, y, 70f, 25f), "Vender")) sellIndex = i;
        }

        if (sellIndex >= 0) d.SellCarried(sellIndex);

        if (d.carried.Count > 0 && GUI.Button(new Rect(x + 15f, 100f + boxH - 70f, 370f, 28f), "Vender todo"))
        {
            for (int i = d.carried.Count - 1; i >= 0; i--)
                d.SellCarried(i);
        }

        if (GUI.Button(new Rect(x + 15f, 100f + boxH - 35f, 370f, 28f), "Cerrar [E]"))
            CloseMenu();
    }
}
