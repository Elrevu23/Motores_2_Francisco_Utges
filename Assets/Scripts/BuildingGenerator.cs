using System;
using System.Collections.Generic;
using UnityEngine;

// Datos de un mueble (posición, giro, saqueable)
[Serializable]
public class FurnitureSpot
{
    public string name = "Mueble";
    public Vector2 position;
    public Vector3 size = Vector3.one;
    public bool decorative;
    public float yRotation;
}

// Generador de edificios huecos
public class BuildingGenerator : MonoBehaviour
{
    [Header("Tipo")]
    public LootPool lootPool = LootPool.Casa;
    public bool lootable = true;
    public bool sellZone = false;

    [Header("Medidas (metros)")]
    public int floors = 1;
    public float width = 8f;
    public float depth = 8f;
    public float height = 4f;
    public float wallThickness = 0.3f;

    [Header("Puerta")]
    public float doorWidth = 1.6f;
    public float doorHeight = 2.4f;

    [Header("Escalera")]
    public float stairWidth = 1.6f;
    public bool doubleStairs = false;

    [Header("Interior")]
    public bool roof = true;
    public bool furnished = true;
    public bool enterable = true;

    // Medidas de cada tipo de mueble
    private struct FurnitureType
    {
        public string name;
        public float len, height, depth;

        public FurnitureType(string name, float len, float height, float depth)
        {
            this.name = name;
            this.len = len;
            this.height = height;
            this.depth = depth;
        }
    }

    private static readonly FurnitureType[] furnitureTypes =
    {
        new FurnitureType("Ropero",   1.2f, 2.0f, 0.6f),
        new FurnitureType("Estante",  1.0f, 1.8f, 0.4f),
        new FurnitureType("Cama",     1.4f, 0.6f, 2.0f),
        new FurnitureType("Cajonera", 0.8f, 1.0f, 0.5f),
        new FurnitureType("Sofa",     1.8f, 0.8f, 0.8f),
        new FurnitureType("Mesa",     1.2f, 0.8f, 0.8f),
    };

    // Modelos de muebles (Kenney)
    private static readonly Dictionary<string, string> furnitureModels = new Dictionary<string, string>
    {
        { "Cama", "bedDouble" },
        { "Cama de guardado", "bedSingle" },
        { "Estante", "bookcaseClosedWide" },
        { "Ropero", "bookcaseClosedDoors" },
        { "Armario de almacen", "bookcaseClosedDoors" },
        { "Cajonera", "cabinetBedDrawer" },
        { "Sofa", "loungeSofa" },
        { "Mesa", "tableRound" },
        { "Estante grande", "bookcaseOpen" },
        { "Libreria", "bookcaseClosed" },
        { "Escritorio", "desk" },
        { "PC mercado negro", "computerScreen" },
        { "Silla", "chairDesk" },
        { "Mostrador", "kitchenCabinet" },
    };

    private const string modelFolder = "Assets/Models/Kenney/Models/FBX format/";

    // Muebles de casa y tienda
    private static readonly FurnitureType[] casaShelves =
    {
        new FurnitureType("Estante", 1.0f, 1.8f, 0.4f),
        new FurnitureType("Ropero",  1.2f, 2.0f, 0.6f),
    };

    private static readonly FurnitureType[] tiendaShelves =
    {
        new FurnitureType("Estante",  1.0f, 1.8f, 0.4f),
        new FurnitureType("Cajonera", 0.8f, 1.0f, 0.5f),
    };

    [ContextMenu("Generar edificio")]
    public void Generate()
    {
        // Borrar lo generado antes
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        int count = Mathf.Max(1, floors);
        float t = wallThickness;
        float totalH = height * count;


        float innerW = width - 2f * t;
        float innerD = depth - 2f * t;
        float leftInner = -width / 2f + t;
        float rightInner = width / 2f - t;
        float frontInner = -innerD / 2f;
        float backInner = innerD / 2f;

        // Paredes
        Block("Pared izquierda", new Vector3(-width / 2f + t / 2f, totalH / 2f, 0f), new Vector3(t, totalH, depth));
        Block("Pared derecha", new Vector3(width / 2f - t / 2f, totalH / 2f, 0f), new Vector3(t, totalH, depth));
        Block("Pared trasera", new Vector3(0f, totalH / 2f, depth / 2f - t / 2f), new Vector3(innerW, totalH, t));

        // Pared frontal con puerta
        float frontZ = -depth / 2f + t / 2f;
        float sideLen = (innerW - doorWidth) / 2f;
        float sideX = doorWidth / 2f + sideLen / 2f;
        Block("Pared frontal izq", new Vector3(-sideX, totalH / 2f, frontZ), new Vector3(sideLen, totalH, t));
        Block("Pared frontal der", new Vector3(sideX, totalH / 2f, frontZ), new Vector3(sideLen, totalH, t));
        float lintelH = height - doorHeight;
        if (lintelH > 0.01f)
            Block("Dintel", new Vector3(0f, doorHeight + lintelH / 2f, frontZ), new Vector3(doorWidth, lintelH, t));
        if (count > 1)
            Block("Pared frontal superior", new Vector3(0f, height + (totalH - height) / 2f, frontZ),
                  new Vector3(innerW, totalH - height, t));

        // Piso de planta baja
        Block("Piso", new Vector3(0f, 0.05f, 0f), new Vector3(width - 0.02f, 0.1f, depth - 0.02f));

        // Pisos superiores con huecos de escalera
        float midSlabW = innerW - 2f * stairWidth;
        for (int f = 1; f < count; f++)
        {
            float y = f * height;
            Block($"Piso {f + 1}", new Vector3(0f, y - 0.1f, 0f), new Vector3(midSlabW, 0.2f, innerD));
            CreateStairRails(f, y, leftInner, rightInner, frontInner, backInner);
        }

        // Huecos de escalera
        float leftStairX = leftInner + stairWidth / 2f;
        float rightStairX = rightInner - stairWidth / 2f;

        // Escaleras (una por piso, alternando)
        for (int k = 0; k < count - 1; k++)
        {
            // Dirección de cada tramo
            bool towardBack = k % 2 == 0;
            float y0 = k * height;
            float y1 = (k + 1) * height;
            float midY = (y0 + y1) / 2f;

            if (doubleStairs)
            {
                // Escalera doble
                CreateRamp(k, "izq", new Vector3(leftStairX, midY, 0f), height, innerD, towardBack);
                CreateRamp(k, "der", new Vector3(rightStairX, midY, 0f), height, innerD, towardBack);
            }
            else
            {
                // Escalera simple
                bool onLeft = k % 2 == 0;
                float x = onLeft ? leftStairX : rightStairX;
                CreateRamp(k, onLeft ? "izq" : "der", new Vector3(x, midY, 0f), height, innerD, towardBack);
            }
        }

        // Muebles por piso
        if (furnished && enterable)
        {
            for (int f = 0; f < count; f++)
            {
                // Hospital: solo el piso de arriba saqueable
                bool canLoot = lootable && !(lootPool == LootPool.Hospital && f == 0);
                List<FurnitureSpot> layout = BuildLayout(f, count, leftInner, rightInner, frontInner, backInner);
                foreach (FurnitureSpot spot in layout)
                    SpawnFurniture(spot, f * height, canLoot);
            }
        }

        // Puerta
        CreateDoor(!enterable);

        // Techo
        if (roof)
            Block("Techo", new Vector3(0f, totalH + 0.1f, 0f), new Vector3(width, 0.2f, depth));

        if (sellZone)
        {
            SpawnSafeHouseStations();
            SetupSellZone(totalH);
        }
    }

    // Casa de venta: cama y PC
    void SpawnSafeHouseStations()
    {
        // Cama (decoración)
        CreateFurnitureObject("Cama", new Vector3(-1f, 0f, 1.7f), new Vector3(1.4f, 0.6f, 2f));
        // Mesa con PC del mercado negro
        CreateFurnitureObject("Mesa", new Vector3(0.5f, 0f, 1.7f), new Vector3(1.2f, 0.8f, 0.8f));
        SpawnStation("PC mercado negro", StationKind.BlackMarket, new Vector3(0.5f, 1.0f, 1.7f), new Vector3(0.5f, 0.4f, 0.4f));
        // Silla de decoración
        GameObject silla = CreateFurnitureObject("Silla", new Vector3(0.5f, 0f, 0.9f), new Vector3(0.5f, 0.9f, 0.5f));
        silla.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // Mueble interactivo con trigger
    void SpawnStation(string stationName, StationKind kind, Vector3 center, Vector3 size)
    {
        GameObject st = CreateFurnitureObject(stationName, new Vector3(center.x, center.y - size.y / 2f, center.z), size);
        BoxCollider zone = st.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2.5f, 2f, 2.5f);
        zone.center = new Vector3(0f, size.y / 2f, 0f);
        st.AddComponent<SafeHouseStation>().kind = kind;
    }

    // Disposición fija de muebles por piso
    List<FurnitureSpot> BuildLayout(int floor, int count, float leftInner, float rightInner, float frontInner, float backInner)
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();
        int seed = Mathf.RoundToInt(transform.position.x * 100f) * 397
                 + Mathf.RoundToInt(transform.position.z * 100f) * 7919
                 + floor * 997 + 12345;
        System.Random rng = new System.Random(seed);

        // Layouts por edificio
        if (count == 1 && lootPool == LootPool.Casa)
            return SmallBuildingLayout(rng, leftInner, rightInner, frontInner, backInner);
        if (count == 1 && lootPool == LootPool.Tienda)
            return TiendaLayout(leftInner, rightInner, frontInner, backInner);
        if (count == 1 && lootPool == LootPool.Joyeria)
            return JewelryLayout();
        // Hospital planta baja y oficinas
        if (count > 1 && lootPool == LootPool.Hospital && floor == 0)
            return LobbyLayout();
        if (count > 1 && lootPool == LootPool.Hospital)
            return StorageLayout(rng);
        if (count > 1 && lootPool == LootPool.Oficina)
            return OfficeLayout(floor, leftInner, rightInner, frontInner, backInner, rng);

        bool multi = count > 1;
        // Camino de rampas libre
        bool routeBack = multi && floor % 2 == 1;
        float stairClear = multi ? stairWidth + 0.3f : 0f;
        float xMin = leftInner + stairClear;
        float xMax = rightInner - stairClear;
        float doorHalf = doorWidth / 2f + 0.4f;

        // Muebles al fondo
        if (!multi || !routeBack)
            PlaceRow(list, rng, xMin, xMax, backInner, -1f);

        // Muebles al frente
        if (!multi || routeBack)
        {
            if (!multi && floor == 0)
            {
                PlaceRow(list, rng, xMin, -doorHalf, frontInner, +1f);
                PlaceRow(list, rng, doorHalf, xMax, frontInner, +1f);
            }
            else
            {
                PlaceRow(list, rng, xMin, xMax, frontInner, +1f);
            }
        }

        // Muebles en paredes laterales (un piso)
        if (!multi)
        {
            PlaceSideRow(list, rng, leftInner, +1f, frontInner + 2.5f, backInner - 2.5f);
            PlaceSideRow(list, rng, rightInner, -1f, frontInner + 2.5f, backInner - 2.5f);
        }

        return list;
    }

    // Fila de muebles en pared de fondo o frente
    void PlaceRow(List<FurnitureSpot> list, System.Random rng, float xStart, float xEnd, float wallZ, float dir,
                  int limit = int.MaxValue, FurnitureType[] types = null)
    {
        float x = xStart;
        while (x < xEnd && list.Count < limit)
        {
            FurnitureType t = PickFitting(rng, xEnd - x, types ?? furnitureTypes);
            if (t.len <= 0f) break;

            float cx = x + t.len / 2f;
            float cz = wallZ + dir * t.depth / 2f;
            list.Add(new FurnitureSpot
            {
                name = t.name,
                position = new Vector2(cx, cz),
                size = new Vector3(t.len, t.height, t.depth)
            });

            x += t.len + 0.3f + (float)rng.NextDouble() * 0.9f;
        }
    }

    // Fila de muebles en pared lateral
    void PlaceSideRow(List<FurnitureSpot> list, System.Random rng, float wallX, float dir, float zStart, float zEnd,
                      int limit = int.MaxValue, FurnitureType[] types = null)
    {
        float z = zStart;
        while (z < zEnd && list.Count < limit)
        {
            FurnitureType t = PickFitting(rng, zEnd - z, types ?? furnitureTypes);
            if (t.len <= 0f) break;

            float cx = wallX + dir * t.depth / 2f;
            float cz = z + t.len / 2f;
            list.Add(new FurnitureSpot
            {
                name = t.name,
                position = new Vector2(cx, cz),
                size = new Vector3(t.depth, t.height, t.len)
            });

            z += t.len + 0.3f + (float)rng.NextDouble() * 0.9f;
        }
    }

    // Elegir mueble que entre en el espacio
    FurnitureType PickFitting(System.Random rng, float space, FurnitureType[] types)
    {
        List<FurnitureType> fit = new List<FurnitureType>();
        foreach (FurnitureType t in types)
        {
            if (t.len <= space) fit.Add(t);
        }

        if (fit.Count == 0)
            return new FurnitureType("", 0f, 0f, 0f);

        return fit[rng.Next(fit.Count)];
    }

    // Barandas de escalera
    void CreateStairRails(int floor, float y, float leftInner, float rightInner, float frontInner, float backInner)
    {
        const float gapLen = 1.8f;
        const float railThickness = 0.15f;
        const float railHeight = 1.0f;

        bool gapAtBack = floor % 2 == 1;
        float zStart = gapAtBack ? frontInner : frontInner + gapLen;
        float zEnd = gapAtBack ? backInner - gapLen : backInner;
        float len = zEnd - zStart;
        float cz = (zStart + zEnd) / 2f;

        float leftRailX = leftInner + stairWidth;
        float rightRailX = rightInner - stairWidth;
        Block($"Baranda {floor + 1} izq", new Vector3(leftRailX, y + railHeight / 2f, cz),
              new Vector3(railThickness, railHeight, len));
        Block($"Baranda {floor + 1} der", new Vector3(rightRailX, y + railHeight / 2f, cz),
              new Vector3(railThickness, railHeight, len));
    }

    // Crear mueble sólido
    GameObject CreateFurnitureObject(string furnitureName, Vector3 basePos, Vector3 size)
    {
        GameObject root = new GameObject(furnitureName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = basePos;

        BoxCollider solid = root.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, size.y / 2f, 0f);
        solid.size = size;

        GameObject model = LoadFurnitureModel(furnitureName);
        if (model != null)
        {
            GameObject inst = Instantiate(model, root.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            FitToBox(inst.transform, root.transform, size);
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = new Vector3(0f, size.y / 2f, 0f);
            cube.transform.localScale = size;
            DestroyImmediate(cube.GetComponent<BoxCollider>());
        }

        return root;
    }

    // Cargar modelo (solo editor)
    static GameObject LoadFurnitureModel(string furnitureName)
    {
#if UNITY_EDITOR
        if (!furnitureModels.TryGetValue(furnitureName, out string file)) return null;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(modelFolder + file + ".fbx");
#else
        return null;
#endif
    }

    // Escalar modelo al tamaño pedido
    static void FitToBox(Transform model, Transform space, Vector3 size)
    {
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;

        Bounds b = LocalBounds(rs, space);
        Vector3 factor = new Vector3(
            size.x / Mathf.Max(b.size.x, 0.001f),
            size.y / Mathf.Max(b.size.y, 0.001f),
            size.z / Mathf.Max(b.size.z, 0.001f));
        model.localScale = Vector3.Scale(model.localScale, factor);

        // Centrar modelo y apoyarlo en el piso
        b = LocalBounds(rs, space);
        model.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
    }

    // Caja de límites del modelo
    static Bounds LocalBounds(Renderer[] rs, Transform space)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Renderer r in rs)
        {
            Bounds wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                Vector3 corner = wb.center + Vector3.Scale(wb.extents, sign);
                Vector3 l = space.InverseTransformPoint(corner);
                min = Vector3.Min(min, l);
                max = Vector3.Max(max, l);
            }
        }

        Bounds result = new Bounds();
        result.SetMinMax(min, max);
        return result;
    }

    GameObject Block(string blockName, Vector3 localPos, Vector3 size, Vector3 rotation = default)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = blockName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.transform.localRotation = Quaternion.Euler(rotation);
        return go;
    }

    // Rampa inclinada
    void CreateRamp(int index, string side, Vector3 mid, float rise, float run, bool towardBack)
    {
        float length = Mathf.Sqrt(rise * rise + run * run);
        float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        float xRot = towardBack ? -angle : angle;

        // Ajuste de altura de la rampa
        Vector3 pos = mid + new Vector3(0f, -0.1f, 0f);
        Block($"Escalera {index + 1} {side}", pos, new Vector3(stairWidth, 0.2f, length), new Vector3(xRot, 0f, 0f));
    }

    // Muebles de casa y tienda de un piso
    List<FurnitureSpot> SmallBuildingLayout(System.Random rng, float leftInner, float rightInner, float frontInner, float backInner)
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();
        int target = 4 + rng.Next(2);
        bool bed = lootPool == LootPool.Casa;
        FurnitureType[] types = lootPool == LootPool.Casa ? casaShelves : tiendaShelves;
        float doorHalf = doorWidth / 2f + 0.4f;

        if (bed)
        {
            float bedX = leftInner + 0.3f + (float)rng.NextDouble() * (rightInner - leftInner - 1.4f - 0.6f);
            list.Add(Spot("Cama", bedX + 0.7f, backInner - 1f, 1.4f, 0.6f, 2f));
            // Espacio junto a la cama
            PlaceRow(list, rng, leftInner, bedX - 0.3f, backInner, -1f, target, types);
            PlaceRow(list, rng, bedX + 1.7f, rightInner, backInner, -1f, target, types);
        }
        else
        {
            PlaceRow(list, rng, leftInner, rightInner, backInner, -1f, target, types);
        }

        // Muebles junto a la puerta
        PlaceRow(list, rng, leftInner, -doorHalf, frontInner, +1f, target, types);
        PlaceRow(list, rng, doorHalf, rightInner, frontInner, +1f, target, types);

        // Muebles en paredes laterales
        PlaceSideRow(list, rng, leftInner, +1f, frontInner + 2.5f, backInner - 2.5f, target, types);
        PlaceSideRow(list, rng, rightInner, -1f, frontInner + 2.5f, backInner - 2.5f, target, types);

        return list;
    }

    // Joyería
    List<FurnitureSpot> JewelryLayout()
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();

        // Estantes de la joyería
        foreach (float x in new[] { -2.4f, 2.4f })
            foreach (float z in new[] { -1.1f, 1.1f })
                list.Add(Spot("Estante grande", x, z, 0.5f, 1.8f, 2f));

        // Librerías de la joyería
        foreach (float x in new[] { -4.4f, 4.4f })
            foreach (float z in new[] { -1.8f, 1.8f })
                list.Add(Spot("Libreria", x, z, 0.6f, 2.4f, 1.8f));

        // Mostrador al fondo
        list.Add(Decor(Turned(Spot("Mostrador", 0f, 2.2f, 4f, 1.1f, 0.8f))));

        return list;
    }

    // Estante en pared lateral
    static FurnitureSpot WallShelf(float x, float z)
    {
        FurnitureSpot s = Spot("Estante", x, z, 1.0f, 1.8f, 0.4f);
        s.yRotation = 90f;
        return s;
    }

    // Puerta articulada
    void CreateDoor(bool locked)
    {
        float frontZ = -depth / 2f + wallThickness / 2f;

        GameObject pivot = new GameObject("Puerta");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(-doorWidth / 2f, 0f, frontZ);

        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Hoja";
        leaf.transform.SetParent(pivot.transform, false);
        leaf.transform.localPosition = new Vector3(doorWidth / 2f, doorHeight / 2f, 0f);
        leaf.transform.localScale = new Vector3(doorWidth, doorHeight, 0.08f);

        // Zona de interacción de la puerta
        BoxCollider zone = pivot.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(doorWidth / 2f, 1.2f, 0f);
        zone.size = new Vector3(3f, 2.4f, 3f);

        Door door = pivot.AddComponent<Door>();
        door.locked = locked;
    }

    // Tienda
    List<FurnitureSpot> TiendaLayout(float leftInner, float rightInner, float frontInner, float backInner)
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();

        // Mostrador de la tienda
        list.Add(Decor(Turned(Spot("Mostrador", 0f, backInner - 1.2f, 3f, 1.1f, 0.8f))));

        // Estantes de la tienda
        foreach (float z in new[] { -2f, 0f })
        {
            list.Add(WallShelf(leftInner + 0.2f, z));
            list.Add(WallShelf(rightInner - 0.2f, z));
        }

        return list;
    }

    // Hospital planta de arriba: almacén
    List<FurnitureSpot> StorageLayout(System.Random rng)
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();
        float[] xs = { -6f, -2f, 2f, 6f };
        float[] zs = { -5f, 0f, 4f };

        foreach (float z in zs)
        {
            foreach (float x in xs)
            {
                if (rng.NextDouble() < 0.2) continue;
                float jx = x + (float)(rng.NextDouble() * 0.6 - 0.3);
                // Estanterías giradas
                FurnitureSpot shelf = Spot("Estante", jx, z, 1.0f, 1.8f, 0.4f);
                shelf.yRotation = 90f;
                list.Add(shelf);
            }
        }

        return list;
    }

    // Hospital planta baja: sala de espera
    List<FurnitureSpot> LobbyLayout()
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();

        // Sillones de espera
        foreach (float x in new[] { -3f, 0f, 3f })
        {
            list.Add(Decor(Spot("Sofa", x, -2f, 1.8f, 0.8f, 0.8f)));
            list.Add(Decor(Spot("Sofa", x, 2f, 1.8f, 0.8f, 0.8f)));
        }
        list.Add(Decor(Spot("Mesa", 0f, 0f, 1.2f, 0.8f, 0.8f)));

        // Mostrador de atención
        list.Add(Decor(Turned(Spot("Mostrador", -5f, -7f, 3f, 1.1f, 0.8f))));
        FurnitureSpot lobbyChair = Decor(Spot("Silla", -5f, -6f, 0.5f, 0.9f, 0.5f));
        lobbyChair.yRotation = 180f;
        list.Add(lobbyChair);

        return list;
    }

    // Oficinas
    List<FurnitureSpot> OfficeLayout(int floor, float leftInner, float rightInner, float frontInner, float backInner, System.Random rng)
    {
        List<FurnitureSpot> list = new List<FurnitureSpot>();
        bool routeBack = floor % 2 == 1;
        float wallZ = routeBack ? frontInner : backInner;
        float dir = routeBack ? 1f : -1f;
        float xMin = leftInner + stairWidth + 0.3f;
        float xMax = rightInner - stairWidth - 0.3f;

        float x = xMin;
        while (x + 1.2f <= xMax)
        {
            float cx = x + 0.6f;
            if (rng.NextDouble() < 0.6)
            {
                // Escritorio con silla
                list.Add(Spot("Escritorio", cx, wallZ + dir * 0.4f, 1.2f, 0.8f, 0.8f));
                FurnitureSpot chair = Spot("Silla", cx, wallZ + dir * 1.1f, 0.5f, 0.9f, 0.5f);
                chair.yRotation = dir < 0 ? 0f : 180f;
                list.Add(chair);
            }
            else
            {
                // Estantería
                list.Add(Spot("Estante", cx, wallZ + dir * 0.2f, 1.0f, 1.8f, 0.4f));
            }
            x += 1.5f + (float)rng.NextDouble() * 0.6f;
        }

        return list;
    }

    // Girar mueble 180°
    static FurnitureSpot Turned(FurnitureSpot s)
    {
        s.yRotation += 180f;
        return s;
    }

    // Marcar mueble como decoración
    static FurnitureSpot Decor(FurnitureSpot s)
    {
        s.decorative = true;
        return s;
    }

    // Crear mueble con posición y tamaño
    static FurnitureSpot Spot(string name, float x, float z, float sizeX, float height, float sizeZ)
    {
        return new FurnitureSpot { name = name, position = new Vector2(x, z), size = new Vector3(sizeX, height, sizeZ) };
    }

    void SpawnFurniture(FurnitureSpot spot, float yOffset, bool canLoot)
    {
        Vector3 pos = new Vector3(spot.position.x, yOffset, spot.position.y);
        GameObject mueble = CreateFurnitureObject(spot.name, pos, spot.size);
        mueble.transform.localRotation = Quaternion.Euler(0f, spot.yRotation, 0f);

        if (!canLoot || spot.decorative) return;

        // Trigger del mueble
        BoxCollider zone = mueble.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2.5f, 2f, 2.5f);
        zone.center = new Vector3(0f, spot.size.y / 2f, 0f);

        Lootable loot = mueble.AddComponent<Lootable>();
        loot.pool = lootPool;
    }

    // Zona segura de venta
    void SetupSellZone(float totalH)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, totalH / 2f, 0f);
        box.size = new Vector3(width - 2f * wallThickness, totalH, depth - 2f * wallThickness);

        if (GetComponent<SafeZone>() == null)
            gameObject.AddComponent<SafeZone>();
    }
}
