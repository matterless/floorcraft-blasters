using System;

namespace Matterless.Floorcraft
{
    public static class Converters
    {
        private const int SIZE_OF_FLOAT = sizeof(float); // it should be 4
        private const int SIZE_OF_INT = sizeof(int); 
        private const int SIZE_OF_LONG = sizeof(long); 

        public static void ByteToFloat(byte[] array, ref float[] output)
        {
            for (int i = 0; i < output.Length; i++)
            {
                // TODO:: we need to add this per float NOT for the whole float array
                // and also do the same on Float To Byte convertion

                //if (BitConverter.IsLittleEndian) // cpu architecture
                //    Array.Reverse(array, i * SIZE_OF_FLOAT, SIZE_OF_FLOAT);

                output[i] = BitConverter.ToSingle(array, i * SIZE_OF_FLOAT);
            }
        }

        public static void ByteToLong(byte[] array, ref long[] output)
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = BitConverter.ToInt64(array, i * SIZE_OF_LONG);
            }
        }

        public static void ByteToInt(byte[] array, ref int[] output)
        {
            for (int i = 0; i < output.Length; i++)
            {
                // TODO:: we need to add this per float NOT for the whole float array
                // and also do the same on Float To Byte convertion

                //if (BitConverter.IsLittleEndian) // cpu architecture
                //    Array.Reverse(array, i * SIZE_OF_FLOAT, SIZE_OF_FLOAT);

                output[i] = BitConverter.ToInt32(array, i * SIZE_OF_INT);
            }
        }

        public static void FloatToBytes(float value, ref byte[] output)
        {
            output = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(output);
        }

        //public static void ByteToFloat()
        //{

        //}

        
    }
}