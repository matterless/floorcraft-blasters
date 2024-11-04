using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matterless.Floorcraft.Editor
{
    [InitializeOnLoadAttribute]
    public static class MarketingBackgroundSwitcher
    {
        static MarketingBackgroundSwitcher()
        {
            EditorApplication.playModeStateChanged += SwitchRenderer;
        }

        private static void SwitchRenderer(PlayModeStateChange state)
        {
            var pipeline = (UniversalRenderPipelineAsset) GraphicsSettings.renderPipelineAsset;
            FieldInfo propertyInfo = pipeline.GetType()
                .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            ScriptableRendererData scriptableRendererData =
                ((ScriptableRendererData[]) propertyInfo?.GetValue(pipeline))?[0];

            ScriptableRendererFeature marketingRendererFeature = null;
            foreach (ScriptableRendererFeature feature in scriptableRendererData.rendererFeatures)
            {
                if (feature.name == "MarketingRendererFeature")
                    marketingRendererFeature = feature;
            }

            if (marketingRendererFeature == null)
                return;

            marketingRendererFeature.SetActive(state == PlayModeStateChange.EnteredPlayMode);
        }
    }
}