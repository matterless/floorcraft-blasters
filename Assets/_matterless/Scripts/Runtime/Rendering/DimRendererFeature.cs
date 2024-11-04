using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matterless.Floorcraft
{
    public class DimRendererFeature : ScriptableRendererFeature
    {
        private float m_DimmAmmount;
        [SerializeField] private Material m_Material;
        private DimmPass m_ScriptablePass;

        public float DimAmmount
        {
            set
            {
                m_DimmAmmount = value;
            }
            
        }

        public override void Create()
        {
            m_ScriptablePass = new DimmPass(m_Material);
            m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null)
                return;
            
            if (!renderingData.cameraData.camera.CompareTag("MainCamera"))
                return;
            
            m_ScriptablePass.dimmAmmount = m_DimmAmmount;
            renderer.EnqueuePass(m_ScriptablePass);
        }
        
        public class DimmPass : ScriptableRenderPass
        {
            private Material m_MaterialAsset;
            
            private const string profilerTag = "Dimm Backround Pass";
            private RenderTargetIdentifier m_CameraColorTarget, m_TemporaryColorTarget;
            private int m_TemporaryBufferID = Shader.PropertyToID("_TemporaryColorTarget");
            private Material m_ClearMaterial;
            public float dimmAmmount;
            
            private readonly int m_DimmAmmountProperty = Shader.PropertyToID("_DimmAmmount");
            public DimmPass(Material material)
            {
                m_MaterialAsset = material;
                m_ClearMaterial = Instantiate(material);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (m_ClearMaterial == null)
                {
                    Debug.LogWarning("DimRendererFeature.DimmPass: We lost the reference. Reinstantiating material");
                    m_ClearMaterial = Instantiate(m_MaterialAsset);
                }
                
                m_CameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(m_TemporaryBufferID, descriptor, FilterMode.Bilinear);
                m_TemporaryColorTarget = new RenderTargetIdentifier(m_TemporaryBufferID);
                m_ClearMaterial.SetFloat(m_DimmAmmountProperty, dimmAmmount);
                
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
                {
                    Blit(cmd, m_CameraColorTarget, m_TemporaryColorTarget, m_ClearMaterial);
                    Blit(cmd, m_TemporaryColorTarget, m_CameraColorTarget);
                }
                
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(m_TemporaryBufferID);
            }
        }
    }    
}

