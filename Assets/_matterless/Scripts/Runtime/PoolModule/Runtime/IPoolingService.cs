using System;

namespace Matterless.Floorcraft
{
    public interface IPoolingService
    {
        T Pop<T>(Func<T> onNothingToPull) where T : class, IPoolable;
        void Push<T>(IPoolable poolable, Action onLimitExcited);
        void SetLimit<T>(int limit);
    }
}