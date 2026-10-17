using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Practice.Animation;

namespace Practice.Editor
{
    /// <summary>
    /// Practice 1 import pipeline. It takes exactly one Mixamo FBX from the ignored <c>.resources</c> folder,
    /// imports it into the project, configures the Humanoid rig, bakes the Idle &lt;-&gt; SwingDancing state
    /// machine and generates the X Bot prefab.
    ///
    /// The Mixamo download used here is "With Skin", so the single file already contains the X Bot model and
    /// the motion: there is no file classification, no inbox guessing and no second FBX to match up.
    /// Mixamo requires an Adobe login, so only the download itself stays a manual step.
    /// </summary>
    public static class XBotMixamoPipeline
    {
        /// <summary>Name of the Mixamo download this practice uses, inside <see cref="ResourceFolderName"/>.</summary>
        public const string SourceFbxName = "Swing Dancing.fbx";

        public const string InboxFolder = DemoAssetPaths.InboxFolder;
        public const string ResourceFolderName = DemoAssetPaths.ResourceDropFolder;
        public const string CharacterFolder = DemoAssetPaths.CharacterFolder;
        public const string CharacterAssetPath = DemoAssetPaths.XBotModel;
        public const string ControllerPath = DemoAssetPaths.XBotController;
        public const string PrefabPath = DemoAssetPaths.XBotPrefab;
        public const string MaterialFolder = DemoAssetPaths.XBotMaterialFolder;

        /// <summary>
        /// Name of the dance clip and of the Animator state. Fixed to the wording of the exercise sheet
        /// ("Swing Dancing") on purpose, so the demo always presents the expected name.
        /// </summary>
        public const string DanceClipName = "SwingDancing";

        public const string IdleStateName = "Idle";

        // ---------------------------------------------------------------- menu

        [MenuItem("Practice/Import Mixamo X Bot (Practice 1)", false, 20)]
        public static void ImportAndConfigure()
        {
            if (!TryResolveSourceFbx(out string sourcePath))
            {
                EditorUtility.DisplayDialog("Practice 1 - thieu file FBX",
                    $"Khong thay '{SourceFbxName}' trong thu muc {ResourceFolderName}.\n\n" +
                    "Tai tu mixamo.com (Format: FBX Binary, Skin: With Skin, 30 FPS, Keyframe Reduction: none) " +
                    "roi bo file .fbx vao thu muc do.", "OK");
                return;
            }

            if (!TryImport(sourcePath, out string blocker))
            {
                EditorUtility.DisplayDialog("Practice 1 - import that bai", blocker, "OK");
                return;
            }

            // The character is only visible once the scene holds it, so rebuild Practice 1 right away.
            PracticeDemoSceneBuilder.BuildPractice1Scene();
        }

        [MenuItem("Practice/Open .resources Folder", false, 21)]
        public static void RevealResourceFolder()
        {
            string absolute = Path.Combine(ProjectRoot, ResourceFolderName);
            Directory.CreateDirectory(absolute);
            EditorUtility.RevealInFinder(absolute);
        }

        // ---------------------------------------------------------------- api used by the auto build hook

        /// <summary>
        /// Finds the FBX to import: exactly <see cref="SourceFbxName"/>, or - if that name is not there - the
        /// only FBX sitting in <see cref="ResourceFolderName"/>.
        /// </summary>
        public static bool TryResolveSourceFbx(out string sourcePath)
        {
            string resourceFolder = Path.Combine(ProjectRoot, ResourceFolderName);
            sourcePath = Path.Combine(resourceFolder, SourceFbxName);

            if (File.Exists(sourcePath))
            {
                return true;
            }

            if (!Directory.Exists(resourceFolder))
            {
                return false;
            }

            var candidates = Directory.GetFiles(resourceFolder, "*.fbx", SearchOption.AllDirectories);
            if (candidates.Length != 1)
            {
                return false;
            }

            sourcePath = candidates[0];
            Debug.Log($"[Practice 1] '{SourceFbxName}' khong co, dung FBX duy nhat tim thay: {Path.GetFileName(sourcePath)}");
            return true;
        }

        /// <summary>True when the character model already exists at its canonical path.</summary>
        public static bool HasImportedCharacter()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(CharacterAssetPath) != null;
        }

        /// <summary>
        /// True when the character model exists AND its clip settings have keepOriginalPositionY enabled.
        /// </summary>
        public static bool CharacterConfiguredCorrectly()
        {
            var importer = AssetImporter.GetAtPath(CharacterAssetPath) as ModelImporter;
            if (importer == null)
            {
                return false;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            return clips[0].keepOriginalPositionY 
                && clips[0].keepOriginalOrientation 
                && clips[0].lastFrame > clips[0].firstFrame;
        }

        public static bool NeedsReimport()
        {
            if (!HasImportedCharacter())
            {
                return true;
            }

            return !CharacterConfiguredCorrectly();
        }

        /// <summary>
        /// Reconfigures the already imported FBX with the latest clip settings and updates the prefab/controller.
        /// </summary>
        public static bool ReconfigureExistingCharacter()
        {
            if (!HasImportedCharacter())
            {
                return false;
            }

            var avatar = ConfigureCharacterImporter(CharacterAssetPath, DanceClipName);
            if (avatar == null)
            {
                return false;
            }

            var danceClip = FindFirstClip(CharacterAssetPath);
            if (danceClip == null)
            {
                return false;
            }

            var controller = CreateOrUpdateController(danceClip, DanceClipName);
            CreateOrUpdatePrefab(CharacterAssetPath, avatar, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Imports <paramref name="sourcePath"/> and rebuilds everything derived from it (Avatar, clip,
        /// Animator Controller, prefab). Never touches the currently open scene; returns false with a reason
        /// instead of throwing, so it can also run from the auto build hook without dialogs.
        /// </summary>
        public static bool TryImport(string sourcePath, out string blocker)
        {
            blocker = string.Empty;

            string inboxPath = CopyToInbox(sourcePath);
            if (string.IsNullOrEmpty(inboxPath))
            {
                blocker = $"Khong copy duoc '{sourcePath}' vao {InboxFolder}.";
                return false;
            }

            string characterPath = MoveAsset(inboxPath, CharacterAssetPath);
            ClearInbox();

            if (string.IsNullOrEmpty(characterPath))
            {
                blocker = $"Khong di chuyen duoc FBX vao {CharacterAssetPath}.";
                return false;
            }

            var avatar = ConfigureCharacterImporter(characterPath, DanceClipName);
            if (avatar == null)
            {
                blocker = $"'{characterPath}' khong sinh duoc Avatar Humanoid. " +
                          "Kiem tra file tai tu Mixamo: Format FBX Binary, Skin With Skin.";
                return false;
            }

            var danceClip = FindFirstClip(characterPath);
            if (danceClip == null)
            {
                blocker = $"'{characterPath}' khong chua animation clip nao. " +
                          "Tai lai tu Mixamo va chon mot animation truoc khi bam Download.";
                return false;
            }

            var controller = CreateOrUpdateController(danceClip, DanceClipName);
            CreateOrUpdatePrefab(characterPath, avatar, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Practice 1] Da import '{Path.GetFileName(sourcePath)}' -> {characterPath} " +
                      $"(Avatar '{avatar.name}', clip '{danceClip.name}', state Idle <-> {DanceClipName}). " +
                      $"Prefab: {PrefabPath}");
            return true;
        }

        // ---------------------------------------------------------------- file plumbing

        private static string ProjectRoot => Directory.GetParent(Application.dataPath)!.FullName;

        private static string CopyToInbox(string sourcePath)
        {
            UrpMaterialUtility.EnsureFolderPath(InboxFolder);

            string fileName = Path.GetFileName(sourcePath);
            File.Copy(sourcePath, Path.Combine(Application.dataPath, "__MixamoInbox", fileName), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string inboxPath = $"{InboxFolder}/{fileName}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(inboxPath) == null)
            {
                Debug.LogError($"[Practice 1] Unity khong import duoc '{inboxPath}'.");
                return null;
            }

            return inboxPath;
        }

        /// <summary>
        /// Moves an imported FBX to its canonical project path. Re-importing the same file keeps the existing
        /// asset (so GUID references survive); a genuinely different file replaces it, otherwise the old model
        /// would silently stay in place.
        /// </summary>
        private static string MoveAsset(string sourcePath, string targetPath)
        {
            if (sourcePath == targetPath)
            {
                return targetPath;
            }

            UrpMaterialUtility.EnsureFolderPath(Path.GetDirectoryName(targetPath)!.Replace('\\', '/'));

            if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null)
            {
                bool sameFile = new FileInfo(AbsolutePath(sourcePath)).Length
                                == new FileInfo(AbsolutePath(targetPath)).Length;

                if (sameFile)
                {
                    AssetDatabase.DeleteAsset(sourcePath);
                    return targetPath;
                }

                AssetDatabase.DeleteAsset(targetPath);
            }

            string error = AssetDatabase.MoveAsset(sourcePath, targetPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[Practice 1] Move failed ({sourcePath} -> {targetPath}): {error}");
                return null;
            }

            return targetPath;
        }

        private static string AbsolutePath(string projectRelativePath)
        {
            return Path.Combine(ProjectRoot, projectRelativePath);
        }

        private static void ClearInbox()
        {
            if (AssetDatabase.IsValidFolder(InboxFolder))
            {
                AssetDatabase.DeleteAsset(InboxFolder);
                AssetDatabase.Refresh();
            }
        }

        // ---------------------------------------------------------------- rig and clip configuration

        /// <summary>
        /// Configures the single FBX as a Humanoid rig <b>and</b> as the animation source: the clip settings
        /// are applied to the same importer because the model and the motion live in one file.
        /// </summary>
        private static Avatar ConfigureCharacterImporter(string path, string clipName)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Practice 1] No ModelImporter for {path}");
                return null;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            ApplyClipSettings(importer, clipName);
            importer.SaveAndReimport();

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Avatar avatar)
                {
                    Debug.Log($"[Practice 1] Humanoid Avatar '{avatar.name}' generated from {path}");
                    return avatar;
                }
            }

            return null;
        }

        /// <summary>
        /// Turns every take into a looping clip and names the first one after <paramref name="firstClipName"/>.
        /// Root motion is locked because these are in-place motions: without it the character drifts away.
        /// </summary>
        private static void ApplyClipSettings(ModelImporter importer, string firstClipName)
        {
            var defaultClips = importer.defaultClipAnimations;
            var takes = importer.importedTakeInfos;
            if ((defaultClips == null || defaultClips.Length == 0) && (takes == null || takes.Length == 0))
            {
                Debug.LogWarning($"[Practice 1] No animation take found in {importer.assetPath}.");
                return;
            }

            int count = defaultClips != null && defaultClips.Length > 0 ? defaultClips.Length : takes.Length;
            var clipDefinitions = new ModelImporterClipAnimation[count];
            for (int i = 0; i < count; i++)
            {
                string takeName = takes != null && i < takes.Length ? takes[i].name : defaultClips[i].takeName;
                float first = defaultClips != null && i < defaultClips.Length ? defaultClips[i].firstFrame : 0f;
                float last = defaultClips != null && i < defaultClips.Length ? defaultClips[i].lastFrame : (takes != null ? Mathf.Round(takes[i].bakeStopTime * 60f) : 0f);

                clipDefinitions[i] = new ModelImporterClipAnimation
                {
                    name = i == 0 ? firstClipName : takeName,
                    takeName = takeName,
                    firstFrame = first,
                    lastFrame = last,
                    loopTime = true,
                    loopPose = true,
                    lockRootRotation = true,
                    keepOriginalOrientation = true,
                    lockRootHeightY = true,
                    keepOriginalPositionY = true,
                    lockRootPositionXZ = true,
                    keepOriginalPositionXZ = true
                };
            }

            importer.clipAnimations = clipDefinitions;
        }

        private static AnimationClip FindFirstClip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.Log($"[Practice 1] Animation clip '{clip.name}' imported ({clip.length:0.##}s, loop = true)");
                    return clip;
                }
            }

            return null;
        }

        // ---------------------------------------------------------------- generated assets

        private static AnimatorController CreateOrUpdateController(AnimationClip danceClip, string danceStateName)
        {
            UrpMaterialUtility.EnsureFolderPath(Path.GetDirectoryName(ControllerPath)!.Replace('\\', '/'));

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Parameter names match CharacterAnimationController so the existing runtime component keeps working.
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsDancing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "DanceTrigger", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "StopTrigger", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = FindOrAddState(stateMachine, IdleStateName, new Vector3(280f, 0f, 0f));
            var danceState = FindOrAddState(stateMachine, danceStateName, new Vector3(280f, 130f, 0f));

            // A previous run may have used a differently named animation; do not leave that state behind.
            RemoveOtherStates(stateMachine, idleState, danceState);

            danceState.motion = danceClip;
            idleState.motion = null;

            stateMachine.defaultState = danceState;

            ClearTransitions(idleState);
            ClearTransitions(danceState);

            var toDance = idleState.AddTransition(danceState);
            toDance.hasExitTime = false;
            toDance.duration = 0.2f;
            toDance.AddCondition(AnimatorConditionMode.If, 0f, "IsDancing");

            var toIdle = danceState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.2f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDancing");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Practice 1] Animator Controller '{controller.name}' configured (Idle <-> {danceStateName}).");
            return controller;
        }

        /// <summary>Drops states that are not part of the current Idle &lt;-&gt; dance pair.</summary>
        private static void RemoveOtherStates(AnimatorStateMachine stateMachine, params AnimatorState[] keep)
        {
            var existing = new List<AnimatorState>();
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null)
                {
                    existing.Add(childState.state);
                }
            }

            foreach (var state in existing)
            {
                if (!keep.Contains(state))
                {
                    stateMachine.RemoveState(state);
                }
            }
        }

        private static void CreateOrUpdatePrefab(string modelPath, Avatar avatar, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"[Practice 1] Failed to load the model at {modelPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "XBot";

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            var serializedAnimator = new SerializedObject(animator);
            var avatarProp = serializedAnimator.FindProperty("m_Avatar");
            if (avatarProp != null)
            {
                avatarProp.objectReferenceValue = avatar;
            }
            var controllerProp = serializedAnimator.FindProperty("m_Controller");
            if (controllerProp != null)
            {
                controllerProp.objectReferenceValue = controller;
            }
            var rootMotionProp = serializedAnimator.FindProperty("m_ApplyRootMotion");
            if (rootMotionProp != null)
            {
                rootMotionProp.boolValue = false;
            }
            var cullingProp = serializedAnimator.FindProperty("m_CullingMode");
            if (cullingProp != null)
            {
                cullingProp.intValue = (int)AnimatorCullingMode.AlwaysAnimate;
            }
            serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

            int swaps = UrpMaterialUtility.ReplaceNonUrpMaterials(instance, MaterialFolder, "M_XBot");
            if (swaps > 0)
            {
                Debug.Log($"[Practice 1] Replaced {swaps} non-URP material slot(s) with URP Lit materials.");
            }

            var runtimeController = instance.GetComponent<CharacterAnimationController>();
            if (runtimeController == null)
            {
                runtimeController = instance.AddComponent<CharacterAnimationController>();
            }

            // Start on Dance so the dance animation plays immediately in Play Mode.
            runtimeController.SetInitialState(CharacterState.Dance);

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Practice 1] Prefab saved at {PrefabPath}");
        }

        private static AnimatorState FindOrAddState(AnimatorStateMachine stateMachine, string name, Vector3 position)
        {
            var states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == name)
                {
                    return states[i].state;
                }
            }

            return stateMachine.AddState(name, position);
        }

        private static void ClearTransitions(AnimatorState state)
        {
            var existing = new List<AnimatorStateTransition>(state.transitions);
            foreach (var transition in existing)
            {
                state.RemoveTransition(transition);
            }
        }

        private static void EnsureParameter(AnimatorController controller, string name,
            AnimatorControllerParameterType type)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }
    }
}
