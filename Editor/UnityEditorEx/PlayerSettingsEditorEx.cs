using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{ // TODO: move this to independ package, if we want to use this in multiple packages.
    [CustomEditor(typeof(PlayerSettings))]
    internal class PlayerSettingsEditorEx : PlayerSettingsEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                PlayerSettingsEditorUtils.TrigPlayerSettingsChanged();
            }
        }
    }

    public static class PlayerSettingsEditorUtils
    {
        public static event System.Action OnPlayerSettingsChanged = () => { };
        internal static void TrigPlayerSettingsChanged() { OnPlayerSettingsChanged(); }
    }
}