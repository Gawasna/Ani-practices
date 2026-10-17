using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Practice.Airplane;
using Practice.UI;

namespace Practice.Editor
{
    /// <summary>
    /// Builds the two review scenes for the Basic Animation Setup exercise:
    /// Practice 1 (Mixamo X Bot, swing dancing state machine) and Practice 2 (primitive airplane wing,
    /// ThrottleUpAnimation). Both scenes carry a <see cref="PracticeDemoHub"/> so a reviewer can switch
    /// between them with one button while the game is running.
    /// </summary>
    /// <remarks>
    /// The scenes are created additively and closed again whenever they are not open, so building them never
    /// closes, dirties or overwrites whatever scene the user currently has open. When the scene being built is
    /// already open its content is regenerated in place instead, because Unity refuses to save another scene
    /// over the file of a loaded one ("Overwriting the same path as another open scene is not allowed"); that
    /// path is taken only while the open scene has no unsaved changes.
    ///
    /// Unity refuses to create or load a scene additively while any untitled scene is open
    /// ("Only one untitled scene is supported at a time"), so that state is detected up front, a clean
    /// and empty untitled scene is parked automatically, and anything else is reported as a blocker
    /// instead of throwing.
    /// </remarks>
    public static class PracticeDemoSceneBuilder
    {
        /// <summary>Scene paths live in <see cref="DemoAssetPaths"/> so the layout is declared once.</summary>
        public const string Practice1ScenePath = DemoAssetPaths.Practice1Scene;
        public const string Practice2ScenePath = DemoAssetPaths.Practice2Scene;

        /// <summary>Unity's default scene. Preferred host scene and the "nothing worth keeping" scene.</summary>
        public const string DefaultScenePath = DemoAssetPaths.DefaultScene;

        private const string GroundMaterialPath = DemoAssetPaths.GroundMaterial;

        private const string MissingModelStatus =
            "Chua co X Bot: tai 'Swing Dancing.fbx' tu Mixamo (Skin: With Skin) roi bo vao .resources/ " +
            "va chay Practice > Import Mixamo X Bot (Practice 1).";

        /// <summary>Objects a brand new Unity scene contains. Anything else means the scene is real work.</summary>
        private static readonly string[] DefaultObjectNames = { "Main Camera", "Directional Light", "Global Volume" };

        private delegate bool BuildAttempt(out string blocker);

        // ---------------------------------------------------------------- menu commands

        [MenuItem("Practice/Build Demo Scenes (2 Scenes + Button)", false, 1)]
        public static void BuildAllDemoScenes()
        {
            RunMenuBuild(delegate (out string blocker)
            {
                return TryBuildAllDemoScenes(openPractice1AfterBuild: true, out blocker);
            });
        }

        [MenuItem("Practice/Build Practice 1 Scene (X Bot)", false, 2)]
        public static void BuildPractice1Scene()
        {
            RunMenuBuild(delegate (out string blocker)
            {
                if (!TryBuildScene(Practice1ScenePath, BuildPractice1SceneInternal, out blocker))
                {
                    return false;
                }

                RegisterScenesInBuildSettings();
                EditorSceneManager.OpenScene(Practice1ScenePath, OpenSceneMode.Single);
                return true;
            });
        }

        [MenuItem("Practice/Build Practice 2 Scene (Airplane Wing)", false, 3)]
        public static void BuildPractice2Scene()
        {
            RunMenuBuild(delegate (out string blocker)
            {
                if (!TryBuildScene(Practice2ScenePath, BuildPractice2SceneInternal, out blocker))
                {
                    return false;
                }

                RegisterScenesInBuildSettings();
                EditorSceneManager.OpenScene(Practice2ScenePath, OpenSceneMode.Single);
                return true;
            });
        }

        [MenuItem("Practice/Open Practice 1 Scene (X Bot)", false, 20)]
        public static void OpenPractice1Scene()
        {
            OpenDemoScene(Practice1ScenePath);
        }

        [MenuItem("Practice/Open Practice 2 Scene (Airplane Wing)", false, 21)]
        public static void OpenPractice2Scene()
        {
            OpenDemoScene(Practice2ScenePath);
        }

        // ---------------------------------------------------------------- api for the auto build hook

        /// <summary>True when both demo scenes exist on disk.</summary>
        public static bool DemoScenesExist()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(Practice1ScenePath) != null
                   && AssetDatabase.LoadAssetAtPath<SceneAsset>(Practice2ScenePath) != null;
        }

        /// <summary>True when both demo scenes exist on disk and both contain Camera and Light.</summary>
        public static bool DemoScenesValid()
        {
            if (!DemoScenesExist())
            {
                return false;
            }

            try
            {
                if (System.IO.File.Exists(Practice1ScenePath))
                {
                    var text1 = System.IO.File.ReadAllText(Practice1ScenePath);
                    if (!text1.Contains("Main Camera") || !text1.Contains("Directional Light"))
                    {
                        return false;
                    }
                }

                if (System.IO.File.Exists(Practice2ScenePath))
                {
                    var text2 = System.IO.File.ReadAllText(Practice2ScenePath);
                    if (!text2.Contains("Main Camera") || !text2.Contains("Directional Light") || !text2.Contains("Tail_T"))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Builds both scenes without touching the current scene setup and without showing dialogs.
        /// Returns false and reports why when the editor is currently not in a state that allows it.
        /// </summary>
        public static bool TryBuildAllDemoScenesSilently(bool openPractice1IfIdle, out string blocker)
        {
            return TryBuildAllDemoScenes(openPractice1IfIdle, out blocker);
        }

        /// <summary>
        /// Builds what does not need a scene: the Practice 2 clip, its controller and the airplane
        /// materials. Used while the scene build is blocked so the work is not wasted.
        /// </summary>
        public static void BuildSceneIndependentAssets()
        {
            var clip = AirplaneAnimationFactory.CreateOrUpdateThrottleUpClip();
            AirplaneAnimationFactory.CreateOrUpdateController(clip);
            PrimitiveAirplaneFactory.EnsureMaterials();
        }

        // ---------------------------------------------------------------- build flow

        private static bool TryBuildAllDemoScenes(bool openPractice1AfterBuild, out string blocker)
        {
            if (!TryBuildScene(Practice2ScenePath, BuildPractice2SceneInternal, out blocker)
                || !TryBuildScene(Practice1ScenePath, BuildPractice1SceneInternal, out blocker))
            {
                return false;
            }

            RegisterScenesInBuildSettings();

            // Practice 1 is often the scene that was just rebuilt in place, in which case it is already open
            // and opening it again would only reload it.
            if (openPractice1AfterBuild && IsSetupClean() && !FindOpenScene(Practice1ScenePath).IsValid())
            {
                EditorSceneManager.OpenScene(Practice1ScenePath, OpenSceneMode.Single);
            }

            return true;
        }

        /// <summary>
        /// Builds <paramref name="scenePath"/>. A scene that is already open is rebuilt in place, because
        /// Unity refuses to save another scene over the file of a loaded one ("Overwriting the same path as
        /// another open scene is not allowed"); otherwise a brand new scene is created additively, saved and
        /// closed again so the user's open scene is never closed, never dirtied and never saved over.
        /// </summary>
        private static bool TryBuildScene(string scenePath, Action<Scene> buildContent, out string blocker)
        {
            var openScene = FindOpenScene(scenePath);
            if (openScene.IsValid())
            {
                return RebuildOpenScene(openScene, scenePath, buildContent, out blocker);
            }

            if (!TryEnsureBuildableSetup(out blocker))
            {
                return false;
            }

            var previousActiveScene = SceneManager.GetActiveScene();

            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);

                buildContent(scene);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    blocker = $"Unity khong ghi duoc file scene: {scenePath}";
                    return false;
                }

                Debug.Log($"[Practice] Scene saved at {scenePath}");
                blocker = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                blocker = exception.Message;
                Debug.LogError($"[Practice] Could not build scene at {scenePath}: {exception.Message}");
                return false;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                // Side-car assets created while building (materials, clips, controllers, prefabs).
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Rebuilds a scene that is currently open: its content is dropped and regenerated, then the file is
        /// saved. This is only allowed while the scene holds no unsaved changes - the content is thrown away,
        /// so user work must never be at risk. A dirty scene is reported as a blocker until it is saved or
        /// closed, and the retry loop of the auto build hook picks it up afterwards.
        /// </summary>
        private static bool RebuildOpenScene(Scene scene, string scenePath, Action<Scene> buildContent,
            out string blocker)
        {
            if (scene.isDirty)
            {
                blocker = $"Scene \"{scenePath}\" dang mo va co thay doi chua luu. " +
                          "Luu lai bang Ctrl+S (hoac dong scene do) roi thu lai.";
                return false;
            }

            var previousActiveScene = SceneManager.GetActiveScene();
            try
            {
                SceneManager.SetActiveScene(scene);

                foreach (var root in scene.GetRootGameObjects())
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                buildContent(scene);

                EditorSceneManager.MarkSceneDirty(scene);

                // Saving a scene to its own path is always allowed, unlike saving over another open scene.
                if (!EditorSceneManager.SaveScene(scene))
                {
                    blocker = $"Unity khong ghi duoc file scene: {scenePath}";
                    return false;
                }

                Debug.Log($"[Practice] Scene rebuilt in place and saved at {scenePath}");
                blocker = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                blocker = exception.Message;
                Debug.LogError($"[Practice] Could not rebuild the open scene {scenePath}: {exception.Message}");
                return false;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// The loaded scene that already lives at <paramref name="scenePath"/>, or an invalid scene when that
        /// file is not open at the moment.
        /// </summary>
        private static Scene FindOpenScene(string scenePath)
        {
            foreach (var scene in LoadedScenes())
            {
                if (scene.path == scenePath)
                {
                    return scene;
                }
            }

            return default;
        }

        private static void BuildPractice1SceneInternal(Scene scene)
        {
            CreateFloor(7f);
            FrameCamera(scene, new Vector3(0f, 1.45f, 3.4f), new Vector3(0f, 0.95f, 0f));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XBotMixamoPipeline.PrefabPath);
            string status = string.Empty;

            if (prefab == null)
            {
                status = MissingModelStatus;
                Debug.LogWarning("[Practice 1] X Bot prefab not found. The scene was built with a status message instead. " +
                                 "Put the Mixamo FBX in .resources and run Practice > Import Mixamo X Bot (Practice 1).");
            }
            else
            {
                var character = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                character.name = "XBot";
                character.transform.position = Vector3.zero;
                character.transform.rotation = Quaternion.identity;
            }

            CreateDemoHub(status);
        }

        private static void BuildPractice2SceneInternal(Scene scene)
        {
            CreateFloor(9f);
            FrameCamera(scene, new Vector3(6.2f, 3.6f, 7.0f), new Vector3(0f, 1.05f, 0f));

            var airplane = PrimitiveAirplaneFactory.Build(Vector3.zero);

            var clip = AirplaneAnimationFactory.CreateOrUpdateThrottleUpClip();
            var controller = AirplaneAnimationFactory.CreateOrUpdateController(clip);

            var animator = airplane.GetComponent<Animator>();
            if (animator == null)
            {
                animator = airplane.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (airplane.GetComponent<AirplaneTakeoffDemo>() == null)
            {
                airplane.AddComponent<AirplaneTakeoffDemo>();
            }

            CreateDemoHub(string.Empty);
        }

        // ---------------------------------------------------------------- editor state guards

        /// <summary>
        /// Makes sure the editor is in a state where a scene can be created additively. Unity refuses
        /// that while an untitled scene is open ("Only one untitled scene is supported at a time").
        /// A clean and empty untitled scene holds nothing worth keeping, so it is parked automatically by
        /// opening the host scene in Single mode; any other case is reported as a blocker.
        /// </summary>
        private static bool TryEnsureBuildableSetup(out string blocker)
        {
            blocker = string.Empty;

            var loaded = LoadedScenes();
            Scene untitled = default;
            bool hasUntitled = false;
            foreach (var scene in loaded)
            {
                if (!string.IsNullOrEmpty(scene.path))
                {
                    continue;
                }

                hasUntitled = true;
                untitled = scene;
                break;
            }

            if (!hasUntitled)
            {
                return true;
            }

            // Unity tolerates dirty scenes that live in a file, but parking the setup below replaces it.
            foreach (var scene in loaded)
            {
                if (scene.isDirty && !string.IsNullOrEmpty(scene.path))
                {
                    blocker = $"Scene \"{scene.path}\" dang co thay doi chua luu. Luu lai bang Ctrl+S roi thu lai.";
                    return false;
                }
            }

            if (untitled.isDirty || !ContainsOnlyDefaultObjects(untitled))
            {
                blocker = $"Scene \"{untitled.name}\" (Untitled) dang mo va chua duoc luu thanh file. " +
                          "Unity khong cho tao scene moi khi con scene Untitled chua luu. " +
                          "Chon \"Dong scene chua luu & dung tiep\" trong hop thoai, hoac tu xu ly: " +
                          "File > New Scene (Ctrl+N) roi chon \"Don't Save\", hoac Ctrl+S de luu no thanh file .unity.";
                return false;
            }

            string hostScenePath = FindHostScenePath();
            if (string.IsNullOrEmpty(hostScenePath))
            {
                blocker = "Dang co scene Untitled chua luu va khong tim thay scene .unity nao de lam scene nen. " +
                          "Hay luu hoac bo scene Untitled truoc (File > New Scene roi chon \"Don't Save\").";
                return false;
            }

            // Nothing can be lost here, so move the setup to a real scene and carry on.
            EditorSceneManager.OpenScene(hostScenePath, OpenSceneMode.Single);
            Debug.Log($"[Practice] Scene Untitled (rong, khong co thay doi) da duoc thay bang {hostScenePath} " +
                      "de co the dung 2 scene demo.");
            return true;
        }

        private static bool ContainsOnlyDefaultObjects(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (Array.IndexOf(DefaultObjectNames, root.name) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True when no loaded scene is dirty and no untitled scene is open.</summary>
        private static bool IsSetupClean()
        {
            foreach (var scene in LoadedScenes())
            {
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Scene> LoadedScenes()
        {
            var scenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    scenes.Add(scene);
                }
            }

            return scenes;
        }

        private static List<Scene> LoadedUntitledScenes()
        {
            var scenes = new List<Scene>();
            foreach (var scene in LoadedScenes())
            {
                if (string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(scene);
                }
            }

            return scenes;
        }

        /// <summary>An existing scene asset to use as host, never one of the demo scenes we build.</summary>
        private static string FindHostScenePath()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultScenePath) != null)
            {
                return DefaultScenePath;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == Practice1ScenePath || path == Practice2ScenePath)
                {
                    continue;
                }

                return path;
            }

            return string.Empty;
        }

        // ---------------------------------------------------------------- menu helpers

        private static void RunMenuBuild(BuildAttempt attempt)
        {
            if (attempt(out string blocker))
            {
                Debug.Log("[Practice] Demo scenes built. Press Play, then use the on-screen button (or F1 / F2) to switch scene.");
                return;
            }

            if (OfferViewToDiscardUntitledScenes() && attempt(out blocker))
            {
                Debug.Log("[Practice] Demo scenes built. Press Play, then use the on-screen button (or F1 / F2) to switch scene.");
                return;
            }

            ReportBlocker(blocker, showDialog: true);
        }

        /// <summary>
        /// Asks before throwing away an unsaved Untitled scene, because Unity cannot create any scene
        /// while one is open. Nothing is discarded without an explicit confirmation.
        /// </summary>
        private static bool OfferViewToDiscardUntitledScenes()
        {
            var untitledScenes = LoadedUntitledScenes();
            if (untitledScenes.Count == 0)
            {
                return false;
            }

            var names = new List<string>();
            foreach (var scene in untitledScenes)
            {
                names.Add(scene.name);
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Practice - scene Untitled chua luu",
                $"Dong scene chua luu ({string.Join(", ", names)}) ma KHONG luu de dung 2 scene demo?\n\n" +
                "Moi thay doi chua luu trong scene do se bi mat.\n\n" +
                "Neu khong chac, chon Huy roi tu xu ly: Ctrl+S de luu scene thanh file .unity, hoac " +
                "File > New Scene roi chon \"Don't Save\".",
                "Dong & dung tiep", "Huy");

            if (!confirmed)
            {
                return false;
            }

            foreach (var scene in untitledScenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return true;
        }

        private static void ReportBlocker(string blocker, bool showDialog)
        {
            // Keep the data that does not need a scene, so the blocked state still produces something.
            BuildSceneIndependentAssets();

            Debug.LogWarning($"[Practice] Chua dung duoc scene: {blocker} " +
                             "Clip ThrottleUpAnimation, controller AC_AirplaneWing va material may bay da duoc tao truoc.");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Practice - chua dung duoc scene", blocker, "OK");
            }
        }

        // ---------------------------------------------------------------- scene content

        private static void CreateFloor(float size)
        {
            var groundMaterial = UrpMaterialUtility.UpsertMaterial(GroundMaterialPath,
                new Color(0.28f, 0.30f, 0.33f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(size, 1f, size);

            var renderer = floor.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = groundMaterial;
            }
        }

        /// <summary>
        /// Guarantees that the scene contains a Camera (tagged MainCamera with AudioListener)
        /// and a Directional Light. When a scene is rebuilt in place (RebuildOpenScene), all roots
        /// are cleared first, so camera and light must be recreated if absent.
        /// </summary>
        public static Camera EnsureCameraAndLight(Scene scene)
        {
            Camera camera = null;
            Light light = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (camera == null)
                {
                    camera = root.GetComponentInChildren<Camera>(true);
                }

                if (light == null)
                {
                    var l = root.GetComponentInChildren<Light>(true);
                    if (l != null && l.type == LightType.Directional)
                    {
                        light = l;
                    }
                }
            }

            if (camera == null)
            {
                var cameraHost = new GameObject("Main Camera");
                cameraHost.tag = "MainCamera";
                camera = cameraHost.AddComponent<Camera>();
                cameraHost.AddComponent<AudioListener>();
                SceneManager.MoveGameObjectToScene(cameraHost, scene);
            }

            if (light == null)
            {
                var lightHost = new GameObject("Directional Light");
                light = lightHost.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.95686275f, 0.8392157f, 1f);
                light.intensity = 1f;
                light.shadows = LightShadows.Soft;
                lightHost.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                SceneManager.MoveGameObjectToScene(lightHost, scene);
            }

            return camera;
        }

        /// <summary>
        /// Frames the camera of the scene being built. Ensures the camera and light exist in the scene.
        /// </summary>
        private static void FrameCamera(Scene scene, Vector3 position, Vector3 lookAt)
        {
            var camera = EnsureCameraAndLight(scene);
            if (camera != null)
            {
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation(lookAt - position);
            }
        }

        private static void CreateDemoHub(string statusMessage)
        {
            var host = new GameObject("PracticeDemoHub");
            var hub = host.AddComponent<PracticeDemoHub>();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                var serialized = new SerializedObject(hub);
                var statusProperty = serialized.FindProperty("statusMessage");
                if (statusProperty != null)
                {
                    statusProperty.stringValue = statusMessage;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void OpenDemoScene(string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                EditorUtility.DisplayDialog("Practice",
                    $"Scene chua ton tai:\n{scenePath}\n\nChay Practice > Build Demo Scenes (2 Scenes + Button) truoc.",
                    "OK");
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void RegisterScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(Practice1ScenePath, true),
                new EditorBuildSettingsScene(Practice2ScenePath, true)
            };

            foreach (var existing in EditorBuildSettings.scenes)
            {
                bool alreadyAdded = scenes.Exists(scene => scene.path == existing.path);
                if (!alreadyAdded && !string.IsNullOrEmpty(existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Practice] Build Settings updated: Practice 1 and Practice 2 are enabled, existing scenes preserved.");
        }
    }
}
