using UnityEngine;

namespace Matterless.Floorcraft
{
    public class MaterialDebugService
    {
        private Material m_Material;
        private MaterialProperties m_MaterialProperties;
        private static readonly int DiffuseMatcapsArrayIndex = Shader.PropertyToID("_DiffuseMatcapIndex");
        private static readonly int SpecularMatcapsArrayIndex = Shader.PropertyToID("_SpecularMatcapIndex"); 
        private static readonly int DiffuseMatcapStrength = Shader.PropertyToID("_DiffuseMatcapStrength");
        private static readonly int SpecularMatcapStrength = Shader.PropertyToID("_SpecularMatcapStrength");
        private static readonly int DetailNormalMapArrayIndex = Shader.PropertyToID("_DetailNormalMapIndex");
        private static readonly int DetialNormalMapStrength = Shader.PropertyToID("_DetailNormalMapStrength");

        public MaterialDebugService()
        {
        }
        
        
        private bool GetMaterial()
        {
            // TODO: (Marko) fix this once new vehicle is in
            // if(m_Material == null)
            //     m_Material = GameObject.FindObjectOfType<SpeederView>().material;
            //
            // return m_Material != null;
            return false;   
        }

        public void SetUseDiffuseMatcap(bool on)
        {
            if (!GetMaterial())
                return;

            if (on)
            {
                m_Material.EnableKeyword("USE_DIFFUSE_MATCAP");
                return;
            }
            
            m_Material.DisableKeyword("USE_DIFFUSE_MATCAP");
        }

        public void SetUseSpecularMatcap(bool on)
        {
            if (!GetMaterial())
                return;

            if (on)
            {
                m_Material.EnableKeyword("USE_SPECULAR_MATCAP");
                return;
            }
            
            m_Material.DisableKeyword("USE_SPECULAR_MATCAP");
        }

        public void SetDiffuseMatcapArrayIndex(int i)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetInteger(DiffuseMatcapsArrayIndex, i);
        }
        
        public void SetSpecularMatcapArrayIndex(int i)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetInteger(SpecularMatcapsArrayIndex, i);
        }

        public void SetDiffuseMatcapStrength(float value)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetFloat(DiffuseMatcapStrength, value);
        }
        
        public void SetSpecularMatcapStrength(float value)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetFloat(SpecularMatcapStrength, value);
        }
    
        public void SetDetailNormalMapArrayIndex(int i)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetInteger(DetailNormalMapArrayIndex, i);
        }
        
        public void SetDetailNormalMapStrength(float value)
        {
            if (!GetMaterial())
                return;
            
            m_Material.SetFloat(DetialNormalMapStrength, value);
        }
        
        public class MaterialProperties
        {
            public bool UseDiffuseMatcap;
            public bool UseSpecularMatcap;
            public int DiffuseMatcapArrayIndex;
            public int SpecularMatcapArrayIndex;
            public float DiffuseMatcapStrength;
            public float SpecularMatcapStrength;
            public int DetailNormalMapArrayIndex;
            public float DetailNormalMapStrength;
        }

        public MaterialProperties GetMaterialProperties()
        {
            if (!GetMaterial())
                return null;
            
            if (m_MaterialProperties == null)
                m_MaterialProperties = new MaterialProperties();

            m_MaterialProperties.UseDiffuseMatcap = m_Material.IsKeywordEnabled("USE_DIFFUSE_MATCAP");
            m_MaterialProperties.UseSpecularMatcap = m_Material.IsKeywordEnabled("USE_SPECULAR_MATCAP");
            m_MaterialProperties.DiffuseMatcapArrayIndex = m_Material.GetInteger(DiffuseMatcapsArrayIndex);
            m_MaterialProperties.SpecularMatcapArrayIndex = m_Material.GetInteger(SpecularMatcapsArrayIndex);
            m_MaterialProperties.DiffuseMatcapStrength = m_Material.GetFloat(DiffuseMatcapStrength);
            m_MaterialProperties.SpecularMatcapStrength = m_Material.GetFloat(SpecularMatcapStrength);
            m_MaterialProperties.DetailNormalMapArrayIndex = m_Material.GetInteger(DetailNormalMapArrayIndex);
            m_MaterialProperties.DetailNormalMapStrength = m_Material.GetFloat(DetialNormalMapStrength);
            
            return m_MaterialProperties;
        }
    }
}
    


