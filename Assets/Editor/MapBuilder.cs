using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Generar el mapa de la ciudad
public static class MapBuilder
{
    private struct BuildingDef
    {
        public string name;
        public Vector3 pos;
        public LootPool pool;
        public bool lootable;
        public bool sellZone;
        public bool furnished;
        public float width, depth, height;
        public int floors;
        public Vector3 door;
        public bool enterable;
        public bool doubleStairs;

        public BuildingDef(string name, float x, float z, LootPool pool, bool lootable, bool sellZone = false,
                           float width = 8f, float depth = 8f, float height = 4f, int floors = 1,
                           bool furnished = true, Vector3 door = default, bool enterable = true, bool doubleStairs = false)
        {
            this.enterable = enterable;
            this.doubleStairs = doubleStairs;
            this.name = name;
            pos = new Vector3(x, 0f, z);
            this.pool = pool;
            this.lootable = lootable;
            this.sellZone = sellZone;
            this.furnished = furnished;
            this.width = width;
            this.depth = depth;
            this.height = height;
            this.floors = floors;
            this.door = door;
        }
    }

    private static readonly Vector3 East = Vector3.right;
    private static readonly Vector3 West = Vector3.left;
    private static readonly Vector3 North = Vector3.back;
    private static readonly Vector3 South = Vector3.forward;

    private static readonly BuildingDef[] buildings =
    {
        // Ubicación de los edificios del centro
        new BuildingDef("Mapa_Comisaria",      21f,   0f, LootPool.Oficina,  false, false, 22f, 18f, 3.2f, 2, false, East, false),
        new BuildingDef("Mapa_Hospital",      -18f,   0f, LootPool.Hospital, true,  false, 22f, 18f, 3.2f, 2, true, West, true, true),
        new BuildingDef("Mapa_Joyeria",         2f,   0f, LootPool.Joyeria,  true,  false, 10f,  8f, 4f,   1, true, North),
        new BuildingDef("Mapa_Oficina_Norte",  21f, -18f, LootPool.Oficina,  true,  false, 10f, 10f, 3.2f, 4, true, North),
        new BuildingDef("Mapa_Oficina_Sur",    21f,  18f, LootPool.Oficina,  true,  false, 10f, 10f, 3.2f, 4, true, South),

        // Casas de la orilla: columna este
        new BuildingDef("Mapa_Casa_E1",       46f, -40f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Tienda_E1",     46f, -20f, LootPool.Tienda,  true),
        new BuildingDef("Mapa_Casa_E2",       46f,   0f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Tienda_E2",     46f,  20f, LootPool.Tienda,  true),
        new BuildingDef("Mapa_Casa_E3",       46f,  40f, LootPool.Casa,    true),

        // Casas de la orilla: columna oeste
        new BuildingDef("Mapa_Casa_O1",      -46f, -40f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Casa_O2",      -46f, -20f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Tienda_O1",    -46f,   0f, LootPool.Tienda,  true),
        new BuildingDef("Mapa_Casa_O3",      -46f,  20f, LootPool.Casa,    true),

        // Casas de la orilla: norte y sur
        new BuildingDef("Mapa_Casa_N1",      -20f, -44f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Tienda_N1",      0f, -44f, LootPool.Tienda,  true),
        new BuildingDef("Mapa_Casa_N2",       20f, -44f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Casa_S1",      -20f,  44f, LootPool.Casa,    true),
        new BuildingDef("Mapa_Tienda_S1",      0f,  44f, LootPool.Tienda,  true),
        new BuildingDef("Mapa_Casa_S2",       20f,  44f, LootPool.Casa,    true),

        // Casa de venta
        new BuildingDef("Mapa_Casa_Venta",   -46f, -10f, LootPool.Casa,    false, true, 6f, 6f, 3.2f, 1, false),
    };

    // Calles del anillo
    private static readonly Vector4[] roadSegments =
    {
        // Formato de cada calle
        new Vector4(  0f, -32f, 100f, 8f),
        new Vector4(  0f,  32f, 100f, 8f),
        new Vector4(-37f,   0f,   8f, 100f),
        new Vector4( 37f,   0f,   8f, 100f),
        new Vector4(  2f, -18f,   8f,  28f),
    };

    // Punto de salida de policías
    private static readonly Vector3[] policePoints =
    {
        new Vector3(33.5f, 1f, -2f),
        new Vector3(33.5f, 1f, -0.7f),
        new Vector3(33.5f, 1f,  0.7f),
        new Vector3(33.5f, 1f,  2f),
    };

    // Punto de inicio del jugador
    private static readonly Vector3 playerSpawn = new Vector3(-46f, 1f, -10f);

    [MenuItem("Extraccion/Generar mapa")]
    public static void Build()
    {
        ClearOld();

        // Pasto de fondo
        Material grass = GetMaterial("Assets/Materials/Pasto.mat", CreateGrassTexture(), new Vector2(20f, 20f), Color.white);
        CreateGround(grass);

        Material asphalt = GetMaterial("Assets/Materials/Asfalto.mat", AsphaltTex, new Vector2(12f, 12f), Color.white);

        for (int i = 0; i < roadSegments.Length; i++)
            CreateRoad(i + 1, roadSegments[i], asphalt);

        foreach (BuildingDef def in buildings)
            CreateBuilding(def);

        for (int i = 0; i < policePoints.Length; i++)
            CreatePolice(i + 1, policePoints[i]);

        // Caja fuerte
        CreateFinalSafe(new Vector3(0f, 0f, 0f));

        GameObject player = GameObject.Find("Capsule");
        if (player != null)
            player.transform.position = playerSpawn;

        ApplySkins();

        CreateExitMarker();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Mapa generado: {buildings.Length} edificios, {roadSegments.Length} calles y {policePoints.Length} policías.");
    }

    // Marca verde de la salida: se crea en el editor y queda guardada en la escena
    static void CreateExitMarker()
    {
        GameObject zone = GameObject.Find("Zona_Salida");
        if (zone == null)
        {
            Debug.LogWarning("No hay Zona_Salida en la escena: no se creó la marca verde.");
            return;
        }
        if (zone.transform.Find("Marca_Salida") != null) return;

        GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marca.name = "Marca_Salida";
        Object.DestroyImmediate(marca.GetComponent<Collider>());
        marca.transform.SetParent(zone.transform, false);
        marca.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        marca.transform.localScale = new Vector3(4f, 0.1f, 4f);
        marca.GetComponent<Renderer>().sharedMaterial = GetMaterial("Assets/Materials/Salida.mat", "", Vector2.one, new Color(0.2f, 1f, 0.35f));
    }

    // Caja fuerte (trigger)
    static void CreateFinalSafe(Vector3 position)
    {
        GameObject safe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        safe.name = "Mapa_CajaFinal";
        safe.transform.position = position + Vector3.up * 0.5f;
        safe.transform.localScale = new Vector3(1.2f, 1f, 0.8f);

        Renderer r = safe.GetComponent<Renderer>();
        r.sharedMaterial = GetMaterial("Assets/Materials/CajaFinal.mat", "", Vector2.one, new Color(0.15f, 0.15f, 0.18f));

        SphereCollider zone = safe.AddComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = 2.5f;

        safe.AddComponent<FinalHeist>();
    }

    static void ClearOld()
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            // Saltar objetos ya borrados
            if (t == null || t.parent != null) continue;

            string n = t.name;
            if (n.StartsWith("Mapa_") || n.StartsWith("Policia_") || n == "Casa_1" || n == "Capsule (1)")
                Object.DestroyImmediate(t.gameObject);
        }
    }

    // Textura de pasto
    static string CreateGrassTexture()
    {
        const string assetPath = "Assets/Textures/Pasto.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) != null)
            return assetPath;

        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        Color dark = new Color(0.18f, 0.36f, 0.12f);
        Color light = new Color(0.34f, 0.58f, 0.22f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f
                        + Mathf.PerlinNoise(x * 0.25f + 50f, y * 0.25f + 50f) * 0.4f;
                Color c = Color.Lerp(dark, light, n);
                // Textura: briznas
                float speck = Random.value;
                if (speck > 0.92f) c *= 1.15f;
                else if (speck < 0.04f) c *= 0.8f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        string fullPath = Path.Combine(Application.dataPath, "Textures/Pasto.png");
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath);
        return assetPath;
    }

    // Plano de pasto
    static void CreateGround(Material mat)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Mapa_Pasto";
        ground.transform.position = new Vector3(0f, 0.005f, 0f);
        ground.transform.localScale = new Vector3(12f, 1f, 12f);
        ground.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Texturas de calles y fachadas
    private const string BrickTex = "Assets/Models/Downtown/Textures/T_RedBrick_BaseColor.png";
    private const string RoofTex = "Assets/Models/Downtown/Textures/T_RoofSlate_BaseColor.png";
    private const string AsphaltTex = "Assets/Models/Downtown/Textures/T_Concrete_Asphalt_BaseColor.png";
    private const string MarbleTex = "Assets/Models/Downtown/Textures/T_MarbleFloor_BaseColor.png";
    private const string ConcreteTex = "Assets/Models/Downtown/Textures/T_Concrete_BaseColor.png";

    // Material guardado como asset
    static Material GetMaterial(string path, string texturePath, Vector2 tiling, Color tint)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tiling);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Materiales de paredes y techos
    static void ApplyFacade(Transform root, Material wall, Material roof)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (r.name.StartsWith("Pared") || r.name.StartsWith("Dintel") || r.name.StartsWith("Fachada"))
                r.sharedMaterial = wall;
            else if (r.name == "Techo")
                r.sharedMaterial = roof;
        }
    }

    // Pisos interiores
    static void ApplyFloor(Transform root, Material floor)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (r.name.StartsWith("Piso"))
                r.sharedMaterial = floor;
        }
    }

    static void CreateRoad(int number, Vector4 segment, Material mat)
    {
        // Segmentos de calle
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"Mapa_Calle_{number}";
        // Altura de calles sin parpadeo
        float height = 0.02f + number * 0.002f;
        road.transform.position = new Vector3(segment.x, height, segment.y);
        road.transform.localScale = new Vector3(segment.z, 0.04f, segment.w);
        road.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Dirección de la puerta
    static Vector3 DoorDirection(BuildingDef def)
    {
        if (def.door != Vector3.zero) return def.door.normalized;

        Vector3 p = def.pos;
        if (Mathf.Abs(p.x) >= Mathf.Abs(p.z))
            return new Vector3(-Mathf.Sign(p.x), 0f, 0f);
        return new Vector3(0f, 0f, -Mathf.Sign(p.z));
    }

    static void CreateBuilding(BuildingDef def)
    {
        // Color por tipo de edificio
        string key = FacadeKey(def);
        Color tint = FacadeTint(key);
        Material wall = GetMaterial($"Assets/Materials/Fachada_{key}.mat", BrickTex, new Vector2(4f, 4f), tint);
        Material roof = GetMaterial($"Assets/Materials/Techo_{key}.mat", RoofTex, new Vector2(3f, 3f), Darker(tint));
        Material floor = FloorMaterial(key);

        GameObject root = new GameObject(def.name);
        root.transform.position = def.pos;

        // Rotar edificio hacia la calle
        Vector3 door = DoorDirection(def);
        root.transform.rotation = Quaternion.LookRotation(-door);

        BuildingGenerator bg = root.AddComponent<BuildingGenerator>();
        bg.lootPool = def.pool;
        bg.lootable = def.lootable;
        bg.sellZone = def.sellZone;
        bg.furnished = def.furnished;
        bg.enterable = def.enterable;
        bg.doubleStairs = def.doubleStairs;
        bg.width = def.width;
        bg.depth = def.depth;
        bg.height = def.height;
        bg.floors = def.floors;
        bg.Generate();
        ApplyFacade(root.transform, wall, roof);
        ApplyFloor(root.transform, floor);
    }

    // Piso por tipo de edificio
    static Material FloorMaterial(string key)
    {
        string texture;
        Color tint;
        switch (key)
        {
            case "Hospital": texture = MarbleTex; tint = new Color(0.92f, 0.97f, 1.00f); break;
            case "Joyeria": texture = MarbleTex; tint = new Color(1.00f, 0.92f, 0.75f); break;
            case "Venta": texture = MarbleTex; tint = new Color(0.85f, 1.00f, 0.85f); break;
            case "Tienda": texture = ConcreteTex; tint = new Color(0.95f, 0.85f, 0.75f); break;
            case "Comisaria": texture = ConcreteTex; tint = new Color(0.55f, 0.60f, 0.72f); break;
            case "Oficina": texture = ConcreteTex; tint = new Color(0.78f, 0.78f, 0.80f); break;
            default: texture = ConcreteTex; tint = new Color(0.62f, 0.45f, 0.30f); break;
        }
        return GetMaterial($"Assets/Materials/Piso_{key}.mat", texture, new Vector2(3f, 3f), tint);
    }

    // Fachada por tipo de edificio
    static string FacadeKey(BuildingDef def)
    {
        if (def.name.Contains("Hospital")) return "Hospital";
        if (def.name.Contains("Comisaria")) return "Comisaria";
        if (def.name.Contains("Joyeria")) return "Joyeria";
        if (def.name.Contains("Oficina")) return "Oficina";
        if (def.name.Contains("Tienda")) return "Tienda";
        if (def.sellZone) return "Venta";
        return "Casa";
    }

    // Color de fachada
    static Color FacadeTint(string key)
    {
        switch (key)
        {
            case "Hospital": return new Color(0.85f, 0.95f, 1.00f);
            case "Comisaria": return new Color(0.30f, 0.40f, 0.75f);
            case "Joyeria": return new Color(1.00f, 0.80f, 0.35f);
            case "Oficina": return new Color(0.55f, 0.60f, 0.65f);
            case "Tienda": return new Color(1.00f, 0.55f, 0.30f);
            case "Venta": return new Color(0.55f, 0.85f, 0.50f);
            default: return Color.white;
        }
    }

    // Color de techo
    static Color Darker(Color c)
    {
        return new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f);
    }

    // Ruta de patrulla
    private static readonly Vector3[] patrolRoute =
    {
        new Vector3( 37f, 1f, -32f),
        new Vector3( 37f, 1f,  32f),
        new Vector3(-37f, 1f,  32f),
        new Vector3(-37f, 1f, -32f),
    };

    static void CreatePolice(int number, Vector3 position)
    {
        GameObject police = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        police.name = $"Policia_{number}";
        police.transform.position = position;

        PoliceAgent agent = police.AddComponent<PoliceAgent>();
        agent.mode = PoliceAgent.PatrolMode.Waypoints;

        // Crear waypoints
        agent.waypoints = new Transform[patrolRoute.Length];
        for (int k = 0; k < patrolRoute.Length; k++)
        {
            Vector3 p = patrolRoute[(k + number - 1) % patrolRoute.Length];
            GameObject wp = new GameObject($"Mapa_Ruta_{number}_{k + 1}");
            wp.transform.position = p;
            agent.waypoints[k] = wp.transform;
        }
    }

    // Skins de jugador y policías
    const string PlayerModelPath = "Assets/Gang members - characters pack/Models/Man_2.fbx";
    const string CopModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_02.fbx";

    // Rehacer solo las skins
    [MenuItem("Extraccion/Aplicar skins")]
    public static void ApplySkinsMenu()
    {
        ApplySkins();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Skins aplicadas. Guarda la escena (Ctrl+S).");
    }

    static void ApplySkins()
    {
        // Materiales de paquetes a URP
        CharacterMaterialFixer.ConvertCharacterMaterials();

        // Clips compartidos
        SetAnimationType(PlayerModelPath, ModelImporterAnimationType.Human);
        SetAnimationType(CopModelPath, ModelImporterAnimationType.Human);

        GameObject playerModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        GameObject copModel = AssetDatabase.LoadAssetAtPath<GameObject>(CopModelPath);
        if (playerModel == null || copModel == null)
        {
            Debug.LogWarning("No se encontraron los modelos de personaje o policía.");
            return;
        }

        // Animator del jugador
        AnimatorController playerController = PlayerAnimatorSetup.EnsureController();
        GameObject player = GameObject.Find("Capsule");
        if (player != null)
        {
            GameObject playerSkin = SkinCapsule(player.transform, playerModel);
            if (playerController == null)
                Debug.LogWarning("Animator del jugador: no se pudo crear Jugador.controller.");
            if (playerSkin != null)
            {
                // Desempaquetar para guardar el Animator
                if (PrefabUtility.IsPartOfPrefabInstance(playerSkin))
                    PrefabUtility.UnpackPrefabInstance(playerSkin, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                Animator anim = playerSkin.GetComponent<Animator>();
                if (anim == null) anim = playerSkin.AddComponent<Animator>();
                if (playerController != null)
                    anim.runtimeAnimatorController = playerController;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath).OfType<Avatar>().FirstOrDefault(a => a.isHuman);
                if (avatar != null) anim.avatar = avatar;
                else Debug.LogWarning("Animator del jugador: no se encontró el Avatar humanoide de male_casual.");

                EditorUtility.SetDirty(playerSkin);
            }
            if (player.GetComponent<PlayerAnimatorDriver>() == null)
                player.AddComponent<PlayerAnimatorDriver>();
        }

        // Animator de policías
        AnimatorController policeController = PlayerAnimatorSetup.EnsurePoliceController();
        Avatar copAvatar = AssetDatabase.LoadAllAssetsAtPath(CopModelPath).OfType<Avatar>().FirstOrDefault(a => a.isHuman);
        if (copAvatar == null)
            Debug.LogWarning("Policía: no se pudo crear el Avatar humanoide del modelo; quedará sin animación.");

        for (int i = 1; i <= policePoints.Length; i++)
        {
            GameObject police = GameObject.Find($"Policia_{i}");
            if (police == null) continue;

            GameObject copSkin = SkinCapsule(police.transform, copModel);
            if (copSkin == null) continue;

            if (PrefabUtility.IsPartOfPrefabInstance(copSkin))
                PrefabUtility.UnpackPrefabInstance(copSkin, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            Animator copAnim = copSkin.GetComponent<Animator>();
            if (copAnim == null) copAnim = copSkin.AddComponent<Animator>();
            copAnim.runtimeAnimatorController = policeController;
            copAnim.avatar = copAvatar;
            copAnim.applyRootMotion = false;
            copAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(copSkin);

            if (police.GetComponent<PoliceAnimatorDriver>() == null)
                police.AddComponent<PoliceAnimatorDriver>();
        }
    }

    // Tipo de rig del FBX
    static void SetAnimationType(string path, ModelImporterAnimationType type)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        if (importer.animationType != type)
        {
            importer.animationType = type;
            importer.SaveAndReimport();
        }
    }

    static GameObject SkinCapsule(Transform capsule, GameObject model)
    {
        Transform old = capsule.Find("Skin");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        MeshRenderer capsuleMesh = capsule.GetComponent<MeshRenderer>();
        if (capsuleMesh != null)
            capsuleMesh.enabled = false;

        GameObject skin = (GameObject)PrefabUtility.InstantiatePrefab(model);
        skin.name = "Skin";
        skin.transform.SetParent(capsule, false);
        skin.transform.localPosition = Vector3.zero;
        skin.transform.localRotation = Quaternion.identity;
        skin.transform.localScale = Vector3.one;

        // Quedarse con una armadura
        KeepFirstCharacter(skin.transform);

        // Escala 1,8 m
        Bounds b = RendererBounds(skin);
        if (b.size.y > 0.001f)
            skin.transform.localScale = Vector3.one * (1.8f / b.size.y);

        // Centrar y apoyar el modelo
        b = RendererBounds(skin);
        skin.transform.position += new Vector3(
            capsule.position.x - b.center.x,
            capsule.position.y - 1f - b.min.y,
            capsule.position.z - b.center.z);

        return skin;
    }

    static void KeepFirstCharacter(Transform skin)
    {
        // Borrar armaduras extra
        int rigs = 0;
        foreach (Transform child in skin)
            if (child.name.StartsWith("metarig")) rigs++;
        if (rigs < 2) return;

        bool kept = false;
        for (int i = 0; i < skin.childCount; i++)
        {
            Transform child = skin.GetChild(i);
            if (child.GetComponentInChildren<Renderer>() == null) continue;

            if (!kept)
            {
                kept = true;
                continue;
            }
            Object.DestroyImmediate(child.gameObject);
            i--;
        }
    }

    static Bounds RendererBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);
        return b;
    }
}
