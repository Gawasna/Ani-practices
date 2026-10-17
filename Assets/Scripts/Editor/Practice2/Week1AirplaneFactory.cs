using UnityEditor;
using UnityEngine;

namespace Practice.Editor
{
    /// <summary>
    /// Constructs the 3D airplane model strictly according to Slide 1.2 (Pages 14-20):
    /// - Practice 1: 4 primitive parts with exact Transforms (Body, Wing, TailPlane, Fin).
    /// - Practice 2: Hierarchy parented under empty 'AirPlane' root for nested transformation testing.
    /// Reuses materials from Week 7 (M_Fuselage.mat, M_Wing.mat).
    /// </summary>
    public static class Week1AirplaneFactory
    {
        public const string RootName = "AirPlane";
        public const string BodyName = "AirplaneBody";
        public const string WingName = "AirplaneWing";
        public const string TailPlaneName = "TailPlane";
        public const string FinName = "AirplaneFin";
        public const string PrefabPath = "Assets/Prefabs/AirPlane.prefab";

        public static GameObject Build(Vector3 rootPosition)
        {
            var fuselageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Airplanes/Materials/M_Fuselage.mat");
            var wingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Airplanes/Materials/M_Wing.mat");

            if (fuselageMaterial == null || wingMaterial == null)
            {
                PrimitiveAirplaneFactory.EnsureMaterials();
                fuselageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Airplanes/Materials/M_Fuselage.mat");
                wingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Airplanes/Materials/M_Wing.mat");
            }

            // 1. Root Empty GameObject: AirPlane (Slide 1.2, Page 20)
            var root = new GameObject(RootName);
            root.transform.position = rootPosition;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // 2. AirplaneBody: Capsule (Slide 1.2, Page 15 & 19)
            // Pos (0, 0, 0), Rot (-90, 0, 0), Scale (1, 4.2, 1.1)
            CreateChildPrimitive(PrimitiveType.Capsule, root.transform, BodyName,
                new Vector3(0f, 0f, 0f),
                new Vector3(-90f, 0f, 0f),
                new Vector3(1f, 4.2f, 1.1f),
                fuselageMaterial);

            // 3. AirplaneWing: Cube (Slide 1.2, Page 16 & 19)
            // Pos (0, 0, -0.58), Rot (0, 0, 0), Scale (6.8, 0.18, 1.3)
            CreateChildPrimitive(PrimitiveType.Cube, root.transform, WingName,
                new Vector3(0f, 0f, -0.58f),
                Vector3.zero,
                new Vector3(6.8f, 0.18f, 1.3f),
                wingMaterial);

            // 4. TailPlane: Cube (Slide 1.2, Page 17 & 19)
            // Pos (0, 0, 3.23), Rot (0, 0, 0), Scale (1.93, 0.2, 0.87)
            CreateChildPrimitive(PrimitiveType.Cube, root.transform, TailPlaneName,
                new Vector3(0f, 0f, 3.23f),
                Vector3.zero,
                new Vector3(1.93f, 0.2f, 0.87f),
                wingMaterial);

            // 5. AirplaneFin: Cube (Slide 1.2, Page 18 & 19)
            // Pos (-0.1, 0.5, 3.6), Rot (18, 0, 0), Scale (0.2, 1.04, 0.614)
            CreateChildPrimitive(PrimitiveType.Cube, root.transform, FinName,
                new Vector3(-0.1f, 0.5f, 3.6f),
                new Vector3(18f, 0f, 0f),
                new Vector3(0.2f, 1.04f, 0.614f),
                fuselageMaterial);

            return root;
        }

        public static GameObject SaveAsPrefab(GameObject instance)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static GameObject CreateChildPrimitive(PrimitiveType type, Transform parent, string name,
            Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }

            return go;
        }
    }
}
