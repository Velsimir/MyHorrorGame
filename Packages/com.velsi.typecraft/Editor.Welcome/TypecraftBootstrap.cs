using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Welcome
{
    [InitializeOnLoad]
    internal static class TypecraftBootstrap
    {
        private const string Prefix = "Velsi.Typecraft.Bootstrap.";
        private const string ReportKey = Prefix + "Report";

        static TypecraftBootstrap()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += Run;
        }

        public static IReadOnlyList<string> Report
        {
            get
            {
                string value = SessionState.GetString(ReportKey, "");
                return value.Length == 0 ? new string[0] : value.Split('\n');
            }
        }

        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!TypecraftDependencies.HasTmpEssentials && Once("TmpEssentials"))
            {
                TypecraftDependencies.ImportTmpEssentials();
                AddReport("TMP Essential Resources");
            }

            if (!TypecraftDependencies.HasTmpEffects)
            {
                if (Once("TmpEffects"))
                {
                    TypecraftDependencies.InstallTmpEffects();
                    AddReport($"TMPEffects {TypecraftDependencies.TmpEffectsVersion}");
                }
            }
            else if (!TypecraftDependencies.HasTmpEffectsResources && Once("TmpEffectsResources"))
            {
                TypecraftDependencies.ImportTmpEffectsResources();
                AddReport("TMPEffects Required Resources");
            }

            if (TypecraftDependencies.Busy)
            {
                TypecraftDependencies.Changed += ShowAfterInstall;
                return;
            }

            TypecraftWelcomeWindow.ShowIfNeeded(Report.Count > 0);
        }

        private static void ShowAfterInstall()
        {
            if (TypecraftDependencies.Busy)
                return;

            TypecraftDependencies.Changed -= ShowAfterInstall;
            TypecraftWelcomeWindow.ShowIfNeeded(true);
        }

        private static bool Once(string step)
        {
            string key = Prefix + "Attempt." + step;
            if (SessionState.GetBool(key, false))
                return false;

            SessionState.SetBool(key, true);
            return true;
        }

        private static void AddReport(string item)
        {
            string value = SessionState.GetString(ReportKey, "");
            SessionState.SetString(ReportKey, value.Length == 0 ? item : value + "\n" + item);
        }
    }
}
