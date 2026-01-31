using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HacknetFontReplace.Utils
{
    internal static class StringExtension
    {
        public static int TryGetAsInt(this string str, int defaultVal = 0)
        {
            if (int.TryParse(str, out var result))
            {
                return result;
            }

            return defaultVal;
        }

        public static bool TryGetAsBool(this string str, bool defaultVal = false)
        {
            if (bool.TryParse(str, out var result))
            {
                return result;
            }

            return defaultVal;
        }

        public static T TryGetAsThrow<T>(this string str)
        {
            return (T)Convert.ChangeType(str, typeof(T));
        }
    }
}
