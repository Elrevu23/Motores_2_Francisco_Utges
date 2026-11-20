using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Publicaciones del mercado negro
[Serializable]
public class MarketListing
{
    public Item item;
    public float timeLeft;
}

// Datos globales entre escenas
public class GameData : MonoBehaviour
{
    private static GameData _instance;

    // Crear datos si faltan
    public static GameData Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameData");
                _instance = go.AddComponent<GameData>();
            }
            return _instance;
        }
    }

    public static readonly string[] BackpackNames = { "Mochila chica", "Mochila mediana", "Mochila grande" };
    public static readonly float[] BackpackCapacities = { 20f, 35f, 60f };
    public static readonly int[] BackpackPrices = { 0, 2000, 5000 };

    public const int MaxListings = 100;
    private const float SummaryInterval = 60f;
    private const float ToastDuration = 4f;

    public int money = 0;
    // Deuda de la demo (diseño: 100000)
    public int debt = 20000;
    public bool heistUnlocked = false;
    public bool heistCompleted = false;
    public int backpackLevel = 0;
    public string currentZone = "Centro";
    public bool pendingExtraction = false;

    public List<Item> carried = new List<Item>();
    public List<Item> stash = new List<Item>();
    public List<MarketListing> listings = new List<MarketListing>();

    // Resumen de ventas por minuto
    private float summaryTimer;
    private int salesThisMinute;
    private int earningsThisMinute;

    // Aviso en pantalla
    private string toastText = "";
    private float toastTimer;

    // Avisos de saqueo
    private class LootNotice
    {
        public string text;
        public float timer;
    }
    private readonly List<LootNotice> lootNotices = new List<LootNotice>();
    private const float LootNoticeDuration = 4f;
    private const int MaxLootNotices = 5;

    public float BackpackCapacity => BackpackCapacities[backpackLevel];

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Al cambiar de escena: destraba el tiempo y reinicia el estado de la policía
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Paused = false;
        Time.timeScale = 1f;
        PoliceAgent.Caught = false;
        PoliceAgent.AlarmActive = false;
    }

    // Pausa: Esc abre/cierra el menú. Frena el tiempo y libera el mouse.
    public static bool Paused;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !SafeZone.PanelOpen && !SafeHouseStation.MenuOpen && !FinalHeist.VictoryShown)
            SetPaused(!Paused);

        if (Paused) return;

        TickListings(Time.deltaTime);
        TickSummary(Time.deltaTime);

        if (toastTimer > 0f)
            toastTimer -= Time.deltaTime;

        for (int i = lootNotices.Count - 1; i >= 0; i--)
        {
            lootNotices[i].timer -= Time.deltaTime;
            if (lootNotices[i].timer <= 0f) lootNotices.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        if (toastTimer > 0f && !string.IsNullOrEmpty(toastText))
            GUI.Box(new Rect(Screen.width / 2f - 200f, 20f, 400f, 30f), toastText);

        DrawLootNotices();
        DrawObjective();
        if (Paused) DrawPauseMenu();
    }

    void SetPaused(bool value)
    {
        Paused = value;
        Time.timeScale = value ? 0f : 1f;
        bool onMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene";
        Cursor.lockState = (value || !onMap) ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value || !onMap;
    }

    void DrawPauseMenu()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 260f, h = 200f;
        Rect box = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
        GUI.Box(box, "PAUSA");

        if (GUI.Button(new Rect(box.x + 30f, box.y + 60f, w - 60f, 40f), "Continuar"))
            SetPaused(false);
        if (GUI.Button(new Rect(box.x + 30f, box.y + 115f, w - 60f, 40f), "Salir del juego"))
        {
            Time.timeScale = 1f;
            Application.Quit();
        }
    }

    // Objetivo en el mapa
    void DrawObjective()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SampleScene") return;
        if (heistCompleted) return;

        string text;
        if (heistUnlocked)
            text = "Objetivo: saque\u00e1 la caja fuerte del centro";
        else if (money >= debt)
            text = "Objetivo: escap\u00e1 al Hub por la zona verde para pagar la deuda";
        else
            text = $"Objetivo: junt\u00e1 dinero y pag\u00e1 la deuda en el Hub (faltan ${debt - money})";

        GUI.Box(new Rect(10f, 10f, 520f, 30f), text);
    }

    // Aviso de saqueo
    public void NotifyLoot(Item item)
    {
        string text = $"Saqueado: {item.name}  |  {item.weight:0.0} kg  |  ${item.value}";
        lootNotices.Add(new LootNotice { text = text, timer = LootNoticeDuration });
        if (lootNotices.Count > MaxLootNotices) lootNotices.RemoveAt(0);
    }

    // Vender todo lo guardado
    public int SellAllStash()
    {
        int count = stash.Count;
        int total = 0;
        foreach (Item item in stash) total += item.value;

        money += total;
        stash.Clear();

        if (count > 0)
        {
            ShowToast($"Mercado negro: vendiste {count} objeto(s) por ${total}");
            SoundFX.Sell();
        }
        return total;
    }

    public void ShowToast(string text)
    {
        toastText = text;
        toastTimer = ToastDuration;
    }

    // Captura: pierde lo que llevaba
    public void PlayerCaught()
    {
        if (PoliceAgent.Caught) return;
        PoliceAgent.Caught = true;
        carried.Clear();
        pendingExtraction = false;
        ShowToast("Te atrapó la policía: perdiste todo lo que llevabas");
        SceneManager.LoadScene("Hub");
    }

    void DrawLootNotices()
    {
        for (int i = 0; i < lootNotices.Count; i++)
        {
            float y = Screen.height - 110f - i * 26f;
            GUI.Box(new Rect(Screen.width / 2f - 260f, y, 520f, 24f), lootNotices[i].text);
        }
    }

    // Tiempo de venta según valor
    public static float SaleTime(int value)
    {
        return Mathf.Clamp(value * 0.05f, 10f, 600f);
    }

    void TickListings(float dt)
    {
        for (int i = listings.Count - 1; i >= 0; i--)
        {
            listings[i].timeLeft -= dt;
            if (listings[i].timeLeft <= 0f)
            {
                Item sold = listings[i].item;
                money += sold.value;
                listings.RemoveAt(i);

                // Acumular avisos por minuto
                salesThisMinute++;
                earningsThisMinute += sold.value;
            }
        }
    }

    // Resumen por minuto
    void TickSummary(float dt)
    {
        summaryTimer += dt;
        if (summaryTimer < SummaryInterval) return;

        summaryTimer -= SummaryInterval;

        if (salesThisMinute > 0)
        {
            toastText = $"Mercado negro (último minuto): +${earningsThisMinute} por {salesThisMinute} venta(s)";
            toastTimer = ToastDuration;
        }

        salesThisMinute = 0;
        earningsThisMinute = 0;
    }

    public bool PublishFromStash(int index)
    {
        if (listings.Count >= MaxListings) return false;
        if (index < 0 || index >= stash.Count) return false;

        Item item = stash[index];
        stash.RemoveAt(index);
        listings.Add(new MarketListing { item = item, timeLeft = SaleTime(item.value) });
        return true;
    }

    public float CarriedWeight()
    {
        float total = 0f;
        foreach (Item item in carried) total += item.weight;
        return total;
    }

    public bool TryPickUp(Item item)
    {
        if (CarriedWeight() + item.weight > BackpackCapacity) return false;
        carried.Add(item);
        return true;
    }

    public void SellCarried(int index)
    {
        money += carried[index].value;
        carried.RemoveAt(index);
    }

    public void KeepCarried(int index)
    {
        stash.Add(carried[index]);
        carried.RemoveAt(index);
    }

    public void SellStash(int index)
    {
        money += stash[index].value;
        stash.RemoveAt(index);
    }

    public void PayDebt(int amount)
    {
        int payment = Mathf.Min(amount, money, debt);
        money -= payment;
        debt -= payment;
        if (debt <= 0)
        {
            debt = 0;
            heistUnlocked = true;
            ShowToast("Deuda pagada: la caja fuerte ya se puede forzar");
        }
    }

    public bool TryBuyNextBackpack()
    {
        int next = backpackLevel + 1;
        if (next >= BackpackCapacities.Length) return false;
        if (money < BackpackPrices[next]) return false;

        money -= BackpackPrices[next];
        backpackLevel = next;
        return true;
    }
}
