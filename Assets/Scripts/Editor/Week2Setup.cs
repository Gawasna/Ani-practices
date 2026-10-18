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
            importer.spritesheet = metas.ToArray();
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

    private static void BuildScene2()
    {
        string scenePath = "Assets/Scenes/Scene2.unity";
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. 2D Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, 0, -10);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.17f);
        camObj.AddComponent<AudioListener>();

        // 2. Grid & Tilemaps
        GameObject gridObj = new GameObject("Grid");
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Ground Layer
        GameObject groundObj = new GameObject("Tilemap_Ground");
        groundObj.transform.parent = gridObj.transform;
        Tilemap groundMap = groundObj.AddComponent<Tilemap>();
        var groundRenderer = groundObj.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;

        // Decorations Layer
        GameObject decoObj = new GameObject("Tilemap_Decorations");
        decoObj.transform.parent = gridObj.transform;
        Tilemap decoMap = decoObj.AddComponent<Tilemap>();
        var decoRenderer = decoObj.AddComponent<TilemapRenderer>();
        decoRenderer.sortingOrder = 1;

        // Obstacles Layer with Collider
        GameObject obstObj = new GameObject("Tilemap_Obstacles");
        obstObj.transform.parent = gridObj.transform;
        Tilemap obstMap = obstObj.AddComponent<Tilemap>();
        var obstRenderer = obstObj.AddComponent<TilemapRenderer>();
        obstRenderer.sortingOrder = 2;
        obstObj.AddComponent<TilemapCollider2D>();

        // 3. Load Persistent Tile Assets Directly
        Tile grassTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetField_1_7.asset")
                      ?? AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetField_1_4.asset");
        Tile pathTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetField_1_1.asset");
        Tile decoBush = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetNature_0_10.asset");
        Tile decoFlower = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetNature_6_10.asset");
        Tile obstRock = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetNature_0_8.asset")
                     ?? AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/TilesetNature_4_8.asset");

        // Paint Ground (-10 to 10 X, -6 to 6 Y)
        for (int x = -10; x <= 10; x++)
        {
            for (int y = -6; y <= 6; y++)
            {
                if (y == -1 || y == 0)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), pathTile ?? grassTile);
                }
                else
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), grassTile);
                }
            }
        }

        // Paint Decorations on upper and lower grass areas
        for (int x = -8; x <= 8; x += 2)
        {
            if (x % 3 == 0 && decoFlower != null)
            {
                decoMap.SetTile(new Vector3Int(x, 2, 0), decoFlower);
                decoMap.SetTile(new Vector3Int(x + 1, -3, 0), decoFlower);
            }
            if (x % 4 == 0 && decoBush != null)
            {
                decoMap.SetTile(new Vector3Int(x, 3, 0), decoBush);
                decoMap.SetTile(new Vector3Int(x, -4, 0), decoBush);
            }
        }

        // Paint Obstacles at borders
        for (int x = -9; x <= 9; x += 3)
        {
            if (obstRock != null)
            {
                obstMap.SetTile(new Vector3Int(x, 5, 0), obstRock);
                obstMap.SetTile(new Vector3Int(x, -5, 0), obstRock);
            }
        }
        for (int y = -3; y <= 3; y += 2)
        {
            if (obstRock != null && y != -1 && y != 0)
            {
                obstMap.SetTile(new Vector3Int(-9, y, 0), obstRock);
                obstMap.SetTile(new Vector3Int(9, y, 0), obstRock);
            }
        }

        EditorUtility.SetDirty(groundMap);
        EditorUtility.SetDirty(groundObj);
        EditorUtility.SetDirty(decoMap);
        EditorUtility.SetDirty(decoObj);
        EditorUtility.SetDirty(obstMap);
        EditorUtility.SetDirty(obstObj);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
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
