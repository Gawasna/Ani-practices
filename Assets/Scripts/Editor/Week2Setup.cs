using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using Cinemachine;
using System.Collections.Generic;
using System.IO;

public static class Week2Setup
{
    [MenuItem("Week2/Setup Assignment Scenes")]
    public static void RunFullSetup()
    {
        // 1. Setup Enemy Animation & Controller
        SetupEnemyAnimation();

        // 2. Setup Ground Sprite
        SetupGroundSprite();

        // 3. Slice Tilesets
        SliceTilesets();

        // 4. Generate Tile Assets
        var tileDict = GenerateTileAssets();

        // 5. Create Palette Prefab
        CreateTilePalettePrefab(tileDict);

        // 6. Build Scene1 (Thuc hanh 1.3: Components & Cinemachine)
        BuildScene1();

        // 7. Build Scene2 (Thuc hanh 1.4: Tilemap & Palette)
        BuildScene2();

        // 8. Configure Build Settings
        UpdateBuildSettings();

        // 9. Remove legacy SampleScene
        RemoveLegacySampleScene();

        Debug.Log("Week 2 setup completed: Scene1 and Scene2 created.");
    }

    [MenuItem("Week2/Fix Scene1 Camera")]
    public static void FixScene1Camera()
    {
        string scenePath = "Assets/Scenes/Scene1.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath);

        // 1. Setup Main Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0, 0.38f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.75f, 0.95f);

        var brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = cam.gameObject.AddComponent<CinemachineBrain>();
        }
        brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;

        // 2. Setup Follow Camera (destroy and recreate to ensure clean pipeline)
        GameObject vcamObj = GameObject.Find("Follow Camera");
        if (vcamObj != null)
        {
            Object.DestroyImmediate(vcamObj);
        }

        vcamObj = new GameObject("Follow Camera");
        vcamObj.transform.position = new Vector3(0, 0.38f, -10f);

        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        var goober = GameObject.Find("Goober (Enemy)");
        if (goober != null)
        {
            vcam.Follow = goober.transform;
        }
        vcam.m_Lens.OrthographicSize = 4.5f;
        vcam.m_Lens.NearClipPlane = 0.1f;
        vcam.m_Lens.FarClipPlane = 100f;

        var framing = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framing.m_CameraDistance = 10f;
        framing.m_XDamping = 1f;
        framing.m_YDamping = 1f;
        framing.m_ZDamping = 1f;
        framing.m_ScreenX = 0.5f;
        framing.m_ScreenY = 0.5f;
        framing.m_DeadZoneWidth = 0.1f;
        framing.m_DeadZoneHeight = 0.1f;

        EditorUtility.SetDirty(cam.gameObject);
        EditorUtility.SetDirty(vcamObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Scene1 camera fixed successfully: sky blue background, position Z=-10, Framing Transposer active.");
    }

    private static void SetupGroundSprite()
    {
        string path = "Assets/Art/Sprites/GroundBlock.png";
        if (!File.Exists(path)) return;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void SetupEnemyAnimation()
    {
        string animPath = "Assets/Animations/Goober_Idle.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.frameRate = 30;
            AnimationCurve curveX = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1.08f),
                new Keyframe(1f, 1f)
            );
            AnimationCurve curveY = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.92f),
                new Keyframe(1f, 1f)
            );
            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", curveX);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.y", curveY);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.z", AnimationCurve.Constant(0f, 1f, 1f));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, animPath);
        }

        string controllerPath = "Assets/Animations/Goober.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            var state = rootStateMachine.AddState("Goober_Idle");
            state.motion = clip;
        }
    }

    private static Vector2Int GetPngDimensions(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
        {
            byte[] buffer = new byte[24];
            stream.Read(buffer, 0, 24);
            int width = (buffer[16] << 24) | (buffer[17] << 16) | (buffer[18] << 8) | buffer[19];
            int height = (buffer[20] << 24) | (buffer[21] << 16) | (buffer[22] << 8) | buffer[23];
            return new Vector2Int(width, height);
        }
    }

    private static void SliceTilesets()
    {
        string[] tilesetPaths = new[] {
            "Assets/Art/Tilesets/TilesetField.png",
            "Assets/Art/Tilesets/TilesetNature.png"
        };

        foreach (var path in tilesetPaths)
        {
            if (!File.Exists(path)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            Vector2Int dims = GetPngDimensions(path);
            int tileSize = 16;
            int cols = dims.x / tileSize;
            int rows = dims.y / tileSize;
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            string baseName = Path.GetFileNameWithoutExtension(path);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.rect = new Rect(c * tileSize, (rows - 1 - r) * tileSize, tileSize, tileSize);
                    meta.name = $"{baseName}_{c}_{r}";
                    meta.alignment = (int)SpriteAlignment.Center;
                    metas.Add(meta);
                }
            }
#pragma warning disable CS0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<string, Tile> GenerateTileAssets()
    {
        var tileDict = new Dictionary<string, Tile>();
        string[] tilesetPaths = new[] {
            "Assets/Art/Tilesets/TilesetField.png",
            "Assets/Art/Tilesets/TilesetNature.png"
        };

        foreach (var path in tilesetPaths)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    string tileAssetPath = $"Assets/Art/Tiles/{sprite.name}.asset";
                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        AssetDatabase.CreateAsset(tile, tileAssetPath);
                    }
                    tileDict[sprite.name] = tile;
                }
            }
        }
        AssetDatabase.SaveAssets();
        return tileDict;
    }

    private static void CreateTilePalettePrefab(Dictionary<string, Tile> tileDict)
    {
        string palettePath = "Assets/Palettes/NaturePalette.prefab";
        GameObject paletteObj = new GameObject("NaturePalette");
        Grid grid = paletteObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject layerObj = new GameObject("Layer1");
        layerObj.transform.parent = paletteObj.transform;
        Tilemap tilemap = layerObj.AddComponent<Tilemap>();
        layerObj.AddComponent<TilemapRenderer>();

        int col = 0, row = 0;
        foreach (var kvp in tileDict)
        {
            tilemap.SetTile(new Vector3Int(col, -row, 0), kvp.Value);
            col++;
            if (col >= 8)
            {
                col = 0;
                row++;
                if (row >= 8) break;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(paletteObj, palettePath);
        Object.DestroyImmediate(paletteObj);
    }

    private static void BuildScene1()
    {
        string scenePath = "Assets/Scenes/Scene1.unity";
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera with CinemachineBrain
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, 0, -10);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.17f);
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<CinemachineBrain>();

        // 2. Tilemaps: Background (1_21), Surface (1_7), Underground (1_8)
        GameObject gridObj = new GameObject("Grid");
        gridObj.AddComponent<Grid>();

        // Background (tile 1_21)
        GameObject bgObj = new GameObject("Tilemap_Background");
        bgObj.transform.parent = gridObj.transform;
        Tilemap bgMap = bgObj.AddComponent<Tilemap>();
        var bgR = bgObj.AddComponent<TilemapRenderer>();
        bgR.sortingOrder = 0;

        Tile bgTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetFloor_1_21.asset");
        for (int x = -14; x <= 14; x++)
        {
            for (int y = 0; y <= 6; y++)
            {
                bgMap.SetTile(new Vector3Int(x, y, 0), bgTile);
            }
        }

        // Ground (surface 1_7, underground 1_8)
        GameObject groundObj = new GameObject("Tilemap_Ground");
        groundObj.transform.parent = gridObj.transform;
        Tilemap groundMap = groundObj.AddComponent<Tilemap>();
        var groundR = groundObj.AddComponent<TilemapRenderer>();
        groundR.sortingOrder = 1;
        groundObj.AddComponent<TilemapCollider2D>();

        Tile surfaceTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetFloor_1_7.asset");
        Tile undergroundTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetFloor_1_8.asset");

        for (int x = -14; x <= 14; x++)
        {
            groundMap.SetTile(new Vector3Int(x, -1, 0), surfaceTile);
            for (int y = -2; y >= -6; y--)
            {
                groundMap.SetTile(new Vector3Int(x, y, 0), undergroundTile);
            }
        }

        EditorUtility.SetDirty(bgMap);
        EditorUtility.SetDirty(bgObj);
        EditorUtility.SetDirty(groundMap);
        EditorUtility.SetDirty(groundObj);

        // 3. Goober (Enemy) with 5 required components
        Sprite gooberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/SPA_Enemy_Right.png");
        GameObject goober = new GameObject("Goober (Enemy)");
        goober.transform.position = new Vector3(0, -0.3f, 0);

        var gooberSr = goober.AddComponent<SpriteRenderer>();
        gooberSr.sprite = gooberSprite;
        gooberSr.sortingOrder = 2;

        var rb2d = goober.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        var col2d = goober.AddComponent<CapsuleCollider2D>();
        col2d.offset = new Vector2(0f, -0.05f);
        col2d.size = new Vector2(0.68f, 0.68f);
        col2d.direction = CapsuleDirection2D.Vertical;

        var anim = goober.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Goober.controller");

        goober.AddComponent<EnemyMovement>();

        PrefabUtility.SaveAsPrefabAsset(goober, "Assets/Prefabs/Goober (Enemy).prefab");

        // 4. Cinemachine Follow Camera
        GameObject vcamObj = new GameObject("Follow Camera");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = goober.transform;
        vcam.m_Lens.OrthographicSize = 4f;

        var framing = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framing.m_CameraDistance = 10f;
        framing.m_XDamping = 1f;
        framing.m_YDamping = 1f;
        framing.m_ZDamping = 1f;
        framing.m_ScreenX = 0.5f;
        framing.m_ScreenY = 0.5f;
        framing.m_DeadZoneWidth = 0.1f;
        framing.m_DeadZoneHeight = 0.1f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    [MenuItem("Week2/Rebuild Scene2 (Slide 1.4 Standard)")]
    public static void BuildScene2()
    {
        string scenePath = "Assets/Scenes/Scene2.unity";
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Instantiate Cameras
        GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/1.2_Basic-Game-Object/Cameras.prefab");
        if (cameraPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(cameraPrefab);
        }
        else
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 0, -10);
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.75f, 0.95f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CinemachineBrain>();
        }

        // 2. Instantiate Tilemap Grid
        GameObject gridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tilemap Grid.prefab")
                             ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/1.2_Basic-Game-Object/Tilemap Grid.prefab");
        if (gridPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(gridPrefab);
        }

        // 3. Instantiate Ginger (Player)
        GameObject gingerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/1.2_Basic-Game-Object/Ginger (Player).prefab");
        GameObject gingerInstance = null;
        if (gingerPrefab != null)
        {
            gingerInstance = PrefabUtility.InstantiatePrefab(gingerPrefab) as GameObject;
            gingerInstance.transform.position = new Vector3(-4.56f, -2.19f, 0);
        }

        // 4. Instantiate Exit
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Exit.prefab");
        if (exitPrefab != null)
        {
            GameObject exitInstance = PrefabUtility.InstantiatePrefab(exitPrefab) as GameObject;
            exitInstance.transform.position = new Vector3(-2.27f, 4.89f, 0);
        }

        // 5. Connect Cinemachine Virtual Camera to Ginger
        if (gingerInstance != null)
        {
            CinemachineVirtualCamera vcam = Object.FindAnyObjectByType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = gingerInstance.transform;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("Scene2 rebuilt successfully based on Slide 1.4 standard.");
    }

    [MenuItem("Week2/Replace Player with Goober in Scene2")]
    public static void ReplacePlayerWithGoober()
    {
        string scenePath = "Assets/Scenes/Scene2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Destroy old Ginger (Player) if exists
        var oldPlayer = GameObject.Find("Ginger (Player)");
        Vector3 spawnPos = new Vector3(-16.3f, 6.5f, 0f);
        if (oldPlayer != null)
        {
            spawnPos = oldPlayer.transform.position;
            Object.DestroyImmediate(oldPlayer);
        }

        var existingGooberPlayer = GameObject.Find("Goober (Player)");
        if (existingGooberPlayer != null)
        {
            Object.DestroyImmediate(existingGooberPlayer);
        }

        // 2. Create Goober (Player)
        GameObject goober = new GameObject("Goober (Player)");
        goober.tag = "Player";
        goober.layer = LayerMask.NameToLayer("Player");
        if (goober.layer == -1) goober.layer = 8;
        goober.transform.position = spawnPos;

        // SpriteRenderer
        var sr = goober.AddComponent<SpriteRenderer>();
        var gooberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SPA_Enemy_Right.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/SPA_Enemy_Right.png");
        sr.sprite = gooberSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10; // Always visible in front of all tilemaps!

        // Rigidbody2D
        var rb = goober.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Physics material
        var slippery = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Slippery.physicsMaterial2D");

        // Body collider (CapsuleCollider2D)
        var bodyCol = goober.AddComponent<CapsuleCollider2D>();
        bodyCol.direction = CapsuleDirection2D.Vertical;
        bodyCol.offset = new Vector2(0f, -0.05f);
        bodyCol.size = new Vector2(0.68f, 0.68f);
        if (slippery != null) bodyCol.sharedMaterial = slippery;

        // Feet collider (BoxCollider2D) for ground detection
        var feetCol = goober.AddComponent<BoxCollider2D>();
        feetCol.offset = new Vector2(0f, -0.35f);
        feetCol.size = new Vector2(0.5f, 0.15f);

        // Animator
        var anim = goober.AddComponent<Animator>();
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Goober.controller")
                          ?? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Slide 1.3/Animations/Goober.controller");
        if (animController != null)
        {
            anim.runtimeAnimatorController = animController;
        }

        // PlayerController script
        goober.AddComponent<PlayerController>();

        // 3. Update Cinemachine Virtual Camera(s) to follow Goober (Player)
        var vcams = Object.FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include);
        foreach (var vcam in vcams)
        {
            vcam.Follow = goober.transform;
        }

        var stateDrivenCams = Object.FindObjectsByType<CinemachineStateDrivenCamera>(FindObjectsInactive.Include);
        foreach (var sdc in stateDrivenCams)
        {
            sdc.Follow = goober.transform;
            sdc.m_AnimatedTarget = anim;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully replaced player with Goober (Player) in Scene2!");
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Scene1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene2.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
    }

    private static void RemoveLegacySampleScene()
    {
        string legacyPath = "Assets/Scenes/SampleScene.unity";
        if (File.Exists(legacyPath))
        {
            AssetDatabase.DeleteAsset(legacyPath);
        }
    }
}
