using System.Collections;
using System.Collections.Generic;
using Matterless.Floorcraft;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NameTagService
    {
        private readonly SpeederService m_SpeederService;
        private readonly NameComponentService m_NameComponentService;
        private readonly NameComponentService.Settings m_Setting;
        private CrownService m_CrownService;
        private IAukiWrapper m_AukiWrapper;

        private ScoreComponentService m_ScoreComponentService;
        private Dictionary<uint, NameTagView> m_NameTags = new Dictionary<uint, NameTagView>();
        public NameTagService(
            IAukiWrapper aukiWrapper,
            SpeederService speederService,
            ScoreComponentService scoreComponentService,
            NameComponentService nameComponentService,
            CrownService crownService,
            NameComponentService.Settings setting)
        {
            m_AukiWrapper = aukiWrapper;
            m_CrownService = crownService;
            m_ScoreComponentService = scoreComponentService;
            m_NameComponentService= nameComponentService;
            m_SpeederService = speederService;
            m_Setting = setting;
            m_NameComponentService.onComponentAdded += OnNameComponentAdded;
            m_ScoreComponentService.onComponentDeleted += OnScoreComponentDelete;
            m_ScoreComponentService.onComponentAdded += OnScoreComponentAdded;
            m_ScoreComponentService.onComponentUpdated += UpdateText;
            m_AukiWrapper.onLeft += Reset;
            m_SpeederService.onSpeederViewAdded += OnSpeederViewAdded;
        }
        
        private void OnScoreComponentAdded(ScoreComponentModel model)
        {
            // Try create name tag when score arrives (view/name may have arrived first for remote joiner).
            CreateNameTagForEntity(model.entityId);
        }

        private void OnNameComponentAdded(NameComponentModel model)
        {
            // Speeder view may not exist yet for remote joiners (name can sync before transform). Create tag only when view exists; otherwise OnSpeederViewAdded will create it.
            if (!m_SpeederService.speederViews.ContainsKey(model.entityId))
            {
                return;
            }
            CreateNameTagForEntity(model.entityId);
        }

        private void OnSpeederViewAdded(uint entityId)
        {
            // View appeared (e.g. remote joiner's vehicle). Create name tag if name component exists and we don't have a tag yet.
            if (m_NameTags.ContainsKey(entityId))
            {
                return;
            }
            if (!m_NameComponentService.nameComponentModels.TryGetValue(entityId, out var nameModel))
            {
                return;
            }
            CreateNameTagForEntity(entityId);
        }

        private void CreateNameTagForEntity(uint entityId)
        {
            if (!m_SpeederService.speederViews.TryGetValue(entityId, out var speederTransform))
            {
                return;
            }
            if (!m_NameComponentService.nameComponentModels.TryGetValue(entityId, out var nameModel))
            {
                return;
            }
            if (!m_ScoreComponentService.scoreComponentModels.ContainsKey(entityId))
            {
                return; // UpdateText needs score; avoid creating until we have it
            }
            if (m_NameTags.ContainsKey(entityId))
            {
                return;
            }
            var model = nameModel;
            NameTagView go = GameObject.Instantiate(Resources.Load<NameTagView>(m_Setting.nameTagResourcePath),
                speederTransform.transform);
            go.SetName(m_Setting.nameTags[model.model.name]);
            go.SetEntityId(entityId);
            go.SetFontSize(m_Setting.fontSize);
            go.transform.localPosition = m_Setting.nameTagOffset;
            m_NameTags.Add(entityId, go);
            UpdateText(null);
        }
        private void OnScoreComponentDelete(uint entityId, bool isMine)
        {
            m_NameComponentService.DeleteComponent(entityId);
            m_NameTags.Remove(entityId);
        }

        private void UpdateText(ScoreComponentModel model)
        {
            foreach (var nameTag in m_NameTags)
            {
                string nameText = string.Empty;
                if (nameTag.Value.entityId == m_CrownService.crownKeeper)
                {
                    nameText += "<sprite name=\"Crown_3D\">\n";
                }

                int score = m_ScoreComponentService.scoreComponentModels[nameTag.Value.entityId].model.score;
                nameText += "<size=130%><cspace=-0.02em>";
                nameText += score < 10 ? score.ToString("00") : score;
                nameText +="\n<size=100%><cspace=-0.01em>";
                nameText += m_Setting.nameTags[m_NameComponentService.nameComponentModels[nameTag.Value.entityId].model.name];
                nameTag.Value.SetName(nameText);
            }
        }
        private void Reset()
        {
            m_NameTags.Clear();
        }
    }
}