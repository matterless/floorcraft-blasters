using System.Text;

namespace Matterless.Rest
{
    public static class Extensions
    {
        /// <summary>
        /// Encode a UTF8 string to a bytes array.
        /// </summary>
        /// <returns>The bytes array of a UTF8 string.</returns>
        /// <param name="str">String.</param>
        public static byte[] GetBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// Encode a bytes array to UTF8 string.
        /// </summary>
        /// <returns>The string of a bytes array in UTF8 encoding.</returns>
        /// <param name="bytes">Bytes.</param>
        public static string GetString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }


    }
}
