using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    public class CapsBuildVersionManager : CapsResBuilder.BaseResBuilderEx<CapsBuildVersionManager>, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static CapsBuildVersionManager()
        {
            //PlayerSettingsEditorUtils.OnPlayerSettingsChanged += SaveBuildConfig; // The ProjectSettings.asset is saved after click Save Project. it will be listened in AssetPostprocessor. It is not needed to listen to the modify instantly.
            PlayerSettingsEditorUtils.OnPlayerSettingsChanged += () => AssetDatabase.SaveAssets();
            CapsModEditor.ShouldAlreadyInit();
            CapsPackageEditor.OnPackagesChanged += CheckBuildConfig;
            CapsDistributeEditor.OnDistributeFlagsChanged += CheckBuildConfig;
            CheckBuildConfig();
        }
        public static void SaveBuildConfig()
        {
            var curconfig = ResManager.EditorResLoader.CheckDistributePathSafe("~Config~/", "ProjectSettings.asset");
            if (curconfig != null)
            {
                PlatDependant.CopyFile("ProjectSettings/ProjectSettings.asset", curconfig);
            }
        }
        public static void CheckBuildConfig()
        {
            var curconfig = ResManager.EditorResLoader.CheckDistributePathSafe("~Config~/", "ProjectSettings.asset");
            if (curconfig != null)
            {
                PlatDependant.CopyFile(curconfig, "ProjectSettings/ProjectSettings.asset");
            }
        }
        private class AssetPostprocessorListener : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                for (int i = 0; i < importedAssets.Length; ++i)
                {
                    var asset = importedAssets[i];
                    if (asset == "ProjectSettings/ProjectSettings.asset")
                    {
                        // The ProjectSettings.asset changed. we'll see if it is cloned from git or modified outside Unity.
                        var candis = CapsDistributeEditor.FindAssetsInModsAndDists("~Config~/", "ProjectSettings.asset");
                        Dictionary<string, string> md5s = new Dictionary<string, string>();
                        foreach (var candi in candis)
                        {
                            var md5 = CapsEditorUtils.GetFileMD5(candi) + "-" + CapsEditorUtils.GetFileLength(candi);
                            md5s[md5] = candi;
                        }

                        var pmd5 = CapsEditorUtils.GetFileMD5(asset) + "-" + CapsEditorUtils.GetFileLength(asset);
                        if (md5s.ContainsKey(pmd5))
                        {
                            // means the ProjectSettings.asset is same as some template stored in some package.
                            // it may be cloned from git, not modified on this machine.

                            // try move correct settings back.
                            var curconfig = ResManager.EditorResLoader.CheckDistributePathSafe("~Config~/", "ProjectSettings.asset");
                            if (curconfig != md5s[pmd5] && curconfig != null)
                            {
                                PlatDependant.CopyFile(curconfig, "ProjectSettings/ProjectSettings.asset");
                            }
                        }
                        else
                        {
                            // modified outside Unity.
                            SaveBuildConfig();
                        }
                        break;
                    }
                }
            }
        }

        private static HierarchicalInitializer _Initializer = new HierarchicalInitializer(0);
        public override void OnSuccess()
        {
            SyncVersion(false);
        }
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
        }
        public void OnPostprocessBuild(BuildReport report)
        {
            SyncVersion(true);
        }

        public static void SyncVersion(bool increaseAppVer)
        {
            int vermain = 0;
            int verpatch = 0;
            int verres = 0;
            int verbuild = 0;
            var vername = PlayerSettings.bundleVersion;
            if (!string.IsNullOrEmpty(vername))
            {
                var parts = vername.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts != null)
                {
                    if (parts.Length > 0)
                    {
                        int.TryParse(parts[0], out vermain);
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out verpatch);
                            if (parts.Length > 2)
                            {
                                int.TryParse(parts[parts.Length - 1], out verbuild);
                                if (parts.Length > 3)
                                {
                                    int.TryParse(parts[2], out verres);
                                }
                            }
                        }
                    }
                }
            }

            int appver = 0;
            if (PlatDependant.IsFileExist("EditorOutput/Build/Latest/app-version.txt"))
            {
                using (var sr = PlatDependant.OpenReadText("EditorOutput/Build/Latest/app-version.txt"))
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        int.TryParse(line, out appver);
                    }
                }
            }
            if (appver <= 0)
            {
                appver = verbuild;
            }
            if (appver <= 0)
            {
                appver = PlayerSettings.Android.bundleVersionCode;
                appver /= 100000;
            }
            if (appver <= 0)
            {
                int.TryParse(PlayerSettings.iOS.buildNumber, out appver);
                appver /= 100000;
            }
            if (appver < 0)
            {
                appver = 0;
            }
            if (increaseAppVer)
            {
                ++appver;
            }
            using (var sw = PlatDependant.OpenWriteText("EditorOutput/Build/Latest/app-version.txt"))
            {
                sw.Write(appver);
            }

            int resver = 0;
            int lastBuildVersion = 0;
            int streamingVersion = 0;
            if (System.IO.File.Exists("Assets/StreamingAssets/res/version.txt"))
            {
                var lines = System.IO.File.ReadAllLines("Assets/StreamingAssets/res/version.txt");
                if (lines != null && lines.Length > 0)
                {
                    int.TryParse(lines[0], out streamingVersion);
                }
            }
            if (System.IO.File.Exists("EditorOutput/Build/Latest/res/version.txt"))
            {
                var lines = System.IO.File.ReadAllLines("EditorOutput/Build/Latest/res/version.txt");
                if (lines != null && lines.Length > 0)
                {
                    int.TryParse(lines[0], out lastBuildVersion);
                }
            }
            if (streamingVersion > 0 || lastBuildVersion <= 0)
            {
                resver = Math.Max(lastBuildVersion, streamingVersion);
            }
            else
            {
                resver = lastBuildVersion;
            }
            if (resver <= 0)
            {
                resver = verres;
            }
            if (resver <= 0)
            {
                resver = PlayerSettings.Android.bundleVersionCode;
                resver %= 100000;
            }
            if (resver <= 0)
            {
                int.TryParse(PlayerSettings.iOS.buildNumber, out resver);
                resver %= 100000;
            }
            if (resver < 0)
            {
                resver = 0;
            }

            var newvername = vermain + "." + verpatch + "." + resver + "." + appver;
            PlayerSettings.bundleVersion = newvername;

            var newvercode = appver * 100000 + resver;
            PlayerSettings.Android.bundleVersionCode = newvercode;
            PlayerSettings.iOS.buildNumber = newvercode.ToString();
        }
    }
}