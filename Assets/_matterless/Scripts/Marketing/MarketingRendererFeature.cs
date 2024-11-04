using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matterless.Floorcraft
{
    public class MarketingRendererFeature : ScriptableRendererFeature
    {
        public enum ColorScheme
        {
            Arena,
            ArenaSecondary,
            Blasters,
            BlastersSecondary,
        }
        
        [SerializeField] private ColorScheme m_ColorScheme;
        [SerializeField] private Shader m_Shader;
        [SerializeField] private Texture m_BlueNoise;
        ClearPass m_ScriptablePass;
        
        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new ClearPass(m_ColorScheme, m_BlueNoise, m_Shader);

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.CompareTag("MainCamera") || renderingData.cameraData.isSceneViewCamera)
                renderer.EnqueuePass(m_ScriptablePass);
        }
        
        class ClearPass : ScriptableRenderPass
        {
            const string profilerTag = "Clear Background With Gradient";
            private Material m_ClearMaterial;
            private RenderTargetIdentifier m_CameraColorTarget;
            private Color32 m_PrimaryColor;
            private Color32 m_SecondaryColor;
            private static readonly int m_PrimaryColorProperty = Shader.PropertyToID("_Color");
            private static readonly int m_SecondaryColorProperty = Shader.PropertyToID("_Color2");
            private static readonly int m_BlueNoiseTextureProperty = Shader.PropertyToID("_BlueNoise");
            
            public ClearPass(ColorScheme colorScheme, Texture blueNoise, Shader shader)
            {
                switch (colorScheme)
                {
                    case ColorScheme.Arena:
                        m_PrimaryColor = new Color32(238, 75, 69, 255);
                        m_SecondaryColor = new Color32(44, 18, 67, 255);
                        break;
                    case ColorScheme.ArenaSecondary:
                        m_PrimaryColor = new Color32(232, 227, 238, 255);
                        m_SecondaryColor = new Color32(44, 18, 67, 255);
                        break;
                    case ColorScheme.Blasters:
                        m_PrimaryColor = new Color32(23, 10, 52, 255);
                        m_SecondaryColor = new Color32(73, 152, 202, 255);
                        break;
                    case ColorScheme.BlastersSecondary:
                        m_PrimaryColor = new Color32(232, 227, 238, 255);
                        m_SecondaryColor = new Color32(73, 152, 202, 255);
                        break;
                }

                m_ClearMaterial = new Material(shader);
                m_ClearMaterial.SetTexture(m_BlueNoiseTextureProperty, blueNoise);
            }
            
            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                m_CameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                m_ClearMaterial.SetColor(m_PrimaryColorProperty, m_PrimaryColor);
                m_ClearMaterial.SetColor(m_SecondaryColorProperty, m_SecondaryColor);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
                {
                    Blit(cmd, m_CameraColorTarget, m_CameraColorTarget, m_ClearMaterial);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }
        }
    }
}



