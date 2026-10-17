using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Cinemachine;

public static class Week2Setup
{
    [MenuItem("Week2/Setup Scene and Assets")]
    public static void RunSetup()
    {
        // 1. Create Goober_Idle.anim
        string animPath = "Assets/Animations/Goober_Idle.anim";
        AnimationClip clip = new AnimationClip();
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
        Debug.Log("Created AnimationClip: " + animPath);

        // 2. Create Goober.controller
        string controllerPath = "Assets/Animations/Goober.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            var state = rootStateMachine.AddState("Goober_Idle");
            state.motion = clip;
            Debug.Log("Created AnimatorController: " + controllerPath);
        }

        // 3. Load Sprites
        Sprite gooberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/SPA_Enemy_Right.png");
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Ground_Tile.png");
        if (gooberSprite == null || groundSprite == null)
        {
            Debug.LogError("Sprites not found! Make sure sprite texture importers are configured.");
            return;
        }

        // 4. Setup Scene
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        // Clean existing root objects except Camera and Directional Light
        var rootObjects = scene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj.name != "Main Camera" && obj.name != "Directional Light")
            {
                Object.DestroyImmediate(obj);
            }
        }

        // Configure Main Camera
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
        mainCam.orthographicSize = 3.5f;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.12f, 0.11f, 0.14f);

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
            Debug.Log("Added CinemachineBrain to Main Camera");
        }

        // 5. Create Ground Platform
        GameObject groundParent = new GameObject("Ground_Platform");
        for (int x = -10; x <= 10; x++)
        {
            GameObject tile = new GameObject("Tile_" + x);
            tile.transform.parent = groundParent.transform;
            tile.transform.position = new Vector3(x, -0.85f, 0);
            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = groundSprite;
            sr.sortingOrder = 0;
        }

        // 6. Create Goober (Enemy)
        GameObject goober = new GameObject("Goober (Enemy)");
        goober.transform.position = Vector3.zero;

        // SpriteRenderer
        var gooberSr = goober.AddComponent<SpriteRenderer>();
        gooberSr.sprite = gooberSprite;
        gooberSr.sortingOrder = 1;

        // Rigidbody2D (Kinematic)
        var rb2d = goober.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        // CapsuleCollider2D
        var col2d = goober.AddComponent<CapsuleCollider2D>();
        col2d.offset = new Vector2(0f, -0.05f);
        col2d.size = new Vector2(0.68f, 0.68f);
        col2d.direction = CapsuleDirection2D.Vertical;

        // Animator
        var anim = goober.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // EnemyMovement
        goober.AddComponent<EnemyMovement>();

        // 7. Create Follow Camera (CinemachineVirtualCamera)
        GameObject vcamObj = new GameObject("Follow Camera");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = goober.transform;
        vcam.m_Lens.OrthographicSize = 3.5f;

        // Body: Framing Transposer
        var framingTransposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_CameraDistance = 10f;
        framingTransposer.m_XDamping = 1f;
        framingTransposer.m_YDamping = 1f;
        framingTransposer.m_ZDamping = 1f;
        framingTransposer.m_TrackedObjectOffset = Vector3.zero;

        Debug.Log("Created Follow Camera (CinemachineVirtualCamera) with Framing Transposer");

        // Save Goober as Prefab
        string prefabPath = "Assets/Prefabs/Goober (Enemy).prefab";
        PrefabUtility.SaveAsPrefabAsset(goober, prefabPath);
        Debug.Log("Saved prefab: " + prefabPath);

        // Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved Scene: " + scene.path);
    }
}
