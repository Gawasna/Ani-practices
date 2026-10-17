using UnityEngine;

namespace Practice.Editor
{
    /// <summary>
    /// Builds the Practice 2 airplane out of primitives: a Cylinder for the fuselage, Cubes for the wings,
    /// and a classic T-tail (vertical fin + horizontal stabilizer on top). The wing pivots are empty GameObjects
    /// placed at the wing root, which gives the hinge behaviour Practice 2 asks for: animating the pivot's
    /// local Rotation.x sweeps the wing.
    /// </summary>
    public static class PrimitiveAirplaneFactory
    {
        public const string RootName = "Airplane";
        public const string LeftWingName = "AirplaneWing";
        public const string RightWingName = "AirplaneWing_R";
        public const string TailVerticalName = "Tail_VerticalFin";
        public const string TailHorizontalName = "Tail_HorizontalWing";
        public const string MaterialFolder = DemoAssetPaths.AirplaneMaterialFolder;

        /// <summary>Unity's Cylinder is 2 units tall, so half of it is the length factor.</summary>
        private const float FuselageLength = 3.4f;
        private const float FuselageDiameter = 0.7f;
        private const float FuselageCenterY = 1f;

        /// <summary>Wing root sits on the fuselage surface, so the pivot is what the wing hinges around.</summary>
        private const float WingRootOffsetX = FuselageDiameter * 0.5f;
        private const float WingPivotY = FuselageCenterY;
        private const float WingPivotZ = 0.35f;
        private const float WingLength = 2.5f;
        private const float WingChord = 1.1f;
        private const float WingThickness = 0.09f;

        /// <summary>T-Tail dimensions at the rear of the fuselage.</summary>
        private const float TailZ = -1.35f;
        private const float TailFinHeight = 0.95f;
        private const float TailFinThickness = 0.08f;
        private const float TailFinChord = 0.7f;
        private const float TailWingSpan = 1.6f;
        private const float TailWingThickness = 0.07f;
        private const float TailWingChord = 0.5f;

        public static GameObject Build(Vector3 position)
        {
            CreateMaterials(out var fuselageMaterial, out var wingMaterial);

            var root = new GameObject(RootName);
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;

            // Cylinder lies down along +Z: its local Y axis is rotated onto the fuselage axis.
            CreatePrimitive(PrimitiveType.Cylinder, root.transform, "Fuselage",
                new Vector3(0f, FuselageCenterY, 0f),
                new Vector3(FuselageDiameter, FuselageLength * 0.5f, FuselageDiameter),
                new Vector3(90f, 0f, 0f), fuselageMaterial);

            CreateWing(root.transform, LeftWingName, 1f, wingMaterial);
            CreateWing(root.transform, RightWingName, -1f, wingMaterial);

            // T-tail: vertical fin + horizontal wing on top of the fin.
            CreateTTail(root.transform, fuselageMaterial, wingMaterial);

            return root;
        }

        /// <summary>
        /// Creates (or refreshes) the airplane materials without touching any scene. Kept separate from
        /// <see cref="Build"/> so the materials can still be produced while a scene build is not possible.
        /// </summary>
        public static void EnsureMaterials()
        {
            CreateMaterials(out _, out _);
        }

        private static void CreateMaterials(out Material fuselage, out Material wing)
        {
            UrpMaterialUtility.EnsureFolderPath(MaterialFolder);

            fuselage = UrpMaterialUtility.UpsertMaterial($"{MaterialFolder}/M_Fuselage.mat",
                new Color(0.85f, 0.87f, 0.90f));
            wing = UrpMaterialUtility.UpsertMaterial($"{MaterialFolder}/M_Wing.mat",
                new Color(0.60f, 0.67f, 0.78f));
        }

        /// <summary>
        /// Creates one wing: an empty pivot at the wing root plus the wing box offset outwards.
        /// Animating the pivot keeps the hinge axis at the fuselage instead of the wing centre.
        /// </summary>
        private static void CreateWing(Transform parent, string name, float side, Material wingMaterial)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(WingRootOffsetX * side, WingPivotY, WingPivotZ);
            pivot.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Cube, pivot.transform, $"{name}_Mesh",
                new Vector3(WingLength * 0.5f * side, 0f, 0f),
                new Vector3(WingLength, WingThickness, WingChord),
                Vector3.zero, wingMaterial);
        }

        /// <summary>
        /// Creates the T-tail: a vertical fin rooted into the fuselage rear, with a horizontal stabilizer
        /// mounted flush on top of the vertical fin to form the distinct letter 'T'.
        /// </summary>
        private static void CreateTTail(Transform parent, Material finMaterial, Material horizontalMaterial)
        {
            var tailRoot = new GameObject("Tail_T");
            tailRoot.transform.SetParent(parent, false);

            // Vertical fin (than dung cua chu T)
            float finCenterY = FuselageCenterY + 0.2f + TailFinHeight * 0.5f;
            CreatePrimitive(PrimitiveType.Cube, tailRoot.transform, TailVerticalName,
                new Vector3(0f, finCenterY, TailZ),
                new Vector3(TailFinThickness, TailFinHeight, TailFinChord),
                Vector3.zero, finMaterial);

            // Horizontal stabilizer mounted on top of the vertical fin (thanh ngang dinh chu T)
            float topY = finCenterY + TailFinHeight * 0.5f + TailWingThickness * 0.5f;
            CreatePrimitive(PrimitiveType.Cube, tailRoot.transform, TailHorizontalName,
                new Vector3(0f, topY, TailZ - 0.05f),
                new Vector3(TailWingSpan, TailWingThickness, TailWingChord),
                Vector3.zero, horizontalMaterial);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name,
            Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.Euler(localEuler);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }
    }
}
