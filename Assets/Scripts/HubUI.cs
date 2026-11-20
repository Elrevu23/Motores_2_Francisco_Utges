using UnityEngine;
using UnityEngine.SceneManagement;

// Interfaz del Hub
public class HubUI : MonoBehaviour
{
    private enum HubPanel { Extraction, Main, Shop }
    private HubPanel panel;

    // Zonas del mapa
    private readonly string[] zones = { "Centro", "Barrio Norte", "Puerto", "Suburbios", "Zona industrial", "Barrio alto" };

    void Start()
    {
        panel = GameData.Instance.pendingExtraction ? HubPanel.Extraction : HubPanel.Main;

        // Desbloquear cursor en el Hub
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        GameData d = GameData.Instance;
        if (d == null) return;

        switch (panel)
        {
            case HubPanel.Extraction: DrawExtraction(d); break;
            case HubPanel.Shop: DrawShop(d); break;
            default: DrawMain(d); break;
        }
    }

    void DrawExtraction(GameData d)
    {
        GUI.Box(new Rect(20, 20, 460, 420), "Salida de la zona");
        GUI.Label(new Rect(35, 50, 430, 20), "Elegí qué vender y qué guardar:");

        // Aplicar cambios después de dibujar
        int sellIndex = -1;
        int keepIndex = -1;

        for (int i = 0; i < d.carried.Count; i++)
        {
            Item item = d.carried[i];
            float y = 80 + i * 30;
            GUI.Label(new Rect(35, y, 220, 25), $"{item.name} ({item.weight} kg)");
            GUI.Label(new Rect(255, y, 60, 25), $"${item.value}");
            if (GUI.Button(new Rect(320, y, 70, 25), "Vender")) sellIndex = i;
            if (GUI.Button(new Rect(395, y, 70, 25), "Guardar")) keepIndex = i;
        }

        if (sellIndex >= 0) d.SellCarried(sellIndex);
        if (keepIndex >= 0) d.KeepCarried(keepIndex);

        if (d.carried.Count == 0 && GUI.Button(new Rect(35, 400, 430, 30), "Continuar"))
        {
            d.pendingExtraction = false;
            panel = HubPanel.Main;
        }
    }

    void DrawMain(GameData d)
    {
        GUI.Box(new Rect(20, 20, 700, 420), "Hub");

        // Panel izquierdo: dinero, tienda, guardado
        GUI.Label(new Rect(35, 50, 230, 20), $"Dinero: ${d.money}");
        GUI.Label(new Rect(35, 70, 230, 20), $"Deuda: ${d.debt}");
        GUI.Label(new Rect(35, 90, 230, 20), $"Equipo: {GameData.BackpackNames[d.backpackLevel]}");

        if (GUI.Button(new Rect(35, 115, 230, 28), "Tienda")) panel = HubPanel.Shop;
        if (GUI.Button(new Rect(35, 150, 230, 28), "Pagar deuda con todo el dinero")) d.PayDebt(d.money);

        GUI.Label(new Rect(35, 190, 230, 20), "Guardado:");
        int sellStash = -1;
        int shown = Mathf.Min(d.stash.Count, 6);
        for (int i = 0; i < shown; i++)
        {
            float y = 210 + i * 28;
            GUI.Label(new Rect(35, y, 150, 25), $"{d.stash[i].name} ${d.stash[i].value}");
            if (GUI.Button(new Rect(190, y, 75, 25), "Vender")) sellStash = i;
        }
        if (d.stash.Count > shown)
            GUI.Label(new Rect(35, 210 + shown * 28, 230, 20), $"... y {d.stash.Count - shown} más");
        if (sellStash >= 0) d.SellStash(sellStash);

        // Panel derecho: mapa de la ciudad
        GUI.Label(new Rect(290, 50, 400, 20), "Elegí a dónde ir:");
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != "Centro") continue;
            int col = i % 3;
            int row = i / 3;
            Rect r = new Rect(290 + col * 133 + 4, 75 + row * 170 + 4, 125, 162);

            if (GUI.Button(r, zones[i] + "\n(entrar)"))
            {
                d.currentZone = zones[i];
                SceneManager.LoadScene("SampleScene");
            }
        }
    }

    void DrawShop(GameData d)
    {
        GUI.Box(new Rect(20, 20, 460, 300), "Tienda");
        GUI.Label(new Rect(35, 50, 430, 20), $"Dinero: ${d.money}");

        for (int i = 0; i < GameData.BackpackNames.Length; i++)
        {
            float y = 80 + i * 40;
            string estado = i <= d.backpackLevel ? "(comprada)" : $"${GameData.BackpackPrices[i]}";
            GUI.Label(new Rect(35, y, 430, 25),
                      $"{GameData.BackpackNames[i]} - {GameData.BackpackCapacities[i]} kg  {estado}");
        }

        int next = d.backpackLevel + 1;
        string buyText = next < GameData.BackpackNames.Length
            ? $"Comprar {GameData.BackpackNames[next]} (${GameData.BackpackPrices[next]})"
            : "Ya tenés la mejor mochila";

        if (GUI.Button(new Rect(35, 210, 430, 30), buyText))
            d.TryBuyNextBackpack();

        if (GUI.Button(new Rect(35, 260, 430, 30), "Volver")) panel = HubPanel.Main;
    }
}
