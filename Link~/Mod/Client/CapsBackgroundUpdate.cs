using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using Capstones.Net;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsBackgroundUpdateUtils
    {
        public static void RefreshWhenBackgroundUpdateDone()
        {
            ResManager.ForgetMissingAssetBundles();
        }

#if MOD_CAPSNETWORK
        public class BackgroundUpdateInfo
        {
            public string Url;
            public string Path;
            public bool CheckZip;
            public string UnzipTo;

            public BackgroundUpdateInfo() { }
            public BackgroundUpdateInfo(string url, string topath, bool checkzip, string unziptodir)
            {
                Url = url;
                Path = topath;
                CheckZip = checkzip;
                UnzipTo = unziptodir;
            }
        }
        private struct VolatileBool
        {
            public volatile bool Value;
            public VolatileBool(bool v)
            {
                Value = v;
            }

            public static implicit operator bool(VolatileBool v)
            {
                return v.Value;
            }
        }
        private struct VolatileInt
        {
            public volatile int Value;
            public VolatileInt(int v)
            {
                Value = v;
            }

            public static implicit operator int(VolatileInt v)
            {
                return v.Value;
            }
        }
        private struct Volatile<T>
        {
            private volatile object _Value;
            public T Value
            {
                get { return _Value.As<T>(); }
                set { _Value = value; }
            }
            public Volatile(T v)
            {
                _Value = v;
            }
        }
        public static TaskProgress UpdateBackground(BackgroundUpdateInfo[] infos)
        {
            var prog = new TaskProgress();
            if (infos == null || infos.Length == 0)
            {
                prog.Done = true;
            }
            else
            {
                VolatileBool cancelled = new VolatileBool(false);
                VolatileInt index = new VolatileInt(0);
                Volatile<TaskProgress> subprog = new Volatile<TaskProgress>();
                prog.Total = infos.Length * 100;
                prog.OnCancel = () =>
                {
                    cancelled.Value = true;
                    var sub = subprog.Value;
                    while (true)
                    {
                        if (sub != null)
                        {
                            sub.Cancel();
                        }
                        var newsub = subprog.Value;
                        if (newsub == sub)
                        {
                            break;
                        }
                        sub = newsub;
                    }
                };
                Action updateOne = null;
                updateOne = () =>
                {
                    var currentindex = index.Value;
                    if (currentindex >= infos.Length)
                    {
                        UnityThreadDispatcher.RunInUnityThreadAndWait(RefreshWhenBackgroundUpdateDone);
                        prog.Done = true;
                    }
                    else if (cancelled.Value)
                    {
                        prog.Error = "cancelled";
                        prog.Done = true;
                    }
                    else
                    {
                        index.Value = currentindex + 1;
                        var info = infos[currentindex];
                        subprog.Value = HttpRequestUtils.DownloadBackground(info.Url, info.Path,
                            error =>
                            {
                                if (error == null)
                                {
                                    if (info.UnzipTo != null)
                                    {
                                        var curprog = prog.Length;
                                        var zipprog = PlatDependant.UnzipAsync(info.Path, info.UnzipTo);
                                        while (!zipprog.Done)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            prog.Length = curprog + (long)((((float)zipprog.Length) / (float)zipprog.Total) * 5f);
                                        }
                                        if (zipprog.Error != null)
                                        {
                                            index.Value = currentindex;
                                            updateOne();
                                            return;
                                        }
                                    }
                                    updateOne();
                                }
                                else
                                {
                                    if (prog.Error == null)
                                    {
                                        prog.Error = error;
                                        prog.Done = true;
                                    }
                                }
                            },
                            reportedprog =>
                            {
                                prog.Length = currentindex * 100 + reportedprog;
                            },
                            checkpath =>
                            {
                                if (info.CheckZip)
                                {
                                    return CapsUpdateUtils.CheckZipFile(checkpath);
                                }
                                else
                                {
                                    return true;
                                }
                            });
                    }
                };
            }
            return prog;
        }
#endif
    }
}