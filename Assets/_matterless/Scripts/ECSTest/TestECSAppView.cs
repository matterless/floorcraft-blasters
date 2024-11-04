using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft.TestECS
{
    public class TestECSAppView : MonoBehaviour
    {
        [SerializeField] Transform m_Container;
        [SerializeField] TestECSColorEntry m_EntryPrefab;
        [SerializeField] Text m_HeaderText;

        Dictionary<uint, TestECSColorEntry> m_Entries = new();
        List<uint> m_EntriesOrder = new();

        private void Awake()
        {
            m_EntryPrefab.Hide();
        }

        public void UpdateHeaderText(string text) => m_HeaderText.text = text;

        public void CreateEntry(TestECSTestComponentModel model)
        {
            var entry = Instantiate(m_EntryPrefab, m_Container)
                .Init(model.entityId, model.isMine, model.color);
            m_Entries.Add(model.entityId, entry);
            m_EntriesOrder.Add(model.entityId);
            OrderEntries();
        }

        private void OrderEntries()
        {
            m_EntriesOrder.Sort();

            foreach (var id in m_EntriesOrder)
                if (m_Entries[id] != null)
                    m_Entries[id].transform.SetAsLastSibling();
        }
        

        public void DeleteEntry(uint id)
        {
            GameObject.Destroy(m_Entries[id].gameObject);
            m_Entries.Remove(id);
            m_EntriesOrder.Remove(id);
        }

        public void UpdateEntry(TestECSTestComponentModel model)
        {
            m_Entries[model.entityId].UpdateColor(model.color);
        }
    }
}