using System.Collections.Generic;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ObstaclesUiService
    {
        private readonly ObstaclesUiView m_View;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly ILocalisationService m_LocalisationService;
        private readonly ObstacleService m_obstacleService;
        
        private bool m_CanSpawn;
        private bool m_ButtonToggle;
        
        public string RemainingString => m_LocalisationService.Translate(m_obstacleService.selectedPlaceable.placeLocalisationTag);
        public string ButtonLabel => $"{RemainingString} ({m_obstacleService.RemainingObstacles}/{m_obstacleService.maxObstacles})";

        
        private int m_PlaneCount;
        private int m_PylonCounter;
        public bool CanSpawnObstacles => m_obstacleService.RemainingObstacles > 0;
        public ObstaclesUiService( 
            IAukiWrapper aukiWrapper,
            ILocalisationService localisationService,
            ObstacleService obstacleService)
        {
            m_AukiWrapper = aukiWrapper;
            m_View = ObstaclesUiView.Create();
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            localisationService.onLanguageChanged += OnLanguageChanged;
            m_LocalisationService = localisationService;
            m_obstacleService = obstacleService;
        }

        private void OnLanguageChanged()
        {
            UpdateRemainingLabel();
        }

        private void UpdateRemainingLabel()
        {
            string removeButtonLabel = m_PylonCounter<2 ? m_LocalisationService.Translate(m_obstacleService.selectedPlaceable.removeLocalisationTag) :
                m_LocalisationService.Translate(m_obstacleService.selectedPlaceable.removesLocalisationTag);
            
            int remainingObstacles = m_obstacleService.RemainingObstacles;
            string remaniningString = m_LocalisationService.Translate(m_obstacleService.selectedPlaceable.placeLocalisationTag);
            string buttonLabel = $"{remaniningString} ({remainingObstacles}/{m_obstacleService.maxObstacles})";
            
            bool isSpawnObstacleButtonInteractable = remainingObstacles > 0;
            bool isResetSpawnObstacleButtonInteractable = m_obstacleService.RemainingObstacles > 0;
            m_View.UpdateUi(buttonLabel, isSpawnObstacleButtonInteractable, isResetSpawnObstacleButtonInteractable,  removeButtonLabel);
        }

        // What is this?
        public void SetInSpawnScreen(bool inSpawnScreen)
        {
        }
    } 
}

