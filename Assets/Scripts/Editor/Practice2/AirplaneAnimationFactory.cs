using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Practice.Editor
{
    /// <summary>
    /// Generates the Practice 2 assets exactly as the exercise specifies:
    /// a 1 second clip named <c>ThrottleUpAnimation</c> where Rotation.x goes 0 -> -15 degrees
    /// over 5 keyframes, plus a two state Animator Controller that plays it on a trigger.
    /// </summary>
    public static class AirplaneAnimationFactory
    {
        public const string ClipFolder = DemoAssetPaths.AirplaneClipFolder;
        public const string ClipPath = DemoAssetPaths.ThrottleUpClip;
        public const string ControllerPath = DemoAssetPaths.AirplaneWingController;
        public const string TakeoffParameter = "Takeoff";
        /// <summary>The airplane root name is owned by the factory, so both files cannot drift apart.</summary>
        public const string DemoTargetName = PrimitiveAirplaneFactory.RootName;

        /// <summary>Sample rate is 60 fps, which is what makes frame 60 equal exactly 1 second.</summary>
        public const float SampleRate = 60f;

        /// <summary>Frame numbers from the exercise sheet.</summary>
        public static readonly float[] KeyFrames = { 0f, 15f, 30f, 45f, 60f };

        /// <summary>Rotation.x values from the exercise sheet, only X is animated (y and z stay 0).</summary>
        public static readonly float[] KeyAngles = { 0f, -3.75f, -7.5f, -11.25f, -15f };

        /// <summary>
        /// Rotation is written as <c>localEulerAnglesRaw.x</c> on purpose: that is the binding the
        /// Animation window itself uses, so the window shows degrees (-15) instead of a quaternion component.
        /// </summary>
        private const string RotationCurveAttribute = "localEulerAnglesRaw.x";

        public static AnimationClip CreateOrUpdateThrottleUpClip()
        {
            UrpMaterialUtility.EnsureFolderPath(ClipFolder);

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            bool isNewClip = clip == null;
            if (isNewClip)
            {
                clip = new AnimationClip { name = "ThrottleUpAnimation" };
            }

            clip.frameRate = SampleRate;

            // A takeoff throttle movement runs once, it must not loop like the dance clip.
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var curve = BuildRotationCurve();
            foreach (string wingName in new[] { PrimitiveAirplaneFactory.LeftWingName, PrimitiveAirplaneFactory.RightWingName })
            {
                var binding = EditorCurveBinding.FloatCurve(wingName, typeof(Transform), RotationCurveAttribute);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            if (isNewClip)
            {
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();

            float length = clip.length;
            Debug.Log($"[Practice 2] Clip '{clip.name}' created at {ClipPath} | sample rate {SampleRate} fps | " +
                      $"length {length:0.###}s | keys: {DescribeKeys()}");

            return clip;
        }

        public static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var folder = System.IO.Path.GetDirectoryName(ControllerPath)?.Replace('\\', '/');
            UrpMaterialUtility.EnsureFolderPath(folder);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            EnsureParameter(controller, TakeoffParameter, AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = FindOrAddState(stateMachine, "Idle", new Vector3(280f, 0f, 0f));
            var takeoffState = FindOrAddState(stateMachine, "Takeoff", new Vector3(280f, 120f, 0f));

            takeoffState.motion = clip;
            takeoffState.speed = 1f;
            takeoffState.writeDefaultValues = true;

            // Idle keeps no motion: with Write Defaults enabled the Transform is reset to its scene value,
            // so the wing visually returns to 0 degrees when the clip finishes.
            idleState.motion = null;
            idleState.writeDefaultValues = true;

            stateMachine.defaultState = idleState;

            ClearTransitions(idleState);
            ClearTransitions(takeoffState);

            var toTakeoff = idleState.AddTransition(takeoffState);
            toTakeoff.hasExitTime = false;
            toTakeoff.duration = 0.05f;
            toTakeoff.AddCondition(AnimatorConditionMode.If, 0f, TakeoffParameter);

            // Automatic return once the clip reached its end, so the demo can be replayed immediately.
            var backToIdle = takeoffState.AddTransition(idleState);
            backToIdle.hasExitTime = true;
            backToIdle.exitTime = 1f;
            backToIdle.duration = 0.25f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Practice 2] Animator Controller '{controller.name}' configured at {ControllerPath} " +
                      "(Idle -> Takeoff on trigger, Takeoff -> Idle on clip end).");

            return controller;
        }

        /// <summary>
        /// Builds the Rotation.x curve. Tangents are computed per key from the neighbouring segment slopes,
        /// so the curve is exactly linear for the data of this exercise (constant -15 deg/s) but stays
        /// correct if the keyframe table is edited later.
        /// </summary>
        public static AnimationCurve BuildRotationCurve()
        {
            int count = Mathf.Min(KeyFrames.Length, KeyAngles.Length);
            var times = new float[count];
            for (int i = 0; i < count; i++)
            {
                times[i] = KeyFrames[i] / SampleRate;
            }

            var curve = new AnimationCurve();
            for (int i = 0; i < count; i++)
            {
                float tangent = ResolveTangent(i, times);
                curve.AddKey(new Keyframe(times[i], KeyAngles[i], tangent, tangent));
            }

            return curve;
        }

        private static float ResolveTangent(int index, float[] times)
        {
            int last = times.Length - 1;
            if (index == 0)
            {
                return SegmentSlope(0, times);
            }

            if (index == last)
            {
                return SegmentSlope(last - 1, times);
            }

            return 0.5f * (SegmentSlope(index - 1, times) + SegmentSlope(index, times));
        }

        private static float SegmentSlope(int index, float[] times)
        {
            float deltaTime = times[index + 1] - times[index];
            if (Mathf.Approximately(deltaTime, 0f))
            {
                return 0f;
            }

            return (KeyAngles[index + 1] - KeyAngles[index]) / deltaTime;
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

        private static string DescribeKeys()
        {
            var parts = new List<string>();
            for (int i = 0; i < KeyFrames.Length && i < KeyAngles.Length; i++)
            {
                parts.Add($"f{KeyFrames[i]:0}={KeyAngles[i]:0.###}");
            }

            return string.Join(", ", parts);
        }
    }
}
