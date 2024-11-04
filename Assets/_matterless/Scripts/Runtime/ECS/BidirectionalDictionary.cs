using System;
using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public class BidirectionalDictionary<U,V>
    {
        private readonly Dictionary<U, V> m_DictionaryUV = new();
        private readonly Dictionary<V, U> m_DictionaryVU = new();

        public int count => m_DictionaryUV.Count;
        public Dictionary<U, V>.KeyCollection firstKeys => m_DictionaryUV.Keys;
        public Dictionary<V, U>.KeyCollection secondKeys => m_DictionaryVU.Keys;

        public void Add(U u, V v)
        {
            // we need to add the replace key logic as for some reason we were unable to 
            // be sure that the dictionary are clear before we use them again

            if (m_DictionaryUV.ContainsKey(u))
            {
                UnityEngine.Debug.LogWarning($"Key {u} already exists. The value will be replaced: {m_DictionaryUV[u]}->{v}");
                m_DictionaryUV[u] = v;
            }
            else
            {
                m_DictionaryUV.Add(u, v);
            }

            if (m_DictionaryVU.ContainsKey(v))
            {
                UnityEngine.Debug.LogWarning($"Key {v} already exists. The value will be replaced: {m_DictionaryVU[v]}->{u}");
                m_DictionaryVU[v] = u;
            }
            else
            {
                m_DictionaryVU.Add(v, u);
            }
        }

        public void Remove(U u)
        {
            m_DictionaryVU.Remove(m_DictionaryUV[u]);
            m_DictionaryUV.Remove(u);
        }

        public void Remove(V v)
        {
            m_DictionaryUV.Remove(m_DictionaryVU[v]);
            m_DictionaryVU.Remove(v);
        }

        public void Clear()
        {
            UnityEngine.Debug.Log("BidirectionalDictionary::Clear()");
            m_DictionaryUV.Clear();
            m_DictionaryVU.Clear();
        }

        public U GetItem(V v) => m_DictionaryVU[v];
        public V GetItem(U u) => m_DictionaryUV[u];
        public bool HasItem(V v) => m_DictionaryVU.ContainsKey(v);
        public bool HasItem(U u) => m_DictionaryUV.ContainsKey(u);


    }
}