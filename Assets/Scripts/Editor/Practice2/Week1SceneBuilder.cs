using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Practice.Airplane;

namespace Practice.Editor
{
    /// <summary>
    /// Builder for the Week 1 Slide 1.2 scene:
    /// - Practice 1: Primitive Airplane (Body, Wing, TailPlane, Fin).
    /// - Practice 2: Nested parenting hierarchy under AirPlane with AirplaneTransformDemo tester.
    /// </summary>
    public static class Week1SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Week1_AirplaneTransform.unity";

        [MenuItem("Practice/Week 1/Build Week 1 Airplane Scene (Slide 1.2)", false, 20)]
        public static void BuildMenuCommand()
        {
            if (BuildWeek1Scene(out string error))
            {
                Debug.Log("[Week 1] Built Week 1 Airplane Transform scene successfully!");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogError($"[Week 1] Failed to build scene: {error}");
            }
        }

        public static bool BuildWeek1Scene(out string blocker)
        {
            blocker = null;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 3f, -10f);
            camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            // 2. Directional Light
            var lightGo = new GameObject("Directional Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. Ground Plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -3f, 0f);
            ground.transform.localScale = new Vector3(50f, 0.2f, 50f);

            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Ground/Materials/M_Ground.mat");
            if (groundMat != null)
            {
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            // 4. Construct Airplane strictly matching Slide 1.2
            var airplane = Week1AirplaneFactory.Build(Vector3.zero);
            airplane.AddComponent<AirplaneTransformDemo>();

            // 5. Save as Prefab
            Week1AirplaneFactory.SaveAsPrefab(airplane);

            // 6. Save Scene
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                blocker = "Failed to save scene at " + ScenePath;
                return false;
            }

            RegisterInBuildSettings();
            return true;
        }

        public static void RegisterInBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes;
            foreach (var s in currentScenes)
            {
                if (s.path == ScenePath)
                {
                    return; // Already registered
                }
            }

            var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            currentScenes.CopyTo(newScenes, 0);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
