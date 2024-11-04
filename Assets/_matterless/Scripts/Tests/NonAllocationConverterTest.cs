using NUnit.Framework;
using System;
using Random = UnityEngine.Random;

namespace Matterless.Test
{
    public class NonAllocationConverterTest
    {
        
        [Test]
        public void Int16Converter()
        {
            var value = (short)Random.Range(short.MinValue, short.MaxValue);
            var data = new byte[2];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToInt16(data), value);
        }
        
        [Test]
        public void IntU16Converter()
        {
            var value = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
            var data = new byte[2];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToUInt16(data), value);
        }
        
        [Test]
        public void Int32Converter()
        {
            var value = Random.Range(int.MinValue, int.MaxValue);
            var data = new byte[4];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToInt32(data), value);
        }
        
        [Test]
        public void IntU32Converter()
        {
            var value = (uint)Random.Range(int.MinValue, int.MaxValue);
            var data = new byte[4];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToUInt32(data), value);
        }
        
        [Test]
        public void Int64Converter()
        {
            var value = (long)Random.Range(int.MinValue, int.MaxValue);
            var data = new byte[8];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToInt64(data), value);
        }
        
        [Test]
        public void IntU64Converter()
        {
            var value = (ulong)Random.Range(int.MinValue, int.MaxValue);
            var data = new byte[8];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToUInt64(data), value);
        }

        
        [Test]
        public void SingleConverter()
        {
            var value = Random.Range(float.MinValue, float.MaxValue);
            var data = new byte[4];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToSingle(data), value);
        }
        
        [Test]
        public void DoubleConverter()
        {
            var value = (double)Random.Range(float.MinValue, float.MaxValue);
            var data = new byte[8];
            
            NonAllocationConverter.GetBytes(value, ref data);
            CollectionAssert.AreEqual(data,BitConverter.GetBytes(value));
            Assert.AreEqual(NonAllocationConverter.ToDouble(data), value);
        }
    }
}
