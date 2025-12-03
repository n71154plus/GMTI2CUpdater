using System;
using System.Collections.Generic;
using System.Globalization;

namespace GMTI2CUpdater.Helper
{
    using System;
    using System.Globalization;

    public static class HexHelper
    {
        public static byte? ParseHexByteOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Trim();

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(2);

            if (byte.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
                return result;

            return null; // 解析失敗也當成沒有定義 / 無效
        }
    }


}
