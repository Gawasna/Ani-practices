namespace Practice.Editor
{
    /// <summary>
    /// Single declaration of where every demo asset lives, so the folder layout is stated once instead of
    /// being repeated inside each factory. Everything the Editor scripts generate is created through these
    /// paths, which keeps the Project window predictable for a reviewer.
    ///
    /// The tree this project follows:
    /// <code>
    /// Assets/
    ///   Art/
    ///     Airplanes/Materials/   M_Fuselage.mat, M_Wing.mat               (Practice 2)
    ///     Characters/XBot/       XBot.fbx, Materials/                     (Practice 1)
    ///     Ground/Materials/      M_Ground.mat                             (both scenes)
    ///   Animations/
    ///     Clips/                 ThrottleUpAnimation.anim                 (Practice 2)
    ///     Controllers/           AC_XBot.controller, AC_AirplaneWing.controller
    ///   Prefabs/                 XBot.prefab
    ///   Scenes/                  Practice1_XBot.unity, Practice2_AirplaneWing.unity
    ///   Scripts/
    ///     UI/                    PracticeDemoHub.cs, IDemoAction.cs
    ///     Animation/             CharacterAnimationController.cs, CharacterState.cs   (character, Practice 1)
    ///     Airplane/              AirplaneTakeoffDemo.cs                                (Practice 2)
    ///     Editor/
    ///       Shared/              DemoAssetPaths.cs, PracticeDemoSceneBuilder.cs,
    ///                            PracticeDemoAutoBuildHook.cs, UrpMaterialUtility.cs
    ///       Practice1/           XBotMixamoPipeline.cs
    ///       Practice2/           PrimitiveAirplaneFactory.cs, AirplaneAnimationFactory.cs
    /// </code>
    ///
    /// The Mixamo file downloaded by hand is kept in the dot-prefixed <see cref="ResourceDropFolder"/> at the
    /// project root, which Unity ignores on purpose; the pipeline copies it into the project and deletes the
    /// temporary <see cref="InboxFolder"/> afterwards.
    /// </summary>
    public static class DemoAssetPaths
    {
        // ---------------------------------------------------------------- scenes

        public const string SceneFolder = "Assets/Scenes";
        public const string Practice1Scene = SceneFolder + "/Practice1_XBot.unity";
        public const string Practice2Scene = SceneFolder + "/Practice2_AirplaneWing.unity";

        /// <summary>Unity's default scene: the host scene used to park an empty Untitled scene, never a demo scene.</summary>
        public const string DefaultScene = SceneFolder + "/SampleScene.unity";

        // ---------------------------------------------------------------- practice 1 (Mixamo X Bot)

        public const string CharacterFolder = "Assets/Art/Characters/XBot";
        public const string XBotModel = CharacterFolder + "/XBot.fbx";
        public const string XBotMaterialFolder = CharacterFolder + "/Materials";
        public const string XBotPrefab = "Assets/Prefabs/XBot.prefab";
        public const string InboxFolder = "Assets/__MixamoInbox";
        public const string ResourceDropFolder = ".resources";

        // ---------------------------------------------------------------- practice 2 (primitive airplane)

        public const string AirplaneMaterialFolder = "Assets/Art/Airplanes/Materials";
        public const string AirplaneClipFolder = "Assets/Animations/Clips";
        public const string ThrottleUpClip = AirplaneClipFolder + "/ThrottleUpAnimation.anim";

        // ---------------------------------------------------------------- controllers and shared materials

        public const string ControllerFolder = "Assets/Animations/Controllers";
        public const string XBotController = ControllerFolder + "/AC_XBot.controller";
        public const string AirplaneWingController = ControllerFolder + "/AC_AirplaneWing.controller";
        public const string GroundMaterial = "Assets/Art/Ground/Materials/M_Ground.mat";
    }
}
