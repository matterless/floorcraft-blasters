using System;
using Matterless.Localisation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class UIView<T> : MonoBehaviour where T : UIView<T>
    {
        [SerializeField] private GameObject m_Panel;

        #region Factory
        public static T Create(string path) => Instantiate(Resources.Load<T>(path));
        #endregion

        /// <summary>
        /// Do not call base.Init() if you want to get a reference from the class.
        /// </summary>
        /// <returns>T type of class</returns>
        public virtual T Init()
        {
            return default;
        }
        
        public virtual T Init(ILocalisationService localisationService)
        {
            return default;
        }

        /// <summary>
        /// Always call base.Show() while overriding
        /// </summary>
        public virtual void Show()
        {
            // we need this to avoid null reference error on app quit
            if (m_Panel == null)
                return;

            m_Panel.SetActive(true);
        }

        /// <summary>
        /// Always call base.Hide() while overriding
        /// </summary>
        public virtual void Hide()
        {
            // we need this to avoid null reference error on app quit
            if (m_Panel == null)
                return;

            m_Panel.SetActive(false);
        }

        public bool IsVisible()
        {
            return m_Panel.activeSelf;
        }
    }
}