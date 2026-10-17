using UnityEditor;
using UnityEngine;

namespace Practice.Editor
{
    /// <summary>
    /// Brings the demo up to date on its own, without waiting for a manual menu click: it imports the Mixamo
    /// FBX of Practice 1 when it appears in <c>.resources</c> and builds the two demo scenes while they are
    /// still missing. After an import the scenes are rebuilt even though they exist, so Practice 1 stops
    /// showing the "missing X Bot" placeholder; if that rebuild is blocked at that moment the retry loop
    /// keeps trying until it succeeds.
    /// It never edits the currently open scene, never closes anything and never shows a
    /// dialog: when the editor is not ready yet (for example while an unsaved untitled scene blocks scene
    /// creation) it reports the blocker once and keeps retrying quietly for a while.
    ///
    /// Kept apart from <see cref="PracticeDemoSceneBuilder"/> on purpose: the builder is pure scene
    /// construction and is also driven by the menu, while this hook owns the editor lifecycle
    /// (auto-run and retry policy).
    /// </summary>
    [InitializeOnLoad]
    internal static class PracticeDemoAutoBuildHook
    {
        private const string SessionKey = "Practice.DemoScenes.AutoBuildDone_v4";

        private const string AutoImportKey = "Practice.Mixamo.AutoImportDone_v4";

        /// <summary>Set when an import has just changed the assets, so the scenes must be rebuilt even if they exist.</summary>
        private const string RebuildNeededKey = "Practice.DemoScenes.RebuildNeeded";

        private const double CheckIntervalSeconds = 5.0;

        /// <summary>Roughly ten minutes of retries, then the menu stays the manual fallback.</summary>
        private const int MaxChecks = 120;

        private static double nextCheckAt;
        private static int remainingChecks = MaxChecks;
        private static string lastReportedBlocker = string.Empty;

        static PracticeDemoAutoBuildHook()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextCheckAt)
            {
                return;
            }

            nextCheckAt = EditorApplication.timeSinceStartup + CheckIntervalSeconds;

            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                return;
            }

            // A fresh import changes what the scenes must contain, so from then on the scenes are rebuilt
            // even when they already exist: otherwise Practice 1 would keep the "missing X Bot" placeholder.
            bool importedCharacter = TryAutoImportCharacter();
            if (importedCharacter)
            {
                SessionState.SetBool(RebuildNeededKey, true);
            }

            if (!importedCharacter && !SessionState.GetBool(RebuildNeededKey, false))
            {
                if (SessionState.GetBool(SessionKey, false))
                {
                    Stop();
                    return;
                }

                if (PracticeDemoSceneBuilder.DemoScenesValid())
                {
                    SessionState.SetBool(SessionKey, true);
                    Stop();
                    return;
                }
            }

            if (remainingChecks-- <= 0)
            {
                Stop();
                return;
            }

            if (PracticeDemoSceneBuilder.TryBuildAllDemoScenesSilently(openPractice1IfIdle: true, out string blocker))
            {
                SessionState.SetBool(RebuildNeededKey, false);
                SessionState.SetBool(SessionKey, true);
                Debug.Log(importedCharacter
                    ? "[Practice] X Bot vua duoc import: hai scene da duoc dung lai (Practice 1 gio co nhan vat)."
                    : "[Practice] Demo scenes auto-built. Open them from the Project window or with " +
                      "Practice > Open Practice 1 Scene (X Bot).");
                Stop();
                return;
            }

            if (blocker != lastReportedBlocker)
            {
                lastReportedBlocker = blocker;
                Debug.LogWarning($"[Practice] Demo scenes chua dung duoc: {blocker} " +
                                 "Se tu thu lai khi Editor san sang, hoac chay Practice > Build Demo Scenes de xu ly ngay.");
            }
        }

        /// <summary>
        /// Imports the Practice 1 character as soon as the Mixamo download is available in
        /// <see cref="XBotMixamoPipeline.ResourceFolderName"/>. Returns true only on the tick that did the
        /// import, so the caller knows the scenes have to be rebuilt right away. When no file is there yet
        /// the hook keeps polling without complaining: the download needs an Adobe login and is a manual step.
        /// </summary>
        private static bool TryAutoImportCharacter()
        {
            if (SessionState.GetBool(AutoImportKey, false))
            {
                return false;
            }

            if (XBotMixamoPipeline.HasImportedCharacter())
            {
                if (XBotMixamoPipeline.NeedsReimport())
                {
                    Debug.Log("[Practice 1] Dang cau hinh lai X Bot voi keepOriginalPositionY de khong bi lun san...");
                    if (XBotMixamoPipeline.ReconfigureExistingCharacter())
                    {
                        SessionState.SetBool(AutoImportKey, true);
                        return true;
                    }
                }

                SessionState.SetBool(AutoImportKey, true);
                return false;
            }

            if (!XBotMixamoPipeline.TryResolveSourceFbx(out string sourcePath))
            {
                return false;
            }

            SessionState.SetBool(AutoImportKey, true);

            if (!XBotMixamoPipeline.TryImport(sourcePath, out string blocker))
            {
                Debug.LogWarning($"[Practice 1] Tu dong import that bai: {blocker} " +
                                 "Chay Practice > Import Mixamo X Bot (Practice 1) de thu lai bang tay.");
                return false;
            }

            Debug.Log($"[Practice 1] Da tu dong import '{System.IO.Path.GetFileName(sourcePath)}' tu " +
                      $"{XBotMixamoPipeline.ResourceFolderName}. Dang dung lai hai scene...");
            return true;
        }

        private static void Stop()
        {
            EditorApplication.update -= Tick;
        }
    }
}
