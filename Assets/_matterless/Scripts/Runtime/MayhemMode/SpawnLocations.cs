using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Matterless.Floorcraft
{
    public class SpawnLocations
    {
        private static int[] s_SpawnPointIndexes = new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
        };
        
        private readonly System.Random m_Random = new System.Random();
        private bool[] m_SpawnPointsBoolArray = new bool[16];
        public byte[] encodedPoints { get; private set; }
        public int[] spawnPoints { get; private set; }


        private void Shuffle<T> (Random rng, T[] array)
        {
            var n = array.Length;
            while (n > 1) 
            {
                var k = rng.Next(n--);
                // Swap
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
        
        public int[] GenerateSpawnPoints(int spawnPointCount)
        {
            Shuffle(m_Random, s_SpawnPointIndexes);
            spawnPoints = new int[spawnPointCount];
            Array.Copy(s_SpawnPointIndexes, spawnPoints, spawnPointCount);
            Encode();
            return spawnPoints;
        }

        private void Encode()
        {
            for (var i = 0; i < m_SpawnPointsBoolArray.Length; i++)
                m_SpawnPointsBoolArray[i] = false;

            foreach (var index in spawnPoints)
                m_SpawnPointsBoolArray[index] = true;
            
            //for (var j = 0; j < m_SpawnPointsBoolArray.Length; j++)
            //    Debug.Log(m_SpawnPointsBoolArray[j]);
            
            encodedPoints = EncodeInternal(m_SpawnPointsBoolArray);
        }
        
        public int[] Decode(byte[] data)
        {
            m_SpawnPointsBoolArray = DecodeInternal(data);
            var points = new List<int>();

            for (int i = 0; i < m_SpawnPointsBoolArray.Length; i++)
            {
                if(m_SpawnPointsBoolArray[i])
                    points.Add(i);
            }
            
            spawnPoints = points.ToArray();
            return spawnPoints;
        }
        
        private byte[] EncodeInternal(bool[] data)
        {
            var result = new byte[2];
            result[0] = ConvertBoolArrayToByte(data[0..8]);
            result[1] = ConvertBoolArrayToByte(data[8..16]);
            return result;
        }
        
        private bool[] DecodeInternal(byte[] data)
        {
            var result = new bool[16];
            ConvertByteToBoolArray(data[0]).CopyTo(result, 0);
            ConvertByteToBoolArray(data[1]).CopyTo(result, 8);
            return result;
        }
        
        private byte ConvertBoolArrayToByte(bool[] data)
        {
            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - data.Length;
            
            foreach (bool bit in data)
            {
                // if the element is 'true' set the bit at that position
                if (bit)
                    result |= (byte)(1 << (7 - index));

                index++;
            }

            return result;
        }
        
        private bool[] ConvertByteToBoolArray(byte data)
        {
            // prepare the return result
            bool[] result = new bool[8];

            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (data & (1 << i)) != 0;

            // reverse the array
            Array.Reverse(result);

            return result;
        }
        
    }
}