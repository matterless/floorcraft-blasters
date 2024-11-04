using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Matterless
{
    public class _MemoryAllocationTest_ : MonoBehaviour
    {
        private int m_IValue = 123;
        private int m_I2Value = Int32.MaxValue;
        
        private float m_FValue = 123.234f;
        private float m_F2Value = 345.765f;
        
        private byte[] m_Data = new byte[4];
        private byte[] m_FullData = new byte[4 + 4];

        // Update is called once per frame
        void Update()
        {
            Test();
        }

        private void SetupTest()
        {
            m_IValue = Random.Range(int.MinValue, int.MaxValue);
            m_I2Value = Random.Range(int.MinValue, int.MaxValue);
            m_FValue = Random.Range(float.MinValue, float.MaxValue);
            m_F2Value = Random.Range(float.MinValue, float.MaxValue);
            m_Data = new byte[4];
            m_FullData = new byte[4 + 4];
        }

        [ContextMenu("Test")]
        private void Test()
        {
            SetupTest();

            NonAllocationConverter.GetBytes(m_FValue, ref m_Data);
            DebugLog(m_Data, 0);
            m_Data = BitConverter.GetBytes(m_FValue);
            DebugLog(m_Data, 0);
            
            NonAllocationConverter.GetBytes(m_F2Value, ref m_Data);
            DebugLog(m_Data, 0);
            m_Data = BitConverter.GetBytes(m_F2Value);
            DebugLog(m_Data, 0);
            
            NonAllocationConverter.GetBytes(m_FValue, ref m_FullData, 0);
            NonAllocationConverter.GetBytes(m_F2Value, ref m_FullData, 4);
            DebugLog(m_FullData, 0);
            DebugLog(m_FullData, 4);

            Debug.Log($"{m_FValue} == {NonAllocationConverter.ToSingle(m_FullData, 0)}");
            Debug.Log($"{m_F2Value} == {NonAllocationConverter.ToSingle(m_FullData, 4)}");
            
            // integers
            NonAllocationConverter.GetBytes(m_IValue, ref m_FullData, 0);
            NonAllocationConverter.GetBytes(m_I2Value, ref m_FullData, 4);
            DebugLog(m_FullData, 0);
            DebugLog(m_FullData, 4);

            Debug.Log($"{m_IValue} == {NonAllocationConverter.ToInt32(m_FullData, 0)}");
            Debug.Log($"{m_I2Value} == {NonAllocationConverter.ToInt32(m_FullData, 4)}");
        }

        private void DebugLog(byte[] data, int index)
        {
            Debug.Log($"{data[index]}-{data[index+1]}-{data[index+2]}-{data[index+3]}");
        }
    }
}