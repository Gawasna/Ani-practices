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
    [MenuItem("Week2/Setup Full Tilemap Scene")]
    public static void RunFullSetup()
    {
        // 1. Setup Enemy Animation & Controller
        SetupEnemyAnimation();

        // 2. Slice Tilesets
        SliceTilesets();

        // 3. Generate Tile Assets
        var tileDict = GenerateTileAssets();

        // 4. Create Palette Prefab
        CreateTilePalettePrefab(tileDict);

        // 5. Build Scene with Tilemap
        BuildTilemapScene(tileDict);

        Debug.Log("Week 2 Tilemap & Cinemachine setup completed successfully!");
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
            "Assets/Art/Tilesets/TilesetFloor.png",
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
            Debug.Log($"Sliced {path}: {cols}x{rows} = {metas.Count} tiles");
        }
    }

    private static Dictionary<string, Tile> GenerateTileAssets()
    {
        var tileDict = new Dictionary<string, Tile>();
        string[] tilesetPaths = new[] {
            "Assets/Art/Tilesets/TilesetField.png",
            "Assets/Art/Tilesets/TilesetFloor.png",
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
        Debug.Log($"Generated {tileDict.Count} Tile assets in Assets/Art/Tiles/");
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

        // Paint a preview 5x5 grid of sample tiles into the palette
        int col = 0, row = 0;
        foreach (var kvp in tileDict)
        {
            tilemap.SetTile(new Vector3Int(col, -row, 0), kvp.Value);
            col++;
            if (col >= 8)
            {
                col = 0;
                row++;
                if (row >= 8) break; // Keep palette compact
            }
        }

        PrefabUtility.SaveAsPrefabAsset(paletteObj, palettePath);
        Object.DestroyImmediate(paletteObj);
        Debug.Log($"Created Tile Palette prefab at {palettePath}");
    }

    private static void BuildTilemapScene(Dictionary<string, Tile> tileDict)
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        // Clean existing scene root objects except cameras/lights
        var rootObjects = scene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj.name != "Main Camera" && obj.name != "Directional Light")
            {
                Object.DestroyImmediate(obj);
            }
        }

        // 1. Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.transform.position = new Vector3(0, 0, -10);
        mainCam.transform.rotation = Quaternion.identity;
        mainCam.orthographic = true;
        mainCam.orthographicSize = 4f;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.13f, 0.12f, 0.16f);

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // 2. Setup Grid and Tilemaps
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

        // Obstacles Layer (with 2D Collider)
        GameObject obstObj = new GameObject("Tilemap_Obstacles");
        obstObj.transform.parent = gridObj.transform;
        Tilemap obstMap = obstObj.AddComponent<Tilemap>();
        var obstRenderer = obstObj.AddComponent<TilemapRenderer>();
        obstRenderer.sortingOrder = 2;
        var tilemapCol = obstObj.AddComponent<TilemapCollider2D>();

        // 3. Paint the Map (Width: -12 to 12, Height: -6 to 6)
        Tile grassTile = GetTile(tileDict, "TilesetField_1_1") ?? GetTile(tileDict, "TilesetFloor_1_1");
        Tile grassEdgeTop = GetTile(tileDict, "TilesetField_1_0");
        Tile grassEdgeBottom = GetTile(tileDict, "TilesetField_1_2");
        Tile pathTile = GetTile(tileDict, "TilesetFloor_2_1") ?? GetTile(tileDict, "TilesetField_2_1");
        Tile flowerTile = GetTile(tileDict, "TilesetNature_1_8");
        Tile flowerWhite = GetTile(tileDict, "TilesetNature_4_8");
        Tile bushTile = GetTile(tileDict, "TilesetNature_0_7");
        Tile rockTile = GetTile(tileDict, "TilesetNature_1_10") ?? GetTile(tileDict, "TilesetNature_0_10");
        Tile treeTop = GetTile(tileDict, "TilesetNature_1_0");
        Tile treeTrunk = GetTile(tileDict, "TilesetNature_1_1");

        // Paint Ground
        for (int x = -14; x <= 14; x++)
        {
            for (int y = -8; y <= 8; y++)
            {
                // Horizontal path running through y = -1 and y = 0
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

        // Paint Decorations (flowers, grass tufts)
        for (int x = -13; x <= 13; x += 2)
        {
            if (x % 3 == 0 && flowerTile != null)
                decoMap.SetTile(new Vector3Int(x, 2, 0), flowerTile);
            if (x % 4 == 0 && flowerWhite != null)
                decoMap.SetTile(new Vector3Int(x, -3, 0), flowerWhite);
            if (x % 5 == 0 && bushTile != null)
                decoMap.SetTile(new Vector3Int(x, 1, 0), bushTile);
        }

        // Paint Obstacles & Trees (borders at y = 5, y = -6, and side barriers)
        for (int x = -14; x <= 14; x += 2)
        {
            if (treeTop != null)
            {
                obstMap.SetTile(new Vector3Int(x, 5, 0), treeTop);
                obstMap.SetTile(new Vector3Int(x, -5, 0), treeTop);
            }
            if (treeTrunk != null)
            {
                obstMap.SetTile(new Vector3Int(x, 4, 0), treeTrunk);
                obstMap.SetTile(new Vector3Int(x, -6, 0), treeTrunk);
            }
        }

        // Side rocks
        for (int y = -4; y <= 4; y += 2)
        {
            if (rockTile != null)
            {
                obstMap.SetTile(new Vector3Int(-12, y, 0), rockTile);
                obstMap.SetTile(new Vector3Int(12, y, 0), rockTile);
            }
        }

        // 4. Create Goober (Enemy)
        Sprite gooberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/SPA_Enemy_Right.png");
        GameObject goober = new GameObject("Goober (Enemy)");
        goober.transform.position = new Vector3(0, -0.2f, 0);

        var gooberSr = goober.AddComponent<SpriteRenderer>();
        gooberSr.sprite = gooberSprite;
        gooberSr.sortingOrder = 3; // In front of ground & decorations

        var rb2d = goober.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        var col2d = goober.AddComponent<CapsuleCollider2D>();
        col2d.offset = new Vector2(0f, -0.05f);
        col2d.size = new Vector2(0.68f, 0.68f);
        col2d.direction = CapsuleDirection2D.Vertical;

        var anim = goober.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Goober.controller");

        goober.AddComponent<EnemyMovement>();

        // Save Goober Prefab
        PrefabUtility.SaveAsPrefabAsset(goober, "Assets/Prefabs/Goober (Enemy).prefab");

        // 5. Create Cinemachine Follow Camera
        GameObject vcamObj = new GameObject("Follow Camera");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = goober.transform;
        vcam.m_Lens.OrthographicSize = 4f;

        var framingTransposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_CameraDistance = 10f;
        framingTransposer.m_XDamping = 1f;
        framingTransposer.m_YDamping = 1f;
        framingTransposer.m_ZDamping = 1f;
        framingTransposer.m_TrackedObjectOffset = Vector3.zero;

        // Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Tile GetTile(Dictionary<string, Tile> dict, string name)
    {
        if (dict.TryGetValue(name, out Tile t)) return t;
        // Fallback: search key containing name
        foreach (var kvp in dict)
        {
            if (kvp.Key.Contains(name)) return kvp.Value;
        }
        return null;
    }
}
