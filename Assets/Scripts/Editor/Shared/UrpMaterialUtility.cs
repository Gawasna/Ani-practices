using UnityEditor;
using UnityEngine;

namespace Practice.Editor
{
    /// <summary>
    /// Material helpers shared by the practice pipelines.
    /// The project runs on URP, so any material imported from an external source
    /// (Mixamo FBX, legacy Standard shader) must be replaced to avoid the magenta error shader.
    /// </summary>
    public static class UrpMaterialUtility
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string FallbackShaderName = "Standard";

        /// <summary>Creates every missing folder along the given "Assets/..." path.</summary>
        public static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            EnsureFolderPath(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static Shader ResolveLitShader()
        {
            return Shader.Find(UrpLitShaderName) ?? Shader.Find(FallbackShaderName);
        }

        /// <summary>Creates or updates a material asset with the given color and optional base map.</summary>
        public static Material UpsertMaterial(string assetPath, Color baseColor, Texture baseMap = null,
            float smoothness = 0.25f)
        {
            EnsureFolderPath(System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

            var shader = ResolveLitShader();
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(assetPath) };
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (baseMap != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", baseMap);
                }
                else if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", baseMap);
                }
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        public static bool IsUrpCompatible(Shader shader)
        {
            if (shader == null)
            {
                return false;
            }

            string shaderName = shader.name;
            return shaderName.StartsWith("Universal Render Pipeline")
                   || shaderName.StartsWith("Shader Graphs")
                   || shaderName.StartsWith("Hidden/Universal");
        }

        /// <summary>
        /// Walks every renderer under <paramref name="root"/> and swaps non-URP materials for generated URP Lit
        /// assets, preserving the original base texture when one exists.
        /// </summary>
        public static int ReplaceNonUrpMaterials(GameObject root, string materialFolder, string prefix)
        {
            int replaced = 0;
            EnsureFolderPath(materialFolder);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source != null && IsUrpCompatible(source.shader))
                    {
                        continue;
                    }

                    string sourceName = source != null && !string.IsNullOrEmpty(source.name) ? source.name : "Slot";
                    string assetPath = $"{materialFolder}/{prefix}_{Sanitize(sourceName)}_{i}.mat";

                    Texture baseMap = null;
                    if (source != null && source.HasProperty("_MainTex"))
                    {
                        baseMap = source.mainTexture;
                    }

                    materials[i] = UpsertMaterial(assetPath, Color.white, baseMap);
                    changed = true;
                    replaced++;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }

            return replaced;
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
