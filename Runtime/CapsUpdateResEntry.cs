using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsUpdateResEntry
    {
        public static class LifetimeOrders
        {
            public const int ArrangeUpdate = 250;
        }
        public class ArrangeUpdateInitItem : ResManager.ILifetime, ResManager.IInitAsync, ResManager.IInitProgressReporter
        {
            public int Order { get { return LifetimeOrders.ArrangeUpdate; } }

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
                var unzipingFlagFilePath = ThreadSafeValues.UpdatePath + "/pending/unzipping.flag.txt";
                if (PlatDependant.IsFileExist(unzipingFlagFilePath))
                { // killed when unzipping?
                    string zippath = null;
                    using (var sr = PlatDependant.OpenReadText(unzipingFlagFilePath))
                    {
                        zippath = sr.ReadLine();
                    }

                    if (zippath != null && PlatDependant.IsFileExist(zippath))
                    {
                        var prog_unzip = PlatDependant.UnzipAsync(zippath, ThreadSafeValues.UpdatePath + "/pending");
                        while (!prog_unzip.Done)
                        {
                            if (prog_unzip.Total > 0)
                            {
                                var curstep = (int)(prog_unzip.Length * 100 / prog_unzip.Total);
                                ReportProgress("WorkingStepInPhase", null, curstep);
                            }
                            if (async) yield return null;
                            else PlatDependant.Sleep(100);
                        }
                        PlatDependant.DeleteFile(zippath);
                    }
                    PlatDependant.DeleteFile(unzipingFlagFilePath);
                }

                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    if (_RunningVer == null)
                    {
                        // delete all existing.
                        if (_PendingFiles != null)
                        {
                            for (int i = 0; i < _PendingFiles.Length; ++i)
                            {
                                if (async && AsyncWorkTimer.Check()) yield return null;
                                var file = _PendingFiles[i];
                                PlatDependant.DeleteFile(file);
                                ReportProgress("WorkingStepAdvance", null, 0);
                            }
                        }
                        if (_UpdateFiles != null)
                        {
                            for (int i = 0; i < _UpdateFiles.Length; ++i)
                            {
                                if (async && AsyncWorkTimer.Check()) yield return null;
                                var file = _UpdateFiles[i];
                                PlatDependant.DeleteFile(file);
                                ReportProgress("WorkingStepAdvance", null, 0);
                            }
                        }
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            if (!ResManager.LoadAssetsFromApk)
                            {
                                var arch = ResManager.AndroidApkZipArchive;
                                if (arch != null)
                                {
                                    var entries = arch.Entries;
                                    for (int i = 0; i < entries.Count; ++i)
                                    {
                                        if (async && AsyncWorkTimer.Check()) yield return null;
                                        try
                                        {
                                            var entry = entries[i];
                                            var name = entry.FullName;
                                            if (name.StartsWith("assets/res/") && name != "assets/res/version.txt")
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
                                        ReportProgress("WorkingStepAdvance", null, 0);
                                    }
                                }
                            }
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
                                        string obbpre = null;
                                        if (ResManager.AllNonRawExObbs[z] != null)
                                        {
                                            obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                        }
                                        var arch = zip;
                                        if (arch != null)
                                        {
                                            var entries = arch.Entries;
                                            for (int i = 0; i < entries.Count; ++i)
                                            {
                                                if (async && AsyncWorkTimer.Check()) yield return null;
                                                try
                                                {
                                                    var entry = entries[i];
                                                    var name = entry.FullName;
                                                    if (obbpre == null || name.StartsWith(obbpre))
                                                    {
                                                        if (obbpre != null)
                                                        {
                                                            name = name.Substring(obbpre.Length);
                                                        }
                                                        if (name.StartsWith("res/") && name != "res/version.txt")
                                                        {
                                                            if (!ResManager.LoadAssetsFromObb || entry.CompressedLength != entry.Length)
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
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    PlatDependant.LogError(e);
                                                }
                                                ReportProgress("WorkingStepAdvance", null, 0);
                                            }
                                        }
                                    }
                                }
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
                        var versionfile = ThreadSafeValues.UpdatePath + "/res/ver.txt";
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
                        yield break;
                    }
                    else if (_OldRunningKeys != null && _OldRunningKeys.Count > 0)
                    {
                        // delete old existing.
                        if (_PendingFiles != null)
                        {
                            string pverfile = ThreadSafeValues.UpdatePath + "/pending/res/ver.txt";
                            bool pendingready = PlatDependant.IsFileExist(pverfile);
                            if (pendingready)
                            {
                                for (int i = 0; i < _PendingFiles.Length; ++i)
                                {
                                    if (async && AsyncWorkTimer.Check()) yield return null;
                                    var file = _PendingFiles[i];
                                    var part = file.Substring(ThreadSafeValues.UpdatePath.Length + "/pending/res/".Length);
                                    if (IsResFileOld(part))
                                    {
                                        PlatDependant.DeleteFile(file);
                                    }
                                    else
                                    {
                                        if (part != "ver.txt")
                                        {
                                            PlatDependant.MoveFile(file, ThreadSafeValues.UpdatePath + "/res/" + part);
                                        }
                                    }
                                    ReportProgress("WorkingStepAdvance", null, 0);
                                }
                                PlatDependant.DeleteFile(pverfile);
                            }
                            else
                            {
                                for (int i = 0; i < _PendingFiles.Length; ++i)
                                {
                                    if (async && AsyncWorkTimer.Check()) yield return null;
                                    var file = _PendingFiles[i];
                                    PlatDependant.DeleteFile(file);
                                    ReportProgress("WorkingStepAdvance", null, 0);
                                }
                            }
                        }
                        if (_UpdateFiles != null)
                        {
                            for (int i = 0; i < _UpdateFiles.Length; ++i)
                            {
                                if (async && AsyncWorkTimer.Check()) yield return null;
                                var file = _UpdateFiles[i];
                                var part = file.Substring(ThreadSafeValues.UpdatePath.Length + "/res/".Length);
                                if (IsResFileOld(part))
                                {
                                    PlatDependant.DeleteFile(file);
                                }
                                ReportProgress("WorkingStepAdvance", null, 0);
                            }
                        }
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            if (!ResManager.LoadAssetsFromApk)
                            {
                                var arch = ResManager.AndroidApkZipArchive;
                                if (arch != null)
                                {
                                    var entries = arch.Entries;
                                    for (int i = 0; i < entries.Count; ++i)
                                    {
                                        if (async && AsyncWorkTimer.Check()) yield return null;
                                        try
                                        {
                                            var entry = entries[i];
                                            var name = entry.FullName;
                                            if (name.StartsWith("assets/res/") && name != "assets/res/version.txt")
                                            {
                                                var part = name.Substring("assets/res/".Length);
                                                if (IsResFileOld(part))
                                                {
                                                    // copy
                                                    using (var src = entry.Open())
                                                    {
                                                        using (var dst = PlatDependant.OpenWrite(ThreadSafeValues.UpdatePath + "/res/" + part))
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
                                        ReportProgress("WorkingStepAdvance", null, 0);
                                    }
                                }
                            }
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
                                        string obbpre = null;
                                        if (ResManager.AllNonRawExObbs[z] != null)
                                        {
                                            obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                        }
                                        var arch = zip;
                                        if (arch != null)
                                        {
                                            var entries = arch.Entries;
                                            for (int i = 0; i < entries.Count; ++i)
                                            {
                                                if (async && AsyncWorkTimer.Check()) yield return null;
                                                try
                                                {
                                                    var entry = entries[i];
                                                    var name = entry.FullName;
                                                    if (obbpre == null || name.StartsWith(obbpre))
                                                    {
                                                        if (obbpre != null)
                                                        {
                                                            name = name.Substring(obbpre.Length);
                                                        }
                                                        if (name.StartsWith("res/") && name != "res/version.txt")
                                                        {
                                                            if (!ResManager.LoadAssetsFromObb || entry.CompressedLength != entry.Length)
                                                            {
                                                                var part = name.Substring("res/".Length);
                                                                if (IsResFileOld(part))
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
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    PlatDependant.LogError(e);
                                                }
                                                ReportProgress("WorkingStepAdvance", null, 0);
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
                        var versionfile = ThreadSafeValues.UpdatePath + "/res/ver.txt";
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
                        yield break;
                    }
                }

                // All running version is new
                // move pending update
                if (_PendingFiles != null)
                {
                    string pverfile = ThreadSafeValues.UpdatePath + "/pending/res/ver.txt";
                    bool pendingready = PlatDependant.IsFileExist(pverfile);
                    for (int i = 0; i < _PendingFiles.Length; ++i)
                    {
                        if (async && AsyncWorkTimer.Check()) yield return null;
                        var file = _PendingFiles[i];
                        if (pendingready)
                        {
                            var part = file.Substring(ThreadSafeValues.UpdatePath.Length + "/pending/res/".Length);
                            if (part != "ver.txt")
                            {
                                PlatDependant.MoveFile(file, ThreadSafeValues.UpdatePath + "/res/" + part);
                            }
                        }
                        else
                        {
                            PlatDependant.DeleteFile(file);
                        }
                        ReportProgress("WorkingStepAdvance", null, 0);
                    }
                    PlatDependant.DeleteFile(pverfile);
                    if (_RunningVer != null && pendingready)
                    {
                        // write version
                        var versionfile = ThreadSafeValues.UpdatePath + "/res/ver.txt";
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
                }
            }

            public event ResManager.ProgressReportDelegate ReportProgress = (key, attached, val) => { };
            public string GetPhaseDesc()
            {
                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    if (_RunningVer == null)
                    {
                        return "CheckingFirstRun";
                    }
                    else if (_OldRunningKeys != null && _OldRunningKeys.Count > 0)
                    {
                        return "CheckingPackage";
                    }
                }
                return "CheckingClient";
            }
            public int CountWorkStep()
            {
                int unzipCount = 0;
                var unzipingFlagFilePath = ThreadSafeValues.UpdatePath + "/pending/unzipping.flag.txt";
                if (PlatDependant.IsFileExist(unzipingFlagFilePath))
                { // killed when unzipping?
                    string zippath = null;
                    using (var sr = PlatDependant.OpenReadText(unzipingFlagFilePath))
                    {
                        zippath = sr.ReadLine();
                    }

                    if (zippath != null && PlatDependant.IsFileExist(zippath))
                    {
                        unzipCount = 100;
                    }
                }

                _PackageVer = 0;
                _ObbVer = 0;
                _PackageResKeys = null;
                _ObbResKeys = null;
                _RunningVer = null;
                _OldRunningKeys = null;
                _PendingFiles = null;
                _UpdateFiles = null;

                // Parse the ver num in the app package.
                ParsePackageResVersion(out _PackageVer, out _ObbVer);

                // Parse the ver num running now.
                if (_PackageVer > 0 || _ObbVer > 0)
                {
                    var uverpath = ThreadSafeValues.UpdatePath + "/res/ver.txt";
                    if (PlatDependant.IsFileExist(uverpath))
                    {
                        _RunningVer = ParseRunningResVersion();
                        if (_RunningVer.Count == 0)
                        {
                            _RunningVer = null;
                        }
                    }
                    else
                    {
                        // _RunningVer = null;
                        // this means: should delete all.
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
                            }
                        }
                    }
                }

                int workcnt = 0;
                if (!IsAllRunningVersionNew)
                {
                    // Parse res keys in the app package.
                    if (Application.streamingAssetsPath.Contains("://"))
                    {
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            if (_PackageVer > 0)
                            {
                                ResManager.SkipPending = true;
                                ResManager.SkipUpdate = true;
                                ResManager.SkipObb = true;
                                ResManager.SkipPackage = false;

                                _PackageResKeys = ParseRunningResKeys();
                                if (_PackageResKeys.Count == 0)
                                {
                                    _PackageResKeys = null;
                                }
                            }
                            if (_ObbVer > 0)
                            {
                                ResManager.SkipPending = true;
                                ResManager.SkipUpdate = true;
                                ResManager.SkipPackage = true;
                                ResManager.SkipObb = false;

                                _ObbResKeys = ParseRunningResKeys();
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
                        ResManager.SkipPending = true;
                        ResManager.SkipUpdate = true;
                        ResManager.SkipObb = true;
                        ResManager.SkipPackage = false;

                        _PackageResKeys = ParseRunningResKeys();
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
                            if (Application.platform == RuntimePlatform.Android && !IsAllRunningVersionNew && !ResManager.LoadAssetsFromApk)
                            {
                                try
                                {
                                    workcnt += ResManager.AndroidApkZipArchive.Entries.Count;
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                            }
                        }
                        if (_ObbResKeys != null)
                        {
                            bool _ObbIsNew = false;
                            for (int i = 0; i < _ObbResKeys.Count; ++i)
                            {
                                var key = _ObbResKeys[i];
                                if (_RunningVer.ContainsKey(key) && _RunningVer[key] < _ObbVer)
                                {
                                    _ObbIsNew = true;
                                    IsAllRunningVersionNew = false;
                                    _OldRunningKeys = _OldRunningKeys ?? new HashSet<string>();
                                    _OldRunningKeys.Add(key);
                                }
                            }
                            if (_ObbIsNew)
                            {
                                try
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
                                            if (zip != null)
                                            {
                                                workcnt += zip.Entries.Count;
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

                if (_RunningVer == null)
                {
                    if (Application.platform == RuntimePlatform.Android && !ResManager.LoadAssetsFromApk)
                    {
                        try
                        {
                            workcnt += ResManager.AndroidApkZipArchive.Entries.Count;
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    if (_ObbVer > 0)
                    {
                        try
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
                                    if (zip != null)
                                    {
                                        workcnt += zip.Entries.Count;
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

                if (IsAllRunningVersionNew)
                {
                    // Check whether the EntrySceneBg is pending to be updated.
                    ResManager.SkipPackage = false;
                    ResManager.SkipObb = false;
                    ResManager.SkipUpdate = false;
                    ResManager.SkipPending = false;

                    List<string> entryPendingAbs = new List<string>();
                    if (PlatDependant.IsFileExist(ThreadSafeValues.UpdatePath + "/pending/res/ver.txt"))
                    {
                        CapsUnityMainBehav.LoadEntrySceneBg();
                        foreach (var kvploaded in ResManager.LoadedAssetBundles)
                        {
                            if (kvploaded.Value != null)
                            {
                                var abname = kvploaded.Key;
                                if (kvploaded.Value.RealName != null)
                                {
                                    abname = kvploaded.Value.RealName;
                                }
                                string path = ThreadSafeValues.UpdatePath + "/pending/res/" + abname;
                                if (PlatDependant.IsFileExist(path))
                                {
                                    entryPendingAbs.Add(abname);
                                }
                            }
                        }
                        CapsUnityMainBehav.UnloadEntrySceneBg();
                    }
                    ResManager.SkipPending = true;

                    if (entryPendingAbs.Count > 0)
                    {
                        for (int i = 0; i < entryPendingAbs.Count; ++i)
                        {
                            var abname = entryPendingAbs[i];
                            var src = ThreadSafeValues.UpdatePath + "/pending/res/" + abname;
                            var dst = ThreadSafeValues.UpdatePath + "/res/" + abname;
                            PlatDependant.MoveFile(src, dst);
                        }
                    }

                    _PendingFiles = PlatDependant.GetAllFiles(ThreadSafeValues.UpdatePath + "/pending/res/");
                    return _PendingFiles.Length + unzipCount;
                }
                else
                {
                    ResManager.SkipPackage = false;
                    ResManager.SkipObb = false;
                    ResManager.SkipUpdate = true;
                    ResManager.SkipPending = true;

                    _PendingFiles = PlatDependant.GetAllFiles(ThreadSafeValues.UpdatePath + "/pending/res/");
                    _UpdateFiles = PlatDependant.GetAllFiles(ThreadSafeValues.UpdatePath + "/res/");
                    return workcnt + _PendingFiles.Length + _UpdateFiles.Length + unzipCount;
                }
            }
        }
        public static readonly ArrangeUpdateInitItem _ArrangeUpdateInitItem = new ArrangeUpdateInitItem();

        private static int _PackageVer = 0;
        private static int _ObbVer = 0;
        private static List<string> _PackageResKeys = null;
        private static List<string> _ObbResKeys = null;
        private static Dictionary<string, int> _RunningVer = null;
        private static HashSet<string> _OldRunningKeys = null;
        private static string[] _PendingFiles = null;
        private static string[] _UpdateFiles = null;

        public static void ParsePackageResVersion(out int packageVer, out int obbVer)
        {
            packageVer = 0;
            obbVer = 0;
            // Parse the ver num in the app package.
            if (Application.streamingAssetsPath.Contains("://"))
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    { // Apk ver.
                        var arch = ResManager.AndroidApkZipArchive;
                        if (arch != null)
                        {
                            try
                            {
                                var entry = arch.GetEntry("assets/res/version.txt");
                                if (entry != null)
                                {
                                    using (var stream = entry.Open())
                                    {
                                        using (var sr = new System.IO.StreamReader(stream))
                                        {
                                            var strver = sr.ReadLine();
                                            int.TryParse(strver, out packageVer);
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
                    { // Obb ver.
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
                                        var entryname = "res/version.txt";
                                        if (ResManager.AllNonRawExObbs[z] != null)
                                        {
                                            var obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                            if (obbpre != null)
                                            {
                                                entryname = obbpre + entryname;
                                            }
                                        }
                                        var entry = arch.GetEntry(entryname);
                                        if (entry != null)
                                        {
                                            using (var stream = entry.Open())
                                            {
                                                using (var sr = new System.IO.StreamReader(stream))
                                                {
                                                    var strver = sr.ReadLine();
                                                    if (int.TryParse(strver, out obbVer))
                                                    {
                                                        break;
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
                else
                {
                    var vertxt = Resources.Load<TextAsset>("version");
                    if (vertxt != null)
                    {
                        try
                        {
                            var strver = vertxt.text;
                            int.TryParse(strver, out packageVer);
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
                var path = Application.streamingAssetsPath + "/res/version.txt";
                if (PlatDependant.IsFileExist(path))
                {
                    using (var sr = PlatDependant.OpenReadText(path))
                    {
                        var strver = sr.ReadLine();
                        int.TryParse(strver, out packageVer);
                    }
                }
            }

        }
        public static void GetPackageResVersion(out int packageVer, out int obbVer)
        {
            if (_PackageVer == 0 && _ObbVer == 0)
            {
                ParsePackageResVersion(out _PackageVer, out _ObbVer);
            }
            packageVer = _PackageVer;
            obbVer = _ObbVer;
        }
        public static Dictionary<string, int> ParseResVersionInFolder(string folder)
        {
            Dictionary<string, int> versions = new Dictionary<string, int>();
            var verfolder = folder + "/version";
            var files = PlatDependant.GetAllFiles(verfolder);
            for (int i = 0; i < files.Length; ++i)
            {
                var file = files[i];
                if (file != null && file.EndsWith(".txt"))
                {
                    var sub = file.Substring(verfolder.Length + 1, file.Length - verfolder.Length - 5);
                    using (var sr = PlatDependant.OpenReadText(file))
                    {
                        if (sr != null)
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    int ver;
                                    bool success = int.TryParse(line, out ver);
                                    if (success)
                                    {
                                        versions[sub] = ver;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    PlatDependant.DeleteFile(file);
                }
            }
            if (versions.Count > 0)
            {
                using (var sw = PlatDependant.OpenAppendText(folder + "/ver.txt"))
                {
                    foreach (var kvp in versions)
                    {
                        sw.Write(kvp.Key);
                        sw.Write("|");
                        sw.WriteLine(kvp.Value);
                    }
                }
            }
            PlatDependant.DeleteFile(folder + "/version.txt");
            return versions;
        }
        public static Dictionary<string, int> ParseResVersion(string verfile)
        {
            Dictionary<string, int> versions = new Dictionary<string, int>();

            if (PlatDependant.IsFileExist(verfile))
            {
                try
                {
                    using (var sr = PlatDependant.OpenReadText(verfile))
                    {
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            if (line != "")
                            {
                                var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts != null && parts.Length >= 2)
                                {
                                    var reskey = parts[0];
                                    if (!string.IsNullOrEmpty(reskey))
                                    {
                                        int partver = 0;
                                        if (int.TryParse(parts[1], out partver))
                                        {
                                            if (versions.ContainsKey(reskey))
                                            {
                                                PlatDependant.LogWarning("Res version record duplicated key: " + reskey);
                                            }
                                            versions[reskey] = partver;
                                        }
                                        else
                                        {
                                            PlatDependant.LogWarning("Res version num is invalid in: " + line);
                                        }
                                    }
                                    else
                                    {
                                        PlatDependant.LogWarning("Res version key is invalid in: " + line);
                                    }
                                }
                                else
                                {
                                    PlatDependant.LogWarning("Res version record line is invalid: " + line);
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
            return versions;
        }
        public static Dictionary<string, int> ParseRunningResVersion()
        {
            var uverpath = ThreadSafeValues.UpdatePath + "/res/ver.txt";
            var resver = ParseResVersion(uverpath);
            ParseResVersionInFolder(ThreadSafeValues.UpdatePath + "/pending/res");
            var pverpath = ThreadSafeValues.UpdatePath + "/pending/res/ver.txt";
            var pver = ParseResVersion(pverpath);
            foreach (var kvpver in pver)
            {
                resver[kvpver.Key] = kvpver.Value;
            }
            return resver;
        }
        public static List<string> ParseRunningResKeys()
        {
            List<string> keys = new List<string>();
            var maniabs = ResManager.GetAllResManiBundleNames();
            if (maniabs != null && maniabs.Length > 0)
            {
                for (int i = 0; i < maniabs.Length; ++i)
                {
                    var maniab = maniabs[i];
                    if (maniab.EndsWith(".m.ab"))
                    {
                        var reskey = maniab.Substring("mani/".Length, maniab.Length - "mani/".Length - ".m.ab".Length);
                        keys.Add(reskey);
                    }
                }
            }
            return keys;
        }
        public static bool IsResOld(string mod, string dist)
        {
            if (_OldRunningKeys != null)
            {
                var key = "m-" + (mod ?? "").ToLower() + "-d-" + (dist ?? "").ToLower();
                if (_OldRunningKeys.Contains(key))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsResFileOld(string resfile)
        {
            if (_OldRunningKeys != null && resfile != null)
            {
                if (resfile.EndsWith(".m.ab"))
                {
                    var key = System.IO.Path.GetFileName(resfile);
                    key = key.Substring(0, key.Length - ".m.ab".Length);
                    if (_OldRunningKeys.Contains(key))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (resfile.EndsWith(".ab"))
                {
                    var file = System.IO.Path.GetFileName(resfile);
                    foreach (var key in _OldRunningKeys)
                    {
                        if (file.StartsWith(key) && file.Length > key.Length && file[key.Length] == '-')
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    var mod = System.IO.Path.GetFileName(resfile);
                    var keypre = "m-" + mod.ToLower() + "-d-";
                    foreach (var key in _OldRunningKeys)
                    {
                        if (key.StartsWith(keypre))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
#if !UNITY_EDITOR
            ResManager.AddInitItem(ResManager.LifetimeOrders.EntrySceneBgLoad, CapsUnityMainBehav.LoadEntrySceneBg);
            ResManager.AddInitItem(ResManager.LifetimeOrders.EntrySceneBgUnload, CapsUnityMainBehav.UnloadEntrySceneBg);
            ResManager.AddInitItem(_ArrangeUpdateInitItem);
#endif
        }
    }
}