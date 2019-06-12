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
        [MenuItem("Res/Build All (Quick)", priority = 200103)]
        public static void BuildAllQuick()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            CapsResBuilder.BuildingParams.makezip = false;
            var work = CapsSptBuilder.BuildSptAsync(null, null, new[] { new CapsSptBuilder.SptBuilderEx_RawCopy() });
            while (work.MoveNext()) ;
            work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
        }
        [MenuItem("Res/Build All (Full)", priority = 200104)]
        public static void BuildAllFull()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            var work = CapsSptBuilder.BuildSptAsync(null, null);
            while (work.MoveNext()) ;
            work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
            CapsUpdateBuilder.BuildNearestUpdate();
        }
        [MenuItem("Res/Build All (Full, With Progress Window)", priority = 200105)]
        public static void BuildAllFullWithProg()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            var winprog = new EditorWorkProgressShowerInConsole();
            winprog.Works.Add(CapsSptBuilder.BuildSptAsync(null, winprog));
            winprog.Works.Add(CapsResBuilder.BuildResAsync(null, winprog));
            winprog.Works.Add(CapsUpdateBuilder.BuildNearestUpdate(winprog));
            winprog.OnQuit += () => { CapsResBuilder.BuildingParams = null; };
            winprog.StartWork();
        }
    }
}