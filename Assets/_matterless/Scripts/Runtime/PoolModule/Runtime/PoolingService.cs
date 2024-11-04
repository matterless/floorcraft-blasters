using System;
using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public class PoolingService : IPoolingService
    {
        private Dictionary<Type, Stack<IPoolable>> m_Pool;
        private Dictionary<Type, int> m_Limits;

        public PoolingService()
        {
            m_Pool = new Dictionary<Type, Stack<IPoolable>>();
            m_Limits = new Dictionary<Type, int>();
        }

        public void SetLimit<T>(int limit)
        {
            var type = typeof(T);

            if (m_Limits.ContainsKey(type))
                throw new Exception($"PoolingService: You can set a limit only once. {type}");

            m_Limits.Add(type, limit);
        }

        public void Push<T>(IPoolable poolable, Action onLimitExcited)
        {
            var type = typeof(T);
            poolable.OnPush();

            if (!m_Pool.ContainsKey(type))
                m_Pool.Add(type, new Stack<IPoolable>());

            if (m_Limits.ContainsKey(type) &&  m_Pool[type].Count == m_Limits[type])
            {
                onLimitExcited();
                return;
            }
            
            m_Pool[type].Push(poolable);
        }

        public T Pop<T>(Func<T> onNothingToPull) where T : class, IPoolable
        {
            var type = typeof(T);

            if (!m_Pool.ContainsKey(type) || m_Pool[type].Count == 0)
            {
                return onNothingToPull();
            }

            return (T)m_Pool[type].Pop();
        }
    }
}