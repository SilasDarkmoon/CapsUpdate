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
    }
}