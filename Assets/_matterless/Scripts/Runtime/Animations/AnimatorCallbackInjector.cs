using System;
using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class  AnimatorCallbackInjector : StateMachineBehaviour
    {
        private Dictionary<int, Action> m_OnEnterCallbacks = new Dictionary<int, Action>();
        private Dictionary<int, Action> m_OnExitCallbacks = new Dictionary<int, Action>();

        public void RegisterOnEnterCallback(string stateName, Action callback)
        {
            m_OnEnterCallbacks.Add(Animator.StringToHash(stateName), callback);
        }

        public void RegisterOnExitCallback(string stateName, Action callback)
        {
            m_OnExitCallbacks.Add(Animator.StringToHash(stateName), callback);
        }

        public void UnregisterAll()
        {
            m_OnEnterCallbacks.Clear();
            m_OnExitCallbacks.Clear();
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
            => InvokeCallbacks(m_OnEnterCallbacks, stateInfo.shortNameHash);

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
            => InvokeCallbacks(m_OnExitCallbacks, stateInfo.shortNameHash);

        private void InvokeCallbacks(Dictionary<int,Action> callbacks, int hash)
        {
            if (callbacks.ContainsKey(hash))
                callbacks[hash]?.Invoke();
        }
    }
}