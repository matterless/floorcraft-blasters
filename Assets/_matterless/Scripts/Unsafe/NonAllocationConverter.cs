namespace Matterless
{
    public static class NonAllocationConverter
    {
        //short
        public static unsafe void GetBytes(short value, ref byte[] output, int index = 0)
        {
            fixed (byte* b = &output[index])
                *((short*) b) = value;
        }

        // int
        public static unsafe void GetBytes(int value, ref byte[] output, int index = 0)
        {
            fixed (byte* b = &output[index])
                *((int*) b) = value;
        }

        // long
        public static unsafe void GetBytes(long value, ref byte[] output, int index = 0)
        {
            fixed (byte* b = &output[index])
                *((long*) b) = value;
        }

        // ushort
        public static unsafe void GetBytes(ushort value, ref byte[] output, int index = 0)
        {
            GetBytes((short) value, ref output, index);
        }

        // uint
        public static unsafe void GetBytes(uint value, ref byte[] output, int index = 0)
        {
            GetBytes((int) value, ref output, index);
        }

        // ulong
        public static unsafe void GetBytes(ulong value, ref byte[] output, int index = 0)
        {
            GetBytes((long) value, ref output, index);
        }

        // float
        public static unsafe void GetBytes(float value, ref byte[] output, int index = 0)
        {
            GetBytes(*(int*) &value, ref output, index);
        }

        // double
        public static unsafe void GetBytes(double value, ref byte[] output, int index = 0)
        {
            GetBytes(*(long*) &value, ref output, index);
        }

        // short
        public static unsafe short ToInt16(byte[] data, int startIndex = 0)
        {
            fixed (byte* ptr = &data[startIndex])
            {
                return *((short*) ptr);
            }
        }

        // int
        public static unsafe int ToInt32(byte[] data, int startIndex = 0)
        {
            fixed (byte* ptr = &data[startIndex])
            {
                return *((int*) ptr);
            }
        }

        // long
        public static unsafe long ToInt64(byte[] data, int startIndex = 0)
        {
            fixed (byte* ptr = &data[startIndex])
            {
                return *((long*) ptr);
            }
        }

        // ushort
        public static unsafe ushort ToUInt16(byte[] data, int startIndex = 0)
        {
            return (ushort) ToInt16(data, startIndex);
        }

        // ushort
        public static unsafe uint ToUInt32(byte[] data, int startIndex = 0)
        {
            return (uint) ToInt32(data, startIndex);
        }

        // ulong
        public static unsafe ulong ToUInt64(byte[] data, int startIndex = 0)
        {
            return (ulong) ToInt64(data, startIndex);
        }

        // float
        public static unsafe float ToSingle(byte[] data, int startIndex = 0)
        {
            var val = ToInt32(data, startIndex);
            return *(float*) &val;
        }

        // float
        public static unsafe double ToDouble(byte[] data, int startIndex = 0)
        {
            var val = ToInt64(data, startIndex);
            return *(double*) &val;
        }
    }
}