using Matterless.Localisation;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class ChargingUi : MonoBehaviour
    {
        private enum State
        {
            Charged,
            Charging
        }

        [SerializeField] private GameObject m_Glow;
        [SerializeField] private TMPro.TextMeshProUGUI m_Label;

        private ILocalisationService m_LocalisationService;
        private string m_ChargedLabelTag;
        private string m_ChargingLabelTag;
        private State m_State;
        private int m_FillPropertyId;

        public void Init(ILocalisationService localisationService, string chargedLabelTag, string chargingLabelTag)
        {
            m_LocalisationService = localisationService;
            m_ChargedLabelTag = chargedLabelTag;
            m_ChargingLabelTag = chargingLabelTag;
            m_LocalisationService.onLanguageChanged += UpdateLabel;
            m_State = State.Charged;
            m_FillPropertyId = Shader.PropertyToID("_Fill");
        }

        private void UpdateLabel()
        {
            switch(m_State)
            {
                case State.Charging:
                    m_Label.text = m_LocalisationService.Translate(m_ChargingLabelTag);
                    m_Label.color = Color.white;
                    break;
                case State.Charged:
                    m_Label.text = m_LocalisationService.Translate(m_ChargedLabelTag);
                    m_Label.color = new Color(0.09f, 0.04f, 0.2f);
                    break;
            }
        }

        public void UpdateUi(float fill)
        {
            if(fill >= 1)
            {
                m_State = State.Charged;
                m_Glow.SetActive(true);
                UpdateLabel();
            }
            else 
            {
                m_State = State.Charging;
                m_Glow.SetActive(false);
                UpdateLabel();
            }
            
            // NOTE: (Marko) Changed this to global shader id cause mask will prevent the material from updating
            //m_ChargingFillMaterial.SetFloat(m_FillPropertyId, Mathf.Clamp01(fill));
            Shader.SetGlobalFloat(m_FillPropertyId, Mathf.Clamp01(fill));
        }
        public void SetLocalisationTags(string charged, string charging)
        {
            m_ChargedLabelTag = charged;
            m_ChargingLabelTag = charging;
        }
    }
}