using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsUpdateSptEntry
    {
        private static TaskProgress _CheckPendingUpdateProgress;
        public static TaskProgress CheckPendingUpdateProgress { get { return _CheckPendingUpdateProgress; } }

        private static HashSet<string> OpMods = new HashSet<string>();
        private static bool IsAndroid;

        public static Dictionary<string, int> ParseRunningSptVersion()
        {
            var uverpath = ThreadSafeValues.UpdatePath + "/spt/ver.txt";
            var resver = CapsUpdateResEntry.ParseResVersion(uverpath);
            CapsUpdateResEntry.ParseResVersionInFolder(ThreadSafeValues.UpdatePath + "/pending/spt");
            var pverpath = ThreadSafeValues.UpdatePath + "/pending/spt/ver.txt";
            var pver = CapsUpdateResEntry.ParseResVersion(pverpath);
            foreach (var kvpver in pver)
            {
                resver[kvpver.Key] = kvpver.Value;
            }
            return resver;
        }
        public static List<string> ParsePackageSptKeys()
        {
            List<string> combkeys = new List<string>();
            Dictionary<string, HashSet<string>> keys = new Dictionary<string, HashSet<string>>();
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (IsAndroid)
                {
                    var arch = ResManager.AndroidApkZipArchive;
                    if (arch != null)
                    {
                        try
                        {
                            var indexentry = arch.GetEntry("assets/spt/index.txt");
                            if (indexentry == null)
                            {
                                var entries = arch.Entries;
                                for (int i = 0; i < entries.Count; ++i)
                                {
                                    var name = entries[i].FullName;
                                    if (name.StartsWith("assets/spt/"))
                                    {
                                        string mod = "";
                                        string dist = "";
                                        string norm = null;
                                        if (name.StartsWith("assets/spt/mod/"))
                                        {
                                            var imodend = name.IndexOf('/', "assets/spt/mod/".Length);
                                            if (imodend > 0)
                                            {
                                                mod = name.Substring("assets/spt/mod/".Length, imodend - "assets/spt/mod/".Length);
                                                norm = name.Substring(imodend + 1);
                                            }
                                        }
                                        if (norm == null)
                                        {
                                            norm = name.Substring("assets/spt/".Length);
                                        }
                                        if (norm.StartsWith("dist/"))
                                        {
                                            var idistend = norm.IndexOf('/', "dist/".Length);
                                            if (idistend > 0)
                                            {
                                                dist = norm.Substring("dist/".Length, idistend - "dist/".Length);
                                            }
                                        }

                                        if (mod != "" && !OpMods.Contains(mod))
                                        {
                                            mod = "";
                                        }

                                        HashSet<string> dists;
                                        if (!keys.TryGetValue(mod, out dists))
                                        {
                                            dists = new HashSet<string>();
                                            keys[mod] = dists;
                                        }
                                        dists.Add(dist);
                                    }
                                }
                            }
                            else
                            {
                                using (var stream = indexentry.Open())
                                {
                                    using (var sr = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                                    {
                                        while (true)
                                        {
                                            var line = sr.ReadLine();
                                            if (line == null)
                                            {
                                                break;
                                            }
                                            line = line.Trim();
                                            if (line != "")
                                            {
                                                combkeys.Add(line);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
            }
            else
            {
                var indexpath = ThreadSafeValues.AppStreamingAssetsPath + "/spt/index.txt";
                if (PlatDependant.IsFileExist(indexpath))
                {
                    using (var sr = PlatDependant.OpenReadText(indexpath))
                    {
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            line = line.Trim();
                            if (line != "")
                            {
                                combkeys.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    string dir = ThreadSafeValues.AppStreamingAssetsPath + "/spt/";
                    var files = PlatDependant.GetAllFiles(dir);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Substring(dir.Length);
                        string mod = "";
                        string dist = "";
                        string norm = file;
                        if (file.StartsWith("mod/"))
                        {
                            var imodend = file.IndexOf('/', "mod/".Length);
                            if (imodend > 0)
                            {
                                mod = file.Substring("mod/".Length, imodend - "mod/".Length);
                                norm = file.Substring(imodend + 1);
                            }
                        }
                        if (norm.StartsWith("dist/"))
                        {
                            var idistend = norm.IndexOf('/', "dist/".Length);
                            if (idistend > 0)
                            {
                                dist = norm.Substring("dist/".Length, idistend - "dist/".Length);
                            }
                        }

                        if (mod != "" && !OpMods.Contains(mod))
                        {
                            mod = "";
                        }

                        HashSet<string> dists;
                        if (!keys.TryGetValue(mod, out dists))
                        {
                            dists = new HashSet<string>();
                            keys[mod] = dists;
                        }
                        dists.Add(dist);
                    }
                }
            }

            foreach (var kvp in keys)
            {
                var mod = kvp.Key;
                var dists = kvp.Value;
                foreach (var dist in dists)
                {
                    var key = "m-" + mod.ToLower() + "-d-" + dist.ToLower();
                    if (!combkeys.Contains(key))
                    {
                        combkeys.Add(key);
                    }
                }
            }

            return combkeys;
        }
        public static List<string> ParseObbSptKeys()
        {
            List<string> combkeys = new List<string>();
            Dictionary<string, HashSet<string>> keys = new Dictionary<string, HashSet<string>>();
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (IsAndroid)
                {
                    var allobbs = ResManager.AllObbZipArchives;
                    if (allobbs != null)
                    {
                        for (int z = 0; z < allobbs.Length; ++z)
                        {
                            if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                            { // means the obb is to be downloaded.
                                continue;
                            }

                            var zip = allobbs[z];
                            var arch = zip;
                            if (arch != null)
                            {
                                try
                                {
                                    var indexentry = arch.GetEntry("spt/index.txt");
                                    if (indexentry == null)
                                    {
                                        var entries = arch.Entries;
                                        for (int i = 0; i < entries.Count; ++i)
                                        {
                                            var name = entries[i].FullName;
                                            if (name.StartsWith("spt/"))
                                            {
                                                string mod = "";
                                                string dist = "";
                                                string norm = null;
                                                if (name.StartsWith("spt/mod/"))
                                                {
                                                    var imodend = name.IndexOf('/', "spt/mod/".Length);
                                                    if (imodend > 0)
                                                    {
                                                        mod = name.Substring("spt/mod/".Length, imodend - "spt/mod/".Length);
                                                        norm = name.Substring(imodend + 1);
                                                    }
                                                }
                                                if (norm == null)
                                                {
                                                    norm = name.Substring("spt/".Length);
                                                }
                                                if (norm.StartsWith("dist/"))
                                                {
                                                    var idistend = norm.IndexOf('/', "dist/".Length);
                                                    if (idistend > 0)
                                                    {
                                                        dist = norm.Substring("dist/".Length, idistend - "dist/".Length);
                                                    }
                                                }

                                                if (mod != "" && !OpMods.Contains(mod))
                                                {
                                                    mod = "";
                                                }

                                                HashSet<string> dists;
                                                if (!keys.TryGetValue(mod, out dists))
                                                {
                                                    dists = new HashSet<string>();
                                                    keys[mod] = dists;
                                                }
                                                dists.Add(dist);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (var stream = indexentry.Open())
                                        {
                                            using (var sr = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                                            {
                                                while (true)
                                                {
                                                    var line = sr.ReadLine();
                                                    if (line == null)
                                                    {
                                                        break;
                                                    }
                                                    line = line.Trim();
                                                    if (line != "")
                                                    {
                                                        combkeys.Add(line);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var kvp in keys)
            {
                var mod = kvp.Key;
                var dists = kvp.Value;
                foreach (var dist in dists)
                {
                    var key = "m-" + mod.ToLower() + "-d-" + dist.ToLower();
                    if (!combkeys.Contains(key))
                    {
                        combkeys.Add(key);
                    }
                }
            }

            return combkeys;
        }
        public static bool IsItemInPackageOrObb(string item)
        {
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (IsAndroid)
                {
                    { // Obb
                        var allobbs = ResManager.AllObbZipArchives;
                        if (allobbs != null)
                        {
                            for (int z = allobbs.Length - 1; z >= 0; --z)
                            {
                                if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                { // means the obb is to be downloaded.
                                    continue;
                                }

                                var zip = allobbs[z];
                                var arch = zip;
                                if (arch != null)
                                {
                                    try
                                    {
                                        var entry = arch.GetEntry(item);
                                        if (entry != null)
                                        {
                                            return true;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                }
                            }
                        }
                    }
                    { // Apk
                        var arch = ResManager.AndroidApkZipArchive;
                        if (arch != null)
                        {
                            try
                            {
                                var entry = arch.GetEntry("assets/" + item);
                                if (entry != null)
                                {
                                    return true;
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                    }
                }
            }
            else
            {
                var path = ThreadSafeValues.AppStreamingAssetsPath + "/" + item;
                if (PlatDependant.IsFileExist(path))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool IsItemOld(string item, HashSet<string> oldkeys, Dictionary<string, int> runningver)
        {
            string opmod = null;
            string mod = "";
            string dist = "";
            string norm = item;
            if (item.StartsWith("mod/"))
            {
                var imodend = item.IndexOf('/', "mod/".Length);
                if (imodend > 0)
                {
                    mod = item.Substring("mod/".Length, imodend - "mod/".Length);
                    norm = item.Substring(imodend + 1);
                }
            }
            if (norm.StartsWith("dist/"))
            {
                var idistend = norm.IndexOf('/', "dist/".Length);
                if (idistend > 0)
                {
                    dist = norm.Substring("dist/".Length, idistend - "dist/".Length);
                }
            }

            if (mod != "" && !OpMods.Contains(mod))
            {
                opmod = mod;
                mod = "";
            }

            var key = "m-" + mod.ToLower() + "-d-" + dist.ToLower();
            bool shouldDelete = false;
            if (oldkeys.Contains(key))
            {
                shouldDelete = true;
                if (opmod != null)
                {
                    var opkey = "m-" + opmod.ToLower() + "-d-" + dist.ToLower();
                    if (runningver.ContainsKey(opkey))
                    {
                        shouldDelete = false;
                        if (IsItemInPackageOrObb("spt/" + item))
                        {
                            shouldDelete = true;
                        }
                    }
                }
            }
            return shouldDelete;
        }
        public static void CheckPendingUpdateWork(TaskProgress progress)
        {
            try
            {
                int _PackageVer = 0;
                int _ObbVer = 0;
                List<string> _PackageResKeys = null;
                List<string> _ObbResKeys = null;
                Dictionary<string, int> _RunningVer = null;
                HashSet<string> _OldRunningKeys = null;

                // Parse the ver num in the app package.
                CapsUpdateResEntry.GetPackageResVersion(out _PackageVer, out _ObbVer);

                // Parse the ver num running now.
                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    _RunningVer = ParseRunningSptVersion();
                    if (_RunningVer.Count == 0)
                    {
                        _RunningVer = null;
                    }
                }

                // Are all running versions newer than the app ver?
                bool IsAllRunningVersionNew = true;
                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    if (_RunningVer == null)
                    {
                        IsAllRunningVersionNew = false;
                    }
                    else
                    {
                        var MaxAppVer = _PackageVer > _ObbVer ? _PackageVer : _ObbVer;
                        foreach (var kvpver in _RunningVer)
                        {
                            if (kvpver.Value < MaxAppVer)
                            {
                                IsAllRunningVersionNew = false;
                                break;
                            }
                        }
                    }
                }

                if (!IsAllRunningVersionNew)
                {
                    // Rebuild the manifest file.
                    var filePath = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
                    PlatDependant.DeleteFile(filePath);

                    // Parse spt keys in the app package.
                    if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
                    {
                        if (IsAndroid)
                        {
                            if (_PackageVer > 0)
                            {
                                _PackageResKeys = ParsePackageSptKeys();
                                if (_PackageResKeys.Count == 0)
                                {
                                    _PackageResKeys = null;
                                }
                            }
                            if (_ObbVer > 0)
                            {
                                _ObbResKeys = ParseObbSptKeys();
                                if (_ObbResKeys.Count == 0)
                                {
                                    _ObbResKeys = null;
                                }
                            }
                        }
                        else
                        {
                            _PackageResKeys = new List<string>(_RunningVer.Keys);
                        }
                    }
                    else
                    {
                        _PackageResKeys = ParsePackageSptKeys();
                        if (_PackageResKeys.Count == 0)
                        {
                            _PackageResKeys = null;
                        }
                    }

                    if (_RunningVer != null)
                    {
                        // Check ver
                        IsAllRunningVersionNew = true;
                        if (_PackageResKeys != null)
                        {
                            for (int i = 0; i < _PackageResKeys.Count; ++i)
                            {
                                var key = _PackageResKeys[i];
                                if (_RunningVer.ContainsKey(key) && _RunningVer[key] < _PackageVer)
                                {
                                    IsAllRunningVersionNew = false;
                                    _OldRunningKeys = _OldRunningKeys ?? new HashSet<string>();
                                    _OldRunningKeys.Add(key);
                                }
                            }
                        }
                        if (_ObbResKeys != null)
                        {
                            for (int i = 0; i < _ObbResKeys.Count; ++i)
                            {
                                var key = _ObbResKeys[i];
                                if (_RunningVer.ContainsKey(key) && _RunningVer[key] < _ObbVer)
                                {
                                    IsAllRunningVersionNew = false;
                                    _OldRunningKeys = _OldRunningKeys ?? new HashSet<string>();
                                    _OldRunningKeys.Add(key);
                                }
                            }
                        }
                    }
                }
//#if DEVELOPMENT_BUILD
//// We want development build always use the newest manifest. But we will rebuild lua and the version will be increase automatically, so this is needless.
//                else
//                {
//                    var filePath = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
//                    PlatDependant.DeleteFile(filePath);
//                }
//#endif

                // do delete or move
                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    if (!IsAllRunningVersionNew)
                    {
                        if (_RunningVer == null || (_OldRunningKeys != null && _RunningVer.Count == _OldRunningKeys.Count))
                        {
                            // delete all existing.
                            var pendingfiles = PlatDependant.GetAllFiles(ThreadSafeValues.UpdatePath + "/pending/spt/");
                            var updatefiles = PlatDependant.GetAllFiles(ThreadSafeValues.UpdatePath + "/spt/");
                            progress.Total = pendingfiles.Length + updatefiles.Length;
                            progress.Length = 0;
                            if (IsAndroid)
                            {
                                if (!ResManager.LoadAssetsFromApk)
                                {
                                    try
                                    {
                                        progress.Total += ResManager.AndroidApkZipArchive.Entries.Count;
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                }
                                if (!ResManager.LoadAssetsFromObb)
                                {
                                    var allobbs = ResManager.AllObbZipArchives;
                                    if (allobbs != null)
                                    {
                                        for (int z = 0; z < allobbs.Length; ++z)
                                        {
                                            if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                            { // means the obb is to be downloaded.
                                                continue;
                                            }

                                            var zip = allobbs[z];
                                            var zobb = zip;
                                            if (zobb != null)
                                            {
                                                try
                                                {
                                                    progress.Total += zobb.Entries.Count;
                                                }
                                                catch (Exception e)
                                                {
                                                    PlatDependant.LogError(e);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            for (int i = 0; i < pendingfiles.Length; ++i)
                            {
                                var file = pendingfiles[i];
                                PlatDependant.DeleteFile(file);
                                ++progress.Length;
                            }
                            for (int i = 0; i < updatefiles.Length; ++i)
                            {
                                var file = updatefiles[i];
                                PlatDependant.DeleteFile(file);
                                ++progress.Length;
                            }
                            if (IsAndroid)
                            {
                                if (!ResManager.LoadAssetsFromApk)
                                {
                                    var arch = ResManager.AndroidApkZipArchive;
                                    if (arch != null)
                                    {
                                        var entries = arch.Entries;
                                        for (int i = 0; i < entries.Count; ++i)
                                        {
                                            try
                                            {
                                                var entry = entries[i];
                                                var name = entry.FullName;
                                                if (name.StartsWith("assets/spt/") && name != "assets/spt/version.txt")
                                                {
                                                    // copy
                                                    using (var src = entry.Open())
                                                    {
                                                        using (var dst = PlatDependant.OpenWrite(ThreadSafeValues.UpdatePath + "/" + name.Substring("assets/".Length)))
                                                        {
                                                            src.CopyTo(dst);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                PlatDependant.LogError(e);
                                            }
                                            ++progress.Length;
                                        }
                                    }
                                }
                                if (!ResManager.LoadAssetsFromObb)
                                {
                                    var allobbs = ResManager.AllObbZipArchives;
                                    if (allobbs != null)
                                    {
                                        for (int z = 0; z < allobbs.Length; ++z)
                                        {
                                            if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                            { // means the obb is to be downloaded.
                                                continue;
                                            }

                                            var zip = allobbs[z];
                                            var arch = zip;
                                            if (arch != null)
                                            {
                                                var entries = arch.Entries;
                                                for (int i = 0; i < entries.Count; ++i)
                                                {
                                                    try
                                                    {
                                                        var entry = entries[i];
                                                        var name = entry.FullName;
                                                        if (name.StartsWith("spt/") && name != "spt/version.txt")
                                                        {
                                                            // copy
                                                            using (var src = entry.Open())
                                                            {
                                                                using (var dst = PlatDependant.OpenWrite(ThreadSafeValues.UpdatePath + "/" + name))
                                                                {
                                                                    src.CopyTo(dst);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        PlatDependant.LogError(e);
                                                    }
                                                    ++progress.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // the default manifest
                            var manifile = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
                            var manifiletmp = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt.tmp";
                            PlatDependant.DeleteFile(manifile);
                            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
                            {
                                if (IsAndroid)
                                {
                                    List<ZipArchive> archs = new List<ZipArchive>(4) { ResManager.AndroidApkZipArchive };
                                    var allobbs = ResManager.AllObbZipArchives;
                                    if (allobbs != null)
                                    {
                                        archs.AddRange(allobbs);
                                    }
                                    for (int i = 0; i < archs.Count; ++i)
                                    {
                                        var arch = archs[i];
                                        if (arch != null)
                                        {
                                            try
                                            {
                                                var entry = arch.GetEntry("assets/spt/manifest.m.txt");
                                                if (entry != null)
                                                {
                                                    // copy
                                                    using (var src = entry.Open())
                                                    {
                                                        using (var dst = PlatDependant.OpenWrite(manifiletmp))
                                                        {
                                                            src.CopyTo(dst);
                                                        }
                                                    }
                                                    PlatDependant.MoveFile(manifiletmp, manifile);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                PlatDependant.LogError(e);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var pmani = ThreadSafeValues.AppStreamingAssetsPath + "/spt/manifest.m.txt";
                                if (PlatDependant.IsFileExist(pmani))
                                {
                                    PlatDependant.CopyFile(pmani, manifiletmp);
                                    PlatDependant.MoveFile(manifiletmp, manifile);
                                }
                            }
                            // write version
                            var finalVersions = new Dictionary<string, int>();
                            if (_PackageResKeys != null)
                            {
                                for (int i = 0; i < _PackageResKeys.Count; ++i)
                                {
                                    finalVersions[_PackageResKeys[i]] = _PackageVer;
                                }
                            }
                            if (_ObbResKeys != null)
                            {
                                for (int i = 0; i < _ObbResKeys.Count; ++i)
                                {
                                    finalVersions[_ObbResKeys[i]] = _ObbVer;
                                }
                            }
                            var versionfile = ThreadSafeValues.UpdatePath + "/spt/ver.txt";
                            var versionfiletmp = versionfile + ".tmp";
                            using (var sw = PlatDependant.OpenWriteText(versionfiletmp))
                            {
                                foreach (var kvpver in finalVersions)
                                {
                                    sw.Write(kvpver.Key);
                                    sw.Write("|");
                                    sw.Write(kvpver.Value);
                                    sw.WriteLine();
                                }
                                sw.Flush();
                            }
                            PlatDependant.MoveFile(versionfiletmp, versionfile);
                            return;
                        }
                        else if (_OldRunningKeys != null && _OldRunningKeys.Count > 0)
                        {
                            // delete old existing.
                            var pendingdir = ThreadSafeValues.UpdatePath + "/pending/spt/";
                            var updatedir = ThreadSafeValues.UpdatePath + "/spt/";
                            var pendingfiles = PlatDependant.GetAllFiles(pendingdir);
                            var updatefiles = PlatDependant.GetAllFiles(updatedir);
                            progress.Total = pendingfiles.Length + updatefiles.Length;
                            progress.Length = 0;
                            if (IsAndroid)
                            {
                                if (!ResManager.LoadAssetsFromApk)
                                {
                                    try
                                    {
                                        progress.Total += ResManager.AndroidApkZipArchive.Entries.Count;
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                }
                                if (!ResManager.LoadAssetsFromObb)
                                {
                                    var allobbs = ResManager.AllObbZipArchives;
                                    if (allobbs != null)
                                    {
                                        for (int z = 0; z < allobbs.Length; ++z)
                                        {
                                            if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                            { // means the obb is to be downloaded.
                                                continue;
                                            }

                                            var zip = allobbs[z];
                                            var zobb = zip;
                                            if (zobb != null)
                                            {
                                                try
                                                {
                                                    progress.Total += zobb.Entries.Count;
                                                }
                                                catch (Exception e)
                                                {
                                                    PlatDependant.LogError(e);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            string pverfile = ThreadSafeValues.UpdatePath + "/pending/spt/ver.txt";
                            bool pendingready = PlatDependant.IsFileExist(pverfile);
                            if (pendingready)
                            {
                                for (int i = 0; i < pendingfiles.Length; ++i)
                                {
                                    var rawfile = pendingfiles[i];
                                    var file = rawfile.Substring(pendingdir.Length);
                                    bool shouldDelete = IsItemOld(file, _OldRunningKeys, _RunningVer);
                                    if (shouldDelete)
                                    {
                                        PlatDependant.DeleteFile(rawfile);
                                    }
                                    else
                                    {
                                        if (file != "ver.txt")
                                        {
                                            PlatDependant.MoveFile(rawfile, ThreadSafeValues.UpdatePath + "/spt/" + file);
                                        }
                                    }
                                    ++progress.Length;
                                }
                                PlatDependant.DeleteFile(pverfile);
                            }
                            else
                            {
                                for (int i = 0; i < pendingfiles.Length; ++i)
                                {
                                    var file = pendingfiles[i];
                                    PlatDependant.DeleteFile(file);
                                    ++progress.Length;
                                }
                            }
                            for (int i = 0; i < updatefiles.Length; ++i)
                            {
                                var rawfile = updatefiles[i];
                                var file = rawfile.Substring(updatedir.Length);
                                bool shouldDelete = IsItemOld(file, _OldRunningKeys, _RunningVer);
                                if (shouldDelete)
                                {
                                    PlatDependant.DeleteFile(rawfile);
                                }
                                ++progress.Length;
                            }
                            if (IsAndroid)
                            {
                                if (!ResManager.LoadAssetsFromApk)
                                {
                                    var arch = ResManager.AndroidApkZipArchive;
                                    if (arch != null)
                                    {
                                        var entries = arch.Entries;
                                        for (int i = 0; i < entries.Count; ++i)
                                        {
                                            try
                                            {
                                                var entry = entries[i];
                                                var name = entry.FullName;
                                                if (name.StartsWith("assets/spt/") && name != "assets/spt/version.txt")
                                                {
                                                    var file = name.Substring("assets/spt/".Length);
                                                    if (IsItemOld(file, _OldRunningKeys, _RunningVer))
                                                    {
                                                        // copy
                                                        using (var src = entry.Open())
                                                        {
                                                            using (var dst = PlatDependant.OpenWrite(ThreadSafeValues.UpdatePath + "/spt/" + file))
                                                            {
                                                                src.CopyTo(dst);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                PlatDependant.LogError(e);
                                            }
                                            ++progress.Length;
                                        }
                                    }
                                }
                                if (!ResManager.LoadAssetsFromObb)
                                {
                                    var allobbs = ResManager.AllObbZipArchives;
                                    if (allobbs != null)
                                    {
                                        for (int z = 0; z < allobbs.Length; ++z)
                                        {
                                            if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                            { // means the obb is to be downloaded.
                                                continue;
                                            }

                                            var zip = allobbs[z];
                                            var arch = zip;
                                            if (arch != null)
                                            {
                                                var entries = arch.Entries;
                                                for (int i = 0; i < entries.Count; ++i)
                                                {
                                                    try
                                                    {
                                                        var entry = entries[i];
                                                        var name = entry.FullName;
                                                        if (name.StartsWith("spt/") && name != "spt/version.txt")
                                                        {
                                                            var file = name.Substring("spt/".Length);
                                                            if (IsItemOld(file, _OldRunningKeys, _RunningVer))
                                                            {
                                                                // copy
                                                                using (var src = entry.Open())
                                                                {
                                                                    using (var dst = PlatDependant.OpenWrite(ThreadSafeValues.UpdatePath + "/spt/" + file))
                                                                    {
                                                                        src.CopyTo(dst);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        PlatDependant.LogError(e);
                                                    }
                                                    ++progress.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // write version
                            var finalVersions = new Dictionary<string, int>(_RunningVer);
                            if (_PackageResKeys != null)
                            {
                                for (int i = 0; i < _PackageResKeys.Count; ++i)
                                {
                                    var key = _PackageResKeys[i];
                                    if (_OldRunningKeys.Contains(key))
                                    {
                                        finalVersions[key] = _PackageVer;
                                    }
                                }
                            }
                            if (_ObbResKeys != null)
                            {
                                for (int i = 0; i < _ObbResKeys.Count; ++i)
                                {
                                    var key = _ObbResKeys[i];
                                    if (_OldRunningKeys.Contains(key))
                                    {
                                        finalVersions[key] = _ObbVer;
                                    }
                                }
                            }
                            var versionfile = ThreadSafeValues.UpdatePath + "/spt/ver.txt";
                            var versionfiletmp = versionfile + ".tmp";
                            using (var sw = PlatDependant.OpenWriteText(versionfiletmp))
                            {
                                foreach (var kvpver in finalVersions)
                                {
                                    sw.Write(kvpver.Key);
                                    sw.Write("|");
                                    sw.Write(kvpver.Value);
                                    sw.WriteLine();
                                }
                                sw.Flush();
                            }
                            PlatDependant.MoveFile(versionfiletmp, versionfile);
                            var manifile = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
                            PlatDependant.DeleteFile(manifile);
                            //CrossEvent.TrigEvent("ResetSptRuntimeManifest");
                            return;
                        }
                    }
                }

                // All running version is new
                // move pending update
                {
                    var pendingdir = ThreadSafeValues.UpdatePath + "/pending/spt/";
                    var updatedir = ThreadSafeValues.UpdatePath + "/spt/";
                    var pendingfiles = PlatDependant.GetAllFiles(pendingdir);
                    progress.Total = pendingfiles.Length;
                    progress.Length = 0;

                    string pverfile = ThreadSafeValues.UpdatePath + "/pending/spt/ver.txt";
                    bool pendingready = PlatDependant.IsFileExist(pverfile);

                    for (int i = 0; i < pendingfiles.Length; ++i)
                    {
                        var rawfile = pendingfiles[i];
                        if (pendingready)
                        {
                            var file = rawfile.Substring(pendingdir.Length);
                            if (file != "ver.txt")
                            {
                                PlatDependant.MoveFile(rawfile, updatedir + file);
                            }
                        }
                        else
                        {
                            PlatDependant.DeleteFile(rawfile);
                        }
                        ++progress.Length;
                    }
                    PlatDependant.DeleteFile(pverfile);
                    if (_RunningVer != null && pendingready)
                    {
                        // write version
                        var versionfile = ThreadSafeValues.UpdatePath + "/spt/ver.txt";
                        var versionfiletmp = versionfile + ".tmp";
                        using (var sw = PlatDependant.OpenWriteText(versionfiletmp))
                        {
                            foreach (var kvpver in _RunningVer)
                            {
                                sw.Write(kvpver.Key);
                                sw.Write("|");
                                sw.Write(kvpver.Value);
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                        PlatDependant.MoveFile(versionfiletmp, versionfile);
                    }
                    if (pendingfiles.Length > 0)
                    {
                        var manifile = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
                        PlatDependant.DeleteFile(manifile);
                        //CrossEvent.TrigEvent("ResetSptRuntimeManifest");
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                progress.Error = e.Message;
                PlatDependant.LogError(e);
            }
            finally
            {
                PrepareRuntimeManifest();
                progress.Done = true;
            }
        }
        public static void StartCheckPendingUpdate()
        {
            // Load resdesc from package. Note: we want to know whether the package is new, so we donot need the updated resdesc.
            OpMods.Clear();
            var descs = Resources.LoadAll<CapsModDesc>("resdesc");
            if (descs != null)
            {
                for (int i = 0; i < descs.Length; ++i)
                {
                    var desc = descs[i];
                    if (desc != null && !string.IsNullOrEmpty(desc.Mod))
                    {
                        if (desc.IsOptional)
                        {
                            OpMods.Add(desc.Mod);
                        }
                    }
                }
            }
            IsAndroid = ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString();
            _CheckPendingUpdateProgress = PlatDependant.RunBackground(CheckPendingUpdateWork);
        }

        private static void PrepareRuntimeManifest()
        {
            Dictionary<string, int> vers = new Dictionary<string, int>();
            foreach (var kvp in CapsUpdateResEntry.ParseRunningResVersion())
            {
                vers["res-" + kvp.Key] = kvp.Value;
            }
            foreach (var kvp in ParseRunningSptVersion())
            {
                vers["spt-" + kvp.Key] = kvp.Value;
            }

            int _PackageVer, _ObbVer;
            CapsUpdateResEntry.GetPackageResVersion(out _PackageVer, out _ObbVer);
            if (_PackageVer > 0 || _ObbVer > 0)
            {
                vers["package"] = Math.Max(_PackageVer, _ObbVer);
            }
            List<string> missingObbNames = new List<string>();
            var allobbs = ResManager.AllObbNames;
            var allobbzips = ResManager.AllObbZipArchives;
            if (allobbs != null)
            {
                for (int i = 0; i < allobbs.Length; ++i)
                {
                    var obbname = allobbs[i];
                    var obbzip = (allobbzips == null || i >= allobbzips.Length) ? null : allobbzips[i];
                    if (obbzip == null)
                    {
                        missingObbNames.Add(obbname);
                    }
                }
                for (int i = 0; i < missingObbNames.Count; ++i)
                {
                    vers["obb-" + missingObbNames[i]] = 0;
                }
            }

            CrossEvent.TrigClrEvent("SptManifestReady", new CrossEvent.RawEventData<Dictionary<string, int>>(vers));
        }

        public class ArrangeUpdateInitItem : ResManager.ILifetime, ResManager.IInitAsync, ResManager.IInitProgressReporter
        {
            public int Order { get { return CapsUpdateResEntry.LifetimeOrders.ArrangeUpdate + 5; } }

            public void Prepare()
            {
                CountWorkStep();
            }
            public void Init()
            {
                var work = InitAsync(false);
                while (work.MoveNext()) ;
            }
            public void Cleanup() { }

            public IEnumerator InitAsync()
            {
                return InitAsync(true);
            }
            public IEnumerator InitAsync(bool async)
            {
                if (_CheckPendingUpdateProgress != null)
                {
                    while (!_CheckPendingUpdateProgress.Done)
                    {
                        if (_CheckPendingUpdateProgress.Total > 0)
                        {
                            int curstep = (int)_CheckPendingUpdateProgress.Length;
                            if (_WorkStep == 100 && _WorkStep != _CheckPendingUpdateProgress.Total)
                            {
                                curstep = (int)(_CheckPendingUpdateProgress.Length * 100 / _CheckPendingUpdateProgress.Total);
                            }
                            ReportProgress("WorkingStepInPhase", null, curstep);
                        }
                        yield return null;
                    }
                }
                //PrepareRuntimeManifest();
                yield break;
            }

            public event ResManager.ProgressReportDelegate ReportProgress = (key, attached, val) => { };
            public string GetPhaseDesc()
            {
                if (_CheckPendingUpdateProgress != null)
                {
                    if (!_CheckPendingUpdateProgress.Done)
                    {
                        return "CheckingFirstRun";
                    }
                }
                return null;
            }
            private int _WorkStep = 0;
            public int CountWorkStep()
            {
                if (_CheckPendingUpdateProgress != null)
                {
                    if (!_CheckPendingUpdateProgress.Done)
                    {
                        if (_CheckPendingUpdateProgress.Total > 0)
                        {
                            _WorkStep = (int)_CheckPendingUpdateProgress.Total;
                        }
                        else
                        {
                            _WorkStep = 100;
                        }
                    }
                }
                return _WorkStep;
            }
        }
        public static readonly ArrangeUpdateInitItem _ArrangeUpdateInitItem = new ArrangeUpdateInitItem();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
#if !UNITY_EDITOR
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.ABLoader + 5, StartCheckPendingUpdate, null));
            ResManager.AddInitItem(_ArrangeUpdateInitItem);
#endif
        }

    }
}
