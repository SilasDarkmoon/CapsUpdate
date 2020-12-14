#if MOD_CAPSRESMANAGER
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public static class AggregatedBuildCommands
    {
        [MenuItem("Res/Build All (Raw)", priority = 200140)]
        public static void BuildAllRaw()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            CapsResBuilder.BuildingParams.makezip = false;
            IEnumerator work;
#if MOD_CAPSLUA
            work = CapsSptBuilder.BuildSptAsync(null, null, new[] { new CapsSptBuilder.SptBuilderEx_RawCopy() });
            while (work.MoveNext()) ;
#endif
            work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
        }
        [MenuItem("Res/Build All (Quick)", priority = 200150)]
        public static void BuildAllQuick()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            CapsResBuilder.BuildingParams.makezip = false;
            IEnumerator work;
#if MOD_CAPSLUA
            work = CapsSptBuilder.BuildSptAsync(null, null);
            while (work.MoveNext()) ;
#endif
            work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
        }
        [MenuItem("Res/Build All (Full)", priority = 200160)]
        public static void BuildAllFull()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            IEnumerator work;
#if MOD_CAPSLUA
            work = CapsSptBuilder.BuildSptAsync(null, null);
            while (work.MoveNext()) ;
#endif
            work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
            CapsUpdateBuilder.BuildNearestUpdate();
        }
        [MenuItem("Res/Build All (Full, With Progress Window)", priority = 200170)]
        public static void BuildAllFullWithProg()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            var winprog = new EditorWorkProgressShowerInConsole();
#if MOD_CAPSLUA
            winprog.Works.Add(CapsSptBuilder.BuildSptAsync(null, winprog));
#endif
            winprog.Works.Add(CapsResBuilder.BuildResAsync(null, winprog));
            winprog.Works.Add(CapsUpdateBuilder.BuildNearestUpdate(winprog));
            winprog.OnQuit += () => { CapsResBuilder.BuildingParams = null; };
            winprog.StartWork();
        }
    }
}
#endif