using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsUpdateUtils
    {
        public static bool CheckZipFile(string path)
        {
            var stream = PlatDependant.OpenRead(path);
            if (stream == null)
            {
                return false;
            }
            using (stream)
            {
                try
                {
                    var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                    if (zip == null)
                    {
                        return false;
                    }
                    using (zip)
                    {
                        var entries = zip.Entries;
                        if (entries == null)
                        {
                            return false;
                        }
                        var etor = entries.GetEnumerator();
                        if (etor.MoveNext() && etor.Current != null)
                        {
                            var estream = etor.Current.Open();
                            estream.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                    return false;
                }
            }
            return true;
        }

        public static string[] GetDelayedObbNames()
        {
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
                        if (obbname.StartsWith("delayed", StringComparison.InvariantCultureIgnoreCase))
                        {
                            missingObbNames.Add(obbname);
                        }
                    }
                }
            }
            return missingObbNames.ToArray();
        }

        public static string GetObbPathForName(string name)
        {
            var allobbs = ResManager.AllObbNames;
            var allobbpaths = ResManager.AllObbPaths;
            if (allobbs != null && allobbpaths != null)
            {
                for (int i = 0; i < allobbs.Length; ++i)
                {
                    if (allobbs[i] == name)
                    {
                        if (allobbpaths.Length > i)
                        {
                            return allobbpaths[i];
                        }
                    }
                }
            }
            return null;
        }
    }
}