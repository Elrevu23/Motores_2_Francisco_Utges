using System;
using System.Collections.Generic;
using UnityEngine;

// Saqueo manteniendo E
public class Lootable : MonoBehaviour
{
    public LootPool pool = LootPool.Casa;
    public float holdTime = 5f;
    public int minItems = 0;
    public int maxItems = 4;
    public float refillMinutes = 10f;
    public bool randomEachRun = false;

    private class FurnitureState
    {
        public long refillAtTicks;
        public int cycle;
    }

    // Cooldown guardado estático
    private static readonly Dictionary<string, FurnitureState> states = new Dictionary<string, FurnitureState>();

    private readonly List<Item> contents = new List<Item>();
    private FurnitureState state;
    private string key;
    private float holdTimer;
    private bool needsRelease;
    private string message = "";

    void Start()
    {
        Vector3 p = transform.position;
        key = $"{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}_{Mathf.RoundToInt(p.z * 10f)}_{pool}";

        if (!states.TryGetValue(key, out state))
        {
            state = new FurnitureState();
            states[key] = state;
        }

        if (IsCoolingDown()) return;
        Fill();
    }

    void Update()
    {
        if (ThirdPersonCamera.Aimed != this)
        {
            holdTimer = 0f;
            message = "";
            return;
        }

        if (IsCoolingDown())
        {
            holdTimer = 0f;
            return;
        }

        if (state.refillAtTicks != 0)
        {
            // Rellenar al terminar el cooldown
            state.refillAtTicks = 0;
            state.cycle++;
            Fill();
        }

        if (contents.Count == 0)
        {
            holdTimer = 0f;
            return;
        }

        if (!Input.GetKey(KeyCode.E))
        {
            holdTimer = 0f;
            needsRelease = false;
            return;
        }

        if (needsRelease) return;

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdTime)
        {
            holdTimer = 0f;
            needsRelease = true;
            Loot();
        }
    }

    bool IsCoolingDown()
    {
        return state.refillAtTicks != 0 && DateTime.UtcNow.Ticks < state.refillAtTicks;
    }

    void Fill()
    {
        contents.Clear();

        int seed = randomEachRun ? UnityEngine.Random.Range(0, int.MaxValue) : SeedFromPosition() + state.cycle * 1009;
        System.Random rng = new System.Random(seed);

        int count = rng.Next(minItems, maxItems + 1);
        for (int i = 0; i < count; i++)
            contents.Add(ItemFactory.Create(rng, pool));
    }

    int SeedFromPosition()
    {
        Vector3 p = transform.position;
        return Mathf.RoundToInt(p.x * 100f) * 397 + Mathf.RoundToInt(p.y * 100f) * 613 + Mathf.RoundToInt(p.z * 100f) * 7919 + (int)pool * 31;
    }

    void Loot()
    {
        for (int i = contents.Count - 1; i >= 0; i--)
        {
            if (GameData.Instance.TryPickUp(contents[i]))
            {
                GameData.Instance.NotifyLoot(contents[i]);
                SoundFX.Loot();
                FX.Burst(transform.position + Vector3.up * 0.5f, Color.yellow, 15);
                contents.RemoveAt(i);
            }
        }

        if (contents.Count == 0)
        {
            state.refillAtTicks = DateTime.UtcNow.AddMinutes(refillMinutes).Ticks;
            message = "";
        }
        else
        {
            message = "Mochila llena";
        }
    }

    // Texto del aviso de saqueo
    public string Prompt()
    {
        if (IsCoolingDown())
            return "Vacío";
        if (holdTimer > 0f)
            return $"Saqueando... {(int)(holdTimer / holdTime * 100f)}%";
        if (contents.Count == 0)
            return "Vacío";
        if (message != "")
            return message;
        return "Mantené [E] para saquear";
    }
}
