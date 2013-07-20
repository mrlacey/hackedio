namespace Hacked.Extensions
{
    using System.Windows.Media;

    public static class ColorExtensions
    {
        public static string ToHexString(this Color source)
        {
            return ColorToHexString(source);
        }

        /// <summary>
        /// Convert a .NET Color to a hex string.
        /// http://www.cambiaresearch.com/articles/1/convert-dotnet-color-to-hex-string
        /// </summary>
        /// <returns>ex: "FFFFFF", "AB12E9"</returns>
        public static string ColorToHexString(Color color)
        {
            char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

            var bytes = new byte[3];
            bytes[0] = color.R;
            bytes[1] = color.G;
            bytes[2] = color.B;
            var chars = new char[bytes.Length * 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }

            return new string(chars);
        }

    }
}
