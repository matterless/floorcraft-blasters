using UnityEditor;
using UnityEngine;


namespace Matterless.Floorcraft.Editor
{
    [CustomEditor(typeof(SkewedImage))]
    public class SkewedImageEditor : UnityEditor.UI.ImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var skewedImage = target as SkewedImage;
            
            EditorGUI.BeginChangeCheck();
            skewedImage.skewX = EditorGUILayout.FloatField("Skew X", skewedImage.skewX);
            skewedImage.skewY = EditorGUILayout.FloatField("Skew Y", skewedImage.skewY);
            skewedImage.mirrorX = EditorGUILayout.Toggle("Mirror X", skewedImage.mirrorX);
            
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(skewedImage);
                
                skewedImage.SetVerticesDirty();
            }
        }
    }
}

