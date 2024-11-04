namespace Matterless.Rest
{
    public enum HttpContentType
    {
        JSON = 0,
        BINARY,
        XML,
        MP4,
        OGG,
        BITMAP,
        PNG,
        TIFF,
        CSV,
        HTML,
        TEXT,
        FORM
    }

    internal static class HttpContentTypeStrings
    {
        private static string[] m_ContentTypes = new string[] { 
            "application/json",
            "application/octet-stream",
            "application/xml",
            "audio/mp4",
            "audio/ogg",
            "image/bmp",
            "image/png",
            "image/tiff",
            "text/csv",
            "text/html",
            "text/plain",
            "application/x-www-form-urlencoded"
        };

        internal static string GetHttpContentType(HttpContentType contentType)
            => m_ContentTypes[(int)contentType];

        internal static string ToContetTypeString(this HttpContentType contentType)
            => m_ContentTypes[(int)contentType];

        // more stuff found in http://www.freeformatter.com/mime-types-list.html
        //public const string JSON = "application/json";
        //public const string BINARY = "application/octet-stream";
        //public const string XML = "application/xml";
        //public const string MP4 = "audio/mp4";
        //public const string OGG = "audio/ogg";
        //public const string BITMAP = "image/bmp";
        //public const string PNG = "image/png";
        //public const string TIFF = "image/tiff";
        //public const string CSV = "text/csv";
        //public const string HTML = "text/html";
        //public const string TEXT = "text/plain";
        //public const string FORM = "application/x-www-form-urlencoded";
    }
}